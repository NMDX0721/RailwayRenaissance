'use strict';
const readline = require('node:readline');
const crypto = require('node:crypto');
let config = null;
const send = (id, payload) => process.stdout.write(JSON.stringify({ id, ...payload }) + '\n');
function base(raw) {
  const url = new URL(String(raw || '').trim());
  if (url.username || url.password || url.search || url.hash) throw new Error('服务地址无效');
  const local = ['localhost', '127.0.0.1', '[::1]'].includes(url.hostname);
  if (url.protocol !== 'https:' && !(local && url.protocol === 'http:')) throw new Error('服务器需要 HTTPS');
  return url.toString().replace(/\/+$/, '');
}
function identity(settings) { return crypto.createHash('sha256').update(settings.base).digest('hex').slice(0,16); }
function encode(settings, id) { return identity(settings) + '~' + Buffer.from(String(id), 'utf8').toString('base64url'); }
function decode(settings, value) {
  const match = /^([a-f0-9]{16})~([A-Za-z0-9_-]{1,300})$/.exec(String(value || ''));
  if (!match || match[1] !== identity(settings)) throw new Error('曲目属于其他服务器');
  return Buffer.from(match[2], 'base64url').toString('utf8');
}
function endpoint(settings, method, params = {}) {
  const url = new URL(settings.base + '/rest/' + method + '.view');
  const salt = crypto.randomBytes(8).toString('hex');
  const token = crypto.createHash('md5').update(settings.password + salt, 'utf8').digest('hex');
  for (const [k,v] of Object.entries({ u: settings.user, t: token, s: salt, v: '1.16.1', c: 'RailwayMusic', f: 'json', ...params })) url.searchParams.set(k,String(v));
  return url;
}
async function request(settings, method, params) {
  const response = await fetch(endpoint(settings, method, params), { redirect: 'error', signal: AbortSignal.timeout(20000) });
  if (!response.ok) throw new Error('服务器请求失败');
  const data = (await response.json())['subsonic-response'];
  if (!data || data.status !== 'ok') throw new Error('服务器拒绝请求，请检查账号、权限或版本');
  return data;
}
const track = (settings, x) => ({ id: encode(settings, x.id), title: x.title || '', artist: x.artist || '', album: x.album || '', durationMs: Number(x.duration || 0) * 1000 });
async function execute(method, p) {
  switch (method) {
    case 'ping': return { service: 'subsonic', protocol: 1 };
    case 'connect': {
      const candidate = { base: base(p.host), user: String(p.username || '').trim(), password: String(p.password || '') };
      if (!candidate.user || !candidate.password) throw new Error('请填写服务器、用户名和密码');
      await request(candidate, 'ping'); config = candidate;
      return { loggedIn: true, nickname: candidate.user };
    }
    case 'status': if (!config) return { loggedIn: false }; await request(config, 'ping'); return { loggedIn: true, nickname: config.user };
    case 'logout': config = null; return { loggedIn: false };
    case 'search': {
      if (!config) throw new Error('请先连接服务器');
      const keyword = String(p.keywords || '').trim().slice(0,200); if (!keyword) throw new Error('请输入搜索内容');
      const data = await request(config, 'search3', { query: keyword, songCount: 50, albumCount: 0, artistCount: 0 });
      return { tracks: (data.searchResult3?.song || []).map(x => track(config, x)) };
    }
    case 'playlists': {
      if (!config) throw new Error('请先连接服务器');
      const data = await request(config, 'getPlaylists');
      return { playlists: (data.playlists?.playlist || []).map(x => ({ id: encode(config, x.id), title: x.name || '', count: Number(x.songCount || 0) })) };
    }
    case 'playlist_tracks': {
      if (!config) throw new Error('请先连接服务器');
      const data = await request(config, 'getPlaylist', { id: decode(config, p.id) });
      return { tracks: (data.playlist?.entry || []).map(x => track(config, x)) };
    }
    case 'resolve': {
      if (!config) throw new Error('请先连接服务器');
      const url = endpoint(config, 'stream', { id: decode(config, p.id), format: 'mp3', maxBitRate: 192 });
      return { url: url.toString(), type: 'mp3', trial: false, localHttp: url.protocol === 'http:' };
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
      const known = ['无效请求','服务地址无效','服务器需要 HTTPS','曲目属于其他服务器','服务器请求失败','服务器拒绝请求，请检查账号、权限或版本','请填写服务器、用户名和密码','请先连接服务器','请输入搜索内容','不支持的请求'];
      send(request && Number.isSafeInteger(request.id) ? request.id : null, { ok: false, error: known.includes(error.message) ? error.message : '服务器连接失败' });
    }
  });
});
