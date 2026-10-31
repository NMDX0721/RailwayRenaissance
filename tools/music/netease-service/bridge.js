'use strict';
// Private child-process protocol: no HTTP listener and no cookies in replies.
// Upstream dependencies may print banners; stdout is reserved for protocol replies.
console.log = console.info = console.debug = console.warn = console.error = () => {};
const readline = require('node:readline');
const fs = require('node:fs');
const path = require('node:path');
const os = require('node:os');
const { generateDeviceId } = require('@neteasecloudmusicapienhanced/api/util/index');
global.deviceId = global.deviceId || generateDeviceId();
const anonymousPath = path.join(os.tmpdir(), 'anonymous_token');
if (!fs.existsSync(anonymousPath)) fs.writeFileSync(anonymousPath, '');
// Match Folia's QR transport fixes: don't attach a synthetic localhost/client IP, and
// preserve the upstream QR poll error instead of the package's broken catch path.
const requestPath = require.resolve('@neteasecloudmusicapienhanced/api/util/request');
const upstreamRequest = require(requestPath);
require.cache[requestPath].exports = (...args) => {
  const [uri, data, options = {}] = args;
  return upstreamRequest(uri, data, { ...options, ip: '', realIP: '', randomCNIP: false });
};
const qrCheckPath = require.resolve('@neteasecloudmusicapienhanced/api/module/login_qr_check');
require(qrCheckPath);
const createOption = require('@neteasecloudmusicapienhanced/api/util/option');
require.cache[qrCheckPath].exports = async (query, request) => {
  const result = await request('/api/login/qrcode/client/login', { key: query.key, type: 3 }, createOption(query));
  return { status: 200, body: { ...result.body, cookie: (result.cookie || []).join(';') }, cookie: result.cookie || [] };
};
const api = require('@neteasecloudmusicapienhanced/api');
let cookie = '';
let qrKey = '';
let qrKeyRetry = false;
let anonymousCookie = '';
let initialized = false;
async function prepareLogin() {
  if (initialized) return;
  const keyPath = path.join(os.tmpdir(), 'xeapi_public_key');
  let cached = {};
  try { cached = JSON.parse(fs.readFileSync(keyPath, 'utf8')); } catch {}
  if (!cached.sk) {
    const { getXeapiPublicKey } = require('@neteasecloudmusicapienhanced/api/util/xeapiKey');
    let timer;
    try {
      const key = await Promise.race([getXeapiPublicKey({}, global.deviceId), new Promise((_, reject) => { timer = setTimeout(() => reject(new Error('登录初始化超时，请重试')), 10000); })]);
      fs.writeFileSync(keyPath, JSON.stringify(key));
    } finally { clearTimeout(timer); }
  }
  const registration = await api.register_anonimous({ randomCNIP: false, timeout: 10000 });
  if (registration.body?.code !== 200 || !registration.body?.cookie) throw new Error('登录设备初始化失败，请重试');
  anonymousCookie = registration.body.cookie;
  initialized = true;
}
const sleep = ms => new Promise(resolve => setTimeout(resolve, ms));
const reply = (id, result) => process.stdout.write(JSON.stringify({ id, ...result }) + '\n');
const body = result => result && result.body || {};
const numberId = value => { const id = String(value || ''); if (!/^\d{1,20}$/.test(id)) throw new Error('无效的音乐编号'); return id; };
const call = async (method, args = {}) => body(await api[method]({ ...args, cookie: cookie || anonymousCookie, randomCNIP: false, timeout: 25000 }));
const coverUrl = value => {
  try { const u = new URL(value); if (u.protocol === 'http:' && (u.hostname === 'music.126.net' || u.hostname.endsWith('.music.126.net'))) u.protocol = 'https:'; return u.protocol === 'https:' && !u.username && !u.password ? u.toString() : ''; } catch { return ''; }
};
const track = s => ({ id: String(s.id), title: s.name || '', artist: (s.ar || s.artists || []).map(a => a.name).filter(Boolean).join(' / '), album: (s.al || s.album || {}).name || '', durationMs: s.dt || s.duration || 0, cover: coverUrl((s.al || s.album || {}).picUrl || '') });
async function execute(method, p) {
  switch (method) {
    case 'ping': return { service: 'netease', protocol: 1 };
    case 'qr_start': {
      await prepareLogin();
      let keyResult = await call('login_qr_key');
      if ((!keyResult.data || !keyResult.data.unikey) && !qrKeyRetry) { qrKeyRetry = true; await sleep(300); keyResult = await call('login_qr_key'); }
      qrKeyRetry = false;
      qrKey = keyResult.data && keyResult.data.unikey || '';
      if (!qrKey) throw new Error('无法获取登录二维码');
      const qr = await call('login_qr_create', { key: qrKey, qrimg: true });
      if (!qr.data || !qr.data.qrimg) throw new Error('无法生成登录二维码');
      return { image: qr.data.qrimg };
    }
    case 'qr_check': {
      if (!qrKey) throw new Error('请先生成二维码');
      const result = await call('login_qr_check', { key: qrKey });
      const code = Number(result.code || 0);
      if (code === 803 && result.cookie) { cookie = result.cookie; qrKey = ''; }
      if (![800, 801, 802, 803].includes(code)) throw new Error('登录被拒绝或设备环境异常，请稍后重试');
      if (code === 803 && !cookie) throw new Error('登录确认未返回有效会话，请重新扫码');
      return { code, message: result.message || '' };
    }
    case 'status': {
      const result = await call('login_status');
      const data = result.data || {};
      return { loggedIn: !!(data.account && data.profile), userId: data.account ? String(data.account.id) : '', nickname: data.profile && data.profile.nickname || '' };
    }
    case 'logout': cookie = ''; qrKey = ''; return { loggedIn: false };
    case 'search': {
      const keywords = String(p.keywords || '').trim().slice(0, 200);
      if (!keywords) throw new Error('请输入搜索内容');
      const data = await call('cloudsearch', { keywords, type: 1, limit: 30 });
      if (data.code !== 200) throw new Error('搜索失败');
      return { tracks: ((data.result || {}).songs || []).map(track) };
    }
    case 'playlists': {
      const result = await call('user_playlist', { uid: numberId(p.userId), limit: 100, offset: Math.max(0, Number(p.offset) || 0) });
      if (result.code !== 200) throw new Error('无法读取歌单');
      return { playlists: (result.playlist || []).map(x => ({ id: String(x.id), title: x.name || '', count: x.trackCount || 0 })), more: !!result.more, nextOffset: Math.max(0, Number(p.offset) || 0) + (result.playlist || []).length };
    }
    case 'playlist_tracks': {
      const result = await call('playlist_track_all', { id: numberId(p.id), limit: 1000, offset: Math.max(0, Number(p.offset) || 0) });
      if (result.code !== 200) throw new Error('无法读取歌单曲目');
      return { tracks: (result.songs || []).map(track), more: (result.songs || []).length >= 1000, nextOffset: Math.max(0, Number(p.offset) || 0) + 1000 };
    }
    case 'lyric': {
      const result = await call('lyric', { id: numberId(p.id) });
      if (result.code !== 200) throw new Error('无法读取歌词');
      return { lyric: (result.lrc || {}).lyric || '', translation: (result.tlyric || {}).lyric || '' };
    }
    case 'resolve': {
      const result = await call('song_url', { id: numberId(p.id), br: 128000 });
      if (result.code !== 200) throw new Error('无法获取播放地址');
      const item = (result.data || [])[0] || {};
      let url = item.url || '';
      if (url) {
        const parsed = new URL(url);
        const host = parsed.hostname.toLowerCase();
        if (parsed.protocol === 'http:' && (host === 'music.126.net' || host.endsWith('.music.126.net') || host === 'music.163.com' || host.endsWith('.music.163.com'))) parsed.protocol = 'https:';
        if (parsed.protocol !== 'https:') throw new Error('播放地址不可用或不安全');
        url = parsed.toString();
      }
      return { url, trial: !!item.freeTrialInfo, type: item.type || '' };
    }
    default: throw new Error('不支持的请求');
  }
}
// Serialize calls so QR state cannot race. Treat upstream error text as sensitive.
let pending = Promise.resolve();
readline.createInterface({ input: process.stdin, crlfDelay: Infinity }).on('line', line => {
  pending = pending.then(async () => {
    let request;
    try {
      if (line.length > 16384) throw new Error('请求过长');
      request = JSON.parse(line.replace(/^\uFEFF/, ''));
      if (!request || !Number.isSafeInteger(request.id) || typeof request.method !== 'string') throw new Error('无效请求');
      const result = await execute(request.method, request.params || {});
      reply(request.id, { ok: true, result });
    } catch (error) {
      reply(request && Number.isSafeInteger(request.id) ? request.id : null, { ok: false, error: error instanceof SyntaxError ? '无效请求' : (error.message || '').startsWith('无效') || ['登录初始化超时，请重试','登录设备初始化失败，请重试','登录被拒绝或设备环境异常，请稍后重试','登录确认未返回有效会话，请重新扫码','不支持的请求','请求过长','请输入搜索内容','请先生成二维码','无法获取登录二维码','无法生成登录二维码','搜索失败','无法读取歌单','无法读取歌单曲目','无法获取播放地址'].includes(error.message) ? error.message : '网易云请求失败，请检查网络或稍后重试' });
    }
  });
});

