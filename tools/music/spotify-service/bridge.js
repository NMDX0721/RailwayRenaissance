// SPDX-License-Identifier: AGPL-3.0
'use strict';
const http = require('node:http');
const { Readable } = require('node:stream');
const { pipeline } = require('node:stream/promises');
const { createSpotifyApiBridge } = require('./vendor/electron/spotify/spotifyApiBridge.cjs');
const ID = /^[A-Za-z0-9]{22}$/;
const messages = {
  'auth-required': '请先在浏览器中登录 Spotify', 'subscription-required': '这首曲目需要 Spotify Premium',
  'network': 'Spotify 网络请求失败或请求过于频繁，请稍后重试', 'unavailable': 'Spotify 服务暂不可用或接口已更新',
  'unsupported': 'Spotify 不支持此操作', 'invalid-response': 'Spotify 返回数据不完整，请重试',
  'not-playable': 'Spotify 曲目当前不可播放', 'region-restricted': 'Spotify 曲目在当前地区不可用',
};
const trackId = uri => typeof uri === 'string' ? /^spotify:track:([A-Za-z0-9]{22})$/.exec(uri)?.[1] || '' : '';
const playlistId = uri => typeof uri === 'string' ? /^spotify:(?:user:[^:]+:)?playlist:([A-Za-z0-9]{22})$/.exec(uri)?.[1] || '' : '';
const track = (row, beside) => {
  const id = trackId(row?.uri || beside);
  if (!id || !row?.name || row.__typename && row.__typename !== 'Track') return null;
  const pictures = [...(row.albumOfTrack?.coverArt?.sources || [])].sort((a,b) => (b.width || 0) - (a.width || 0));
  let cover = '';
  try { const u = new URL(pictures[0]?.url); if (u.protocol === 'https:' && !u.username && !u.password) cover = u.toString(); } catch {}
  return { id, title: row.name, artist: (row.artists?.items || []).map(a => a.profile?.name).filter(Boolean).join(' / '),
    album: row.albumOfTrack?.name || '', durationMs: Math.max(0, Number(row.duration?.totalMilliseconds ?? row.trackDuration?.totalMilliseconds) || 0), cover };
};
const page = (rows, offset, total) => ({ more: rows.length > 0 && (Number.isFinite(total) ? offset + rows.length < total : rows.length >= 50), nextOffset: offset + rows.length });
function createService({ bridge: supplied, fetch: fetcher = fetch } = {}) {
  let loginKey = '', externalUrl = '', server = null, base = '';
  const memory = new Map();
  const bridge = supplied || createSpotifyApiBridge({
    store: { get: key => memory.get(key), set: (key, value) => memory.set(key,value), delete: key => memory.delete(key) },
    fetch: fetcher, openExternal: async url => { const u = new URL(url); if (u.origin !== 'https://accounts.spotify.com' || u.pathname !== '/authorize') throw new Error('无效授权地址'); externalUrl = u.toString(); },
    language: () => 'zh-CN', log: () => {}, warn: () => {},
  });
  const ask = async (operation, params = {}) => {
    const reply = await bridge.request(operation, params);
    if (!reply.ok) throw new Error(messages[reply.error?.code] || 'Spotify 请求失败，请重试');
    return reply.data;
  };
  const query = (name, variables) => ask('query', { name, variables });
  const audioServer = async () => {
    if (server) return base;
    const candidate = http.createServer(async (req, res) => {
      try {
        if (!base || req.headers.host !== new URL(base).host || req.method !== 'GET' || req.headers.origin) { res.writeHead(403).end(); return; }
        const u = new URL(req.url, base);
        const match = /^\/audio\/([A-Za-z0-9]{22})$/.exec(u.pathname);
        if (!match || !/^[a-f0-9]{32}$/.test(u.searchParams.get('k') || '')) { res.writeHead(404).end(); return; }
        const headers = new Headers(); if (req.headers.range) headers.set('range', req.headers.range);
        const answer = await bridge.handleAudioRequest(new Request('folia-spotify://audio/' + match[1] + '?k=' + u.searchParams.get('k'), { headers }));
        if (!answer || answer.status < 200 || answer.status >= 400) { res.writeHead(answer?.status >= 400 ? answer.status : 502).end(); return; }
        for (const [key, value] of answer.headers) if (!key.startsWith('access-control')) res.setHeader(key, value);
        res.writeHead(answer.status);
        if (!answer.body) { res.end(); return; }
        await pipeline(Readable.fromWeb(answer.body), res);
      } catch { if (!res.headersSent) res.writeHead(502); res.end(); }
    });
    candidate.requestTimeout = 35000; candidate.headersTimeout = 10000;
    await new Promise((resolve, reject) => { candidate.once('error', reject); candidate.listen(0, '127.0.0.1', resolve); });
    server = candidate; base = 'http://127.0.0.1:' + candidate.address().port;
    return base;
  };
  return {
    async execute(method, p = {}) {
      const offset = Math.max(0, Math.min(20000, Math.trunc(Number(p.offset) || 0)));
      switch (method) {
        case 'ping': return { service: 'spotify', protocol: 1 };
        case 'login_start': {
          const started = await ask('login_start'); loginKey = started.key; externalUrl = '';
          if (!await ask('login_open', { key: loginKey }) || !externalUrl) throw new Error('无法打开 Spotify 授权，请重试');
          return { url: externalUrl };
        }
        case 'login_check': {
          if (!loginKey) throw new Error('请先发起 Spotify 登录');
          const state = await ask('login_check', { key: loginKey });
          if (state?.state === 'error') throw new Error('Spotify 授权未完成，请重新登录');
          return { code: state?.state === 'confirmed' ? 803 : state?.state === 'expired' ? 800 : 801 };
        }
        case 'status': {
          const user = await ask('login_status');
          return { loggedIn: !!user, userId: user?.id || '', nickname: user?.nickname || '', sessionOnly: true };
        }
        case 'logout': loginKey = ''; await ask('logout'); return { loggedIn: false };
        case 'search': {
          const keywords = String(p.keywords || '').trim().slice(0,200); if (!keywords) throw new Error('请输入搜索内容');
          const answer = await query('searchTracks', { searchTerm: keywords, offset, limit: 50, includePreReleases: false, numberOfTopResults: 20, includeAudiobooks: true, includeAuthors: false });
          const list = answer?.searchV2?.tracksV2; if (!Array.isArray(list?.items)) throw new Error('Spotify 搜索返回数据不完整');
          return { tracks: list.items.map(r => track(r.item?.data)).filter(Boolean), ...page(list.items, offset, list.totalCount) };
        }
        case 'playlists': {
          const answer = await query('libraryV3', { filters: ['Playlists'], order: null, textFilter: '', features: [], limit: 50, offset, flatten: true, expandedFolders: [], folderUri: null, includeFoldersWhenFlattening: true });
          const list = answer?.me?.libraryV3; if (!Array.isArray(list?.items)) throw new Error('Spotify 歌单返回数据不完整');
          const playlists = list.items.map(r => r.item?.data).filter(r => playlistId(r?.uri)).map(r => ({ id: playlistId(r.uri), title: r.name || 'Spotify 歌单', count: r.content?.totalCount || 0 }));
          if (offset === 0) playlists.unshift({ id: 'liked', title: '已点赞的歌曲', count: 0 });
          return { playlists, ...page(list.items, offset, list.totalCount) };
        }
        case 'playlist_tracks': {
          if (p.id !== 'liked' && !ID.test(String(p.id || ''))) throw new Error('无效 Spotify 歌单编号');
          const liked = p.id === 'liked';
          const answer = await query(liked ? 'fetchLibraryTracks' : 'fetchPlaylist', liked ? { offset, limit: 50 } : { uri: 'spotify:playlist:' + p.id, offset, limit: 50, enableWatchFeedEntrypoint: false });
          const list = liked ? answer?.me?.library?.tracks : answer?.playlistV2?.content;
          if (!Array.isArray(list?.items)) throw new Error('Spotify 曲目返回数据不完整');
          return { tracks: list.items.map(r => liked ? track(r.track?.data, r.track?._uri) : track(r.itemV2?.data)).filter(Boolean), ...page(list.items, offset, list.totalCount) };
        }
        case 'resolve': {
          if (!ID.test(String(p.id || ''))) throw new Error('无效 Spotify 曲目编号');
          const audio = await ask('audio', { id: p.id, quality: 'high' });
          const u = new URL(audio.url);
          if (u.protocol !== 'folia-spotify:' || u.hostname !== 'audio' || u.pathname !== '/' + p.id) throw new Error('Spotify 播放地址不可用');
          return { url: await audioServer() + '/audio/' + p.id + u.search, localHttp: true, type: /^MP3/.test(audio.format) ? 'mp3' : 'ogg', trial: false };
        }
        case 'lyric': {
          if (!ID.test(String(p.id || ''))) throw new Error('无效 Spotify 曲目编号');
          const lyric = await ask('lyrics', { id: p.id });
          if (!lyric?.timed) return { lyric: '' };
          return { lyric: (lyric.lines || []).map(l => { const ms = Math.max(0, Math.trunc(l.startMs)); return '[' + String(Math.floor(ms / 60000)).padStart(2,'0') + ':' + (ms % 60000 / 1000).toFixed(3).padStart(6,'0') + ']' + String(l.words).replace(/[\r\n]/g, ' '); }).join('\n') };
        }
        default: throw new Error('不支持的 Spotify 请求');
      }
    },
    async close() { await bridge.request('logout'); if (server) { server.closeAllConnections?.(); server.close(); server = null; base = ''; } },
  };
}
module.exports = { createService, track, playlistId, page };
if (require.main === module) {
  console.log = console.info = console.warn = console.error = console.debug = () => {};
  const service = createService(); let pending = Promise.resolve();
  const input = require('node:readline').createInterface({ input: process.stdin, crlfDelay: Infinity });
  input.on('line', line => { pending = pending.then(async () => {
    let request;
    try {
      if (line.length > 16384) throw new Error('请求过长');
      request = JSON.parse(line.replace(/^\uFEFF/,''));
      if (!Number.isSafeInteger(request.id) || typeof request.method !== 'string') throw new Error('无效请求');
      const result = await service.execute(request.method, request.params || {});
      process.stdout.write(JSON.stringify({ id: request.id, ok: true, result }) + '\n');
    } catch (e) { process.stdout.write(JSON.stringify({ id: request?.id ?? null, ok: false, error: /^(Spotify|请先|请输入|无效|无法|这首|不支持|请求过长)/.test(e.message || '') ? e.message : 'Spotify 请求失败，请重试' }) + '\n'); }
  }); });
  input.on('close', () => { pending.finally(() => service.close()).finally(() => process.exit(0)); });
  process.on('SIGTERM', () => service.close().finally(() => process.exit(0)));
}
