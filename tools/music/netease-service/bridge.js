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
// Login persistence: the session is kept only as a Windows per-user (DPAPI) encrypted blob inside the
// auth directory Unity passes in; the plain cookie never touches disk, argv or the protocol replies.
const { spawn } = require('node:child_process');
const authDir = process.env.RAILWAY_MUSIC_AUTH_DIR || '';
const authFile = authDir && process.platform === 'win32' ? path.join(authDir, 'netease.auth') : '';
let authRestored = false;
const dpapi = (mode, text) => new Promise((resolve, reject) => {
  const script = 'Add-Type -AssemblyName System.Security;$i=[Console]::In.ReadToEnd().Trim();' + (mode === 'protect'
    ? "[Convert]::ToBase64String([Security.Cryptography.ProtectedData]::Protect([Text.Encoding]::UTF8.GetBytes($i),$null,'CurrentUser'))"
    : "[Text.Encoding]::UTF8.GetString([Security.Cryptography.ProtectedData]::Unprotect([Convert]::FromBase64String($i),$null,'CurrentUser'))");
  const child = spawn('powershell.exe', ['-NoProfile', '-NonInteractive', '-Command', script], { windowsHide: true, stdio: ['pipe', 'pipe', 'ignore'] });
  let out = ''; const timer = setTimeout(() => { child.kill(); reject(new Error('dpapi timeout')); }, 20000);
  child.stdout.on('data', chunk => { out += chunk; });
  child.on('error', error => { clearTimeout(timer); reject(error); });
  child.on('close', code => { clearTimeout(timer); code === 0 && out.trim() ? resolve(out.trim()) : reject(new Error('dpapi failed')); });
  child.stdin.on('error', () => {}); child.stdin.end(text);
});
const removeAuth = () => { try { if (authFile) fs.unlinkSync(authFile); } catch {} };
async function persistAuth() {
  if (!authFile || !cookie) return;
  try {
    fs.mkdirSync(authDir, { recursive: true });
    const blob = await dpapi('protect', Buffer.from(JSON.stringify({ cookie, deviceId: global.deviceId })).toString('base64'));
    fs.writeFileSync(authFile + '.tmp', blob); fs.renameSync(authFile + '.tmp', authFile);
  } catch { /* Not persisting is acceptable; the session still works for this run. */ }
}
async function restoreAuth() {
  if (authRestored || cookie || !authFile) return;
  authRestored = true;
  try {
    if (!fs.existsSync(authFile)) return;
    const data = JSON.parse(Buffer.from(await dpapi('unprotect', fs.readFileSync(authFile, 'utf8')), 'base64').toString('utf8'));
    if (typeof data.cookie === 'string' && data.cookie) {
      cookie = data.cookie;
      if (typeof data.deviceId === 'string' && data.deviceId) global.deviceId = data.deviceId;
    }
  } catch { removeAuth(); }
}
async function prepareLogin() {
  if (initialized) return;
  // Folia treats anonymous refresh as best effort; an upstream refusal must not block QR generation.
  initialized = true;
  const keyPath = path.join(os.tmpdir(), 'xeapi_public_key');
  const within = async (work, ms) => {
    let timer;
    try { return await Promise.race([work, new Promise((_, reject) => { timer = setTimeout(() => reject(new Error('timeout')), ms); })]); }
    finally { clearTimeout(timer); }
  };
  let cached = {};
  try { cached = JSON.parse(fs.readFileSync(keyPath, 'utf8')); } catch {}
  if (!cached.sk) {
    try {
      const { getXeapiPublicKey } = require('@neteasecloudmusicapienhanced/api/util/xeapiKey');
      const key = await within(getXeapiPublicKey({}, global.deviceId), 7000);
      fs.writeFileSync(keyPath, JSON.stringify(key));
    } catch { return; }
  }
  try {
    const registration = await within(api.register_anonimous({ randomCNIP: false, timeout: 8000 }), 8000);
    if (registration.body?.code === 200) {
      anonymousCookie = registration.body?.cookie || (registration.cookie || []).join(';');
    }
  } catch { /* QR still uses the initialized, stable device identity. */ }
}

const sleep = ms => new Promise(resolve => setTimeout(resolve, ms));
// NetEase's own word-timed lyrics ("[start,dur](start,dur,0)word...") become enhanced LRC; nothing is estimated.
const lrcStamp = ms => { const t = Math.max(0, Math.trunc(ms)); return String(Math.floor(t / 60000)).padStart(2, '0') + ':' + String(Math.floor(t % 60000 / 1000)).padStart(2, '0') + '.' + String(t % 1000).padStart(3, '0'); };
function yrcToEnhancedLrc(yrc) {
  const out = [];
  for (const raw of String(yrc || '').split('\n')) {
    const head = /^\[(\d+),(\d+)\]/.exec(raw);
    if (!head) continue;
    const parts = []; const re = /\((\d+),(\d+),-?\d+\)([^(]*)/g; re.lastIndex = head[0].length; let m, end = 0;
    while ((m = re.exec(raw))) { parts.push({ at: +m[1], text: m[3] }); end = +m[1] + +m[2]; }
    if (!parts.length || !parts.map(p => p.text).join('').trim()) continue;
    out.push('[' + lrcStamp(+head[1]) + ']' + parts.map(p => '<' + lrcStamp(p.at) + '>' + p.text).join('') + '<' + lrcStamp(end) + '>');
  }
  return out.join('\n');
}
const reply = (id, result) => process.stdout.write(JSON.stringify({ id, ...result }) + '\n');
const body = result => result && result.body || {};
const numberId = value => { const id = String(value || ''); if (!/^\d{1,20}$/.test(id)) throw new Error('无效的音乐编号'); return id; };
const call = async (method, args = {}) => body(await api[method]({ ...args, cookie: cookie || anonymousCookie, randomCNIP: false, timeout: args.timeout || 25000 }));
const coverUrl = value => {
  try { const u = new URL(typeof value === 'string' && value.startsWith('//') ? 'https:' + value : value); if (u.protocol === 'http:' && (u.hostname === 'music.126.net' || u.hostname.endsWith('.music.126.net'))) u.protocol = 'https:'; return u.protocol === 'https:' && !u.username && !u.password ? u.toString() : ''; } catch { return ''; }
};
const track = s => ({ id: String(s.id), title: s.name || '', translated: String((s.tns || [])[0] || '').trim(), artist: (s.ar || s.artists || []).map(a => a.name).filter(Boolean).join(' / '), album: (s.al || s.album || {}).name || '', durationMs: s.dt || s.duration || 0, cover: coverUrl((s.al || s.album || {}).picUrl || '') });
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
      if (code === 803 && result.cookie) { cookie = result.cookie; qrKey = ''; await persistAuth(); }
      if (![800, 801, 802, 803].includes(code)) throw new Error('登录被拒绝或设备环境异常，请稍后重试');
      if (code === 803 && !cookie) throw new Error('登录确认未返回有效会话，请重新扫码');
      return { code, message: result.message || '' };
    }
    case 'status': {
      await restoreAuth();
      // Nothing stored and nothing signed in this run: answer at once so the startup check never queues behind a network call.
      if (!cookie) return { loggedIn: false, userId: '', nickname: '' };
      const result = await call('login_status');
      const data = result.data || {};
      const loggedIn = !!(data.account && data.profile);
      // Forget a stored session only when the service answered and no longer accepts it.
      if (!loggedIn && cookie && (result.code === 200 || data.code === 200)) { cookie = ''; removeAuth(); }
      return { loggedIn, userId: data.account ? String(data.account.id) : '', nickname: data.profile && data.profile.nickname || '' };
    }
    case 'logout': cookie = ''; qrKey = ''; removeAuth(); return { loggedIn: false };
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
      return { playlists: (result.playlist || []).map(x => ({ id: String(x.id), title: x.name || '', count: x.trackCount || 0, cover: coverUrl(x.coverImgUrl) })), more: !!result.more, nextOffset: Math.max(0, Number(p.offset) || 0) + (result.playlist || []).length };
    }
    case 'playlist_tracks': {
      const result = await call('playlist_track_all', { id: numberId(p.id), limit: 1000, offset: Math.max(0, Number(p.offset) || 0) });
      if (result.code !== 200) throw new Error('无法读取歌单曲目');
      return { tracks: (result.songs || []).map(track), more: (result.songs || []).length >= 1000, nextOffset: Math.max(0, Number(p.offset) || 0) + 1000 };
    }
    case 'metadata': {
      const ids = Array.isArray(p.ids) && p.ids.length > 0 && p.ids.length <= 50 ? p.ids.map(numberId).join(',') : numberId(p.id);
      const result = await call('song_detail', { ids, timeout: 10000 });
      if (result.code !== 200 || !result.songs?.length) throw new Error('无法读取曲目信息');
      return { tracks: result.songs.map(track) };
    }
    case 'lyric': {
      const id = numberId(p.id);
      let result = null;
      try { result = await call('lyric_new', { id, timeout: 15000 }); } catch { result = null; }
      if (!result || result.code !== 200) result = await call('lyric', { id });
      if (result.code !== 200) throw new Error('无法读取歌词');
      return { lyric: (result.lrc || {}).lyric || '', translation: (result.tlyric || {}).lyric || '', wordLyric: yrcToEnhancedLrc((result.yrc || {}).lyric), pureMusic: !!result.pureMusic || /纯音乐，请欣赏/.test((result.lrc || {}).lyric || '') };
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

