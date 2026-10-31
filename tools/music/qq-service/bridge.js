'use strict';
// Experimental private process protocol; package root must not be loaded (it starts an HTTP server).
console.log = console.info = console.debug = console.warn = console.error = () => {};
const readline = require('node:readline');
const { randomBytes } = require('node:crypto');
const { handleRequest } = require('@yakult-green-tea/qq-music-api/serverless');
const env = { QQ_SESSION_SECRET: randomBytes(32).toString('hex') };
let key = '', session = '';
const send = (id, payload) => process.stdout.write(JSON.stringify({ id, ...payload }) + '\n');
async function invoke(path, params = {}) {
  const url = new URL('https://local.invalid' + path);
  for (const [name,value] of Object.entries(params)) if (value != null) url.searchParams.set(name, String(value));
  const headers = session ? { 'x-qq-session': session } : {};
  const response = await handleRequest(new Request(url, { headers }), env);
  const body = await response.json();
  return { status: response.status, body, cookie: response.headers.get('set-cookie') || '' };
}
function track(item) {
  const singers = item.singer || item.singers || [];
  const album = item.album || {}; const albumMid = String(album.mid || item.albummid || '');
  const cover = /^[A-Za-z0-9]{1,40}$/.test(albumMid) ? `https://y.gtimg.cn/music/photo_new/T002R300x300M000${albumMid}.jpg` : '';
  return { id: String(item.mid || item.songmid || ''), title: item.name || item.title || item.songname || '', artist: Array.isArray(singers) ? singers.map(x => x.name || x.singername).filter(Boolean).join(' / ') : '', album: album.name || item.albumname || '', cover, durationMs: Number(item.interval || 0) * 1000 };
}
async function execute(method, p) {
  switch (method) {
    case 'ping': return { service: 'qq', protocol: 1 };
    case 'qr_start': {
      const created = await invoke('/login/qr/key', { channel: 'qq' });
      key = created.body.data?.unikey || '';
      if (!key) throw new Error('无法获取 QQ 登录二维码');
      const image = await invoke('/login/qr/create', { key });
      if (!image.body.data?.qrimg) throw new Error('无法生成 QQ 登录二维码');
      return { image: image.body.data.qrimg };
    }
    case 'qr_check': {
      if (!key) throw new Error('请先生成二维码');
      const response = await invoke('/login/qr/check', { key });
      const code = Number(response.body.code || 0);
      if (code === 803) {
        const cookie = response.cookie || response.body.cookie || '';
        const match = /(?:^|[;,]\s*)qqmusic_session=([^;]+)/.exec(cookie);
        if (!match) throw new Error('登录会话未返回');
        session = match[1]; key = '';
      }
      return { code };
    }
    case 'status': {
      const response = await invoke('/login/status');
      const profile = response.body.data?.profile;
      return { loggedIn: !!profile, nickname: profile?.nickname || profile?.nick || '', userId: String(profile?.uin || '') };
    }
    case 'logout': session = ''; key = ''; return { loggedIn: false };
    case 'search': {
      const keywords = String(p.keywords || '').trim().slice(0, 60);
      if (!keywords) throw new Error('请输入搜索内容');
      const response = await invoke('/search', { keyword: keywords, type: 'song', limit: 20 });
      if (response.status !== 200 || response.body.code !== 200) throw new Error(response.body.upstreamCode === 2001 ? 'QQ 拒绝匿名搜索，请先登录后重试' : 'QQ 搜索失败');
      return { tracks: (response.body.items || []).map(track).filter(x => x.id) };
    }
    case 'playlists': {
      if (!session) throw new Error('请先登录 QQ 音乐');
      const response = await invoke('/user/playlist', { favoriteOffset: Math.max(0, Number(p.offset) || 0) });
      if (response.status !== 200) throw new Error('无法读取 QQ 歌单');
      return { playlists: (response.body.playlist || []).map(x => ({ id: String(x.tid || x.dissid || 0) + '-' + String(x.dirid || 0), title: x.diss_name || x.title || x.name || 'QQ 歌单', count: Number(x.song_cnt || x.songnum || 0) })), more: !!response.body.more, nextOffset: response.body.nextFavoriteOffset || 0 };
    }
    case 'playlist_tracks': {
      if (!session || !/^\d+-\d+$/.test(String(p.id || ''))) throw new Error('QQ 歌单不可用');
      const [tid, dirid] = p.id.split('-');
      const response = await invoke('/user/playlist-detail', { tid, dirid, limit: 100, offset: Math.max(0, Number(p.offset) || 0) });
      if (response.status !== 200) throw new Error('无法读取 QQ 曲目');
      return { tracks: (response.body.songs || []).map(track).filter(x => x.id), more: !!response.body.more, nextOffset: Math.max(0, Number(p.offset) || 0) + 100 };
    }
    case 'resolve': {
      if (!session) throw new Error('请先登录 QQ 音乐');
      const id = String(p.id || '');
      if (!/^[A-Za-z0-9]{1,40}$/.test(id)) throw new Error('QQ 曲目编号无效');
      const response = await invoke('/getMusicPlay', { songmid: id, quality: '128' });
      if (response.status !== 200) throw new Error('QQ 曲目不可播放');
      const item = response.body.data?.playUrl?.[id] || {};
      let url = item.url || '';
      if (url) {
        const parsed = new URL(url); const host = parsed.hostname.toLowerCase();
        if (parsed.protocol === 'http:' && (host === 'qqmusic.qq.com' || host.endsWith('.qqmusic.qq.com'))) parsed.protocol = 'https:';
        if (parsed.protocol !== 'https:') throw new Error('QQ 播放地址不可用或不安全');
        url = parsed.toString();
      }
      return { url, trial: !!item.preview || !!item.restriction?.preview, type: 'mp3' };
    }
    default: throw new Error('不支持的请求');
  }
}
let pending = Promise.resolve();
readline.createInterface({ input: process.stdin, crlfDelay: Infinity }).on('line', line => {
  pending = pending.then(async () => {
    let request;
    try {
      if (line.length > 16384) throw new Error('无效请求');
      request = JSON.parse(line.replace(/^\uFEFF/, ''));
      if (!request || !Number.isSafeInteger(request.id) || typeof request.method !== 'string') throw new Error('无效请求');
      send(request.id, { ok: true, result: await execute(request.method, request.params || {}) });
    } catch (error) {
      const known = ['无效请求', '无法获取 QQ 登录二维码', '无法生成 QQ 登录二维码', '请先生成二维码', '登录会话未返回', '请输入搜索内容', 'QQ 拒绝匿名搜索，请先登录后重试', 'QQ 搜索失败', '请先登录 QQ 音乐', '无法读取 QQ 歌单', 'QQ 歌单不可用', '无法读取 QQ 曲目', 'QQ 曲目编号无效', 'QQ 曲目不可播放', 'QQ 播放地址不可用或不安全', '不支持的请求'];
      send(request && Number.isSafeInteger(request.id) ? request.id : null, { ok: false, error: known.includes(error.message) ? error.message : 'QQ 音乐请求失败，请检查网络或稍后重试' });
    }
  });
});
