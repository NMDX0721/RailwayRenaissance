'use strict';
process.env.platform = 'lite';
console.log = console.info = console.debug = console.warn = console.error = () => {};
const readline = require('node:readline');
const api = require('kugoumusicapi');
let key = '', cookie = {};
const send = (id, payload) => process.stdout.write(JSON.stringify({ id, ...payload }) + '\n');
const call = async (method, params = {}) => (await api[method]({ ...params, cookie })).body || {};
const validId = id => /^[A-Fa-f0-9]{32}~\d{1,20}~\d{1,20}$/.test(String(id || ''));
function track(s) {
  const hash = String(s.FileHash || s.hash || s.Hash || '').toLowerCase();
  const mix = String(s.MixSongID || s.album_audio_id || 0), album = String(s.AlbumID || s.album_id || 0);
  const id = hash + '~' + mix + '~' + album;
  return { id, title: s.OriSongName || s.songname || s.FileName || '', artist: s.SingerName || s.singername || '', album: s.AlbumName || s.album_name || '', durationMs: Number(s.Duration || s.duration || 0) * 1000 };
}
async function execute(method, p) {
  switch (method) {
    case 'ping': return { service: 'kugou', protocol: 1 };
    case 'qr_start': {
      const response = await call('login_qr_key'); key = response.data?.qrcode || '';
      if (!key) throw new Error('无法获取酷狗二维码');
      const image = await call('login_qr_create', { key, qrimg: true });
      if (!image.data?.base64) throw new Error('无法生成酷狗二维码');
      return { image: image.data.base64 };
    }
    case 'qr_check': {
      if (!key) throw new Error('请先生成二维码');
      const response = await call('login_qr_check', { key });
      const data = response.data || {}; const raw = Number(data.status);
      if (raw === 4 && data.token && data.userid) { cookie = { token: data.token, userid: String(data.userid) }; key = ''; }
      return { code: raw === 4 && cookie.token ? 803 : raw === 0 ? 800 : raw === 2 ? 802 : 801 };
    }
    case 'status': {
      if (!cookie.token) return { loggedIn: false };
      const response = await call('user_detail');
      const data = response.data || {}; const profile = data.user_info || data.userinfo || data.profile || data;
      const id = String(profile.userid || profile.user_id || profile.id || profile.uid || cookie.userid || '');
      const nickname = String(profile.nickname || profile.nick_name || profile.username || profile.name || '');
      return { loggedIn: response.status === 1 && id === String(cookie.userid) && !!nickname, nickname, userId: id };
    }
    case 'logout': key = ''; cookie = {}; return { loggedIn: false };
    case 'search': {
      const keywords = String(p.keywords || '').trim().slice(0, 100);
      if (!keywords) throw new Error('请输入搜索内容');
      const response = await call('search', { keywords, pagesize: 30 });
      if (response.status !== 1) throw new Error('酷狗搜索失败');
      return { tracks: (response.data?.lists || []).map(track).filter(x => validId(x.id)) };
    }
    case 'playlists': {
      if (!cookie.token) throw new Error('请先登录酷狗');
      const response = await call('user_playlist', { pagesize: 100, page: Math.floor(Math.max(0, Number(p.offset) || 0) / 100) + 1 });
      if (response.status !== 1) throw new Error('无法读取酷狗歌单');
      const items = response.data?.info || response.data?.list || [];
      return { playlists: items.map(x => ({ id: String(x.global_collection_id || x.specialid || ''), title: x.name || x.specialname || '酷狗歌单', count: Number(x.count || x.song_count || 0) })).filter(x => x.id), more: Number(response.data?.total || 0) > Math.max(0, Number(p.offset) || 0) + items.length || items.length >= 100, nextOffset: Math.max(0, Number(p.offset) || 0) + 100 };
    }
    case 'playlist_tracks': {
      if (!/^[A-Za-z0-9_-]{1,80}$/.test(String(p.id || ''))) throw new Error('酷狗歌单编号无效');
      const response = await call('playlist_track_all', { id: p.id, pagesize: 100, page: Math.floor(Math.max(0, Number(p.offset) || 0) / 100) + 1 });
      if (response.status !== 1) throw new Error('无法读取酷狗曲目');
      const items = response.data?.info || response.data?.list || [];
      return { tracks: items.map(track).filter(x => validId(x.id)), more: Number(response.data?.total || 0) > Math.max(0, Number(p.offset) || 0) + items.length || items.length >= 100, nextOffset: Math.max(0, Number(p.offset) || 0) + 100 };
    }
    case 'resolve': {
      if (!validId(p.id)) throw new Error('酷狗曲目编号无效');
      const [hash, mix, album] = p.id.split('~');
      const response = await call('song_url', { hash, album_audio_id: mix, album_id: album, quality: 128 });
      if (response.status !== 1) throw new Error('酷狗曲目不可播放');
      const data = response.data || {}; let url = (data.play_url || data.url || '');
      if (Array.isArray(url)) url = url[0] || '';
      if (url && new URL(url).protocol !== 'https:') throw new Error('酷狗播放地址不可用或不安全');
      return { url, trial: !!data.is_free_part || !!data.free_part, type: 'mp3' };
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
      const known = ['无效请求','无法获取酷狗二维码','无法生成酷狗二维码','请先生成二维码','请输入搜索内容','酷狗搜索失败','请先登录酷狗','无法读取酷狗歌单','酷狗歌单编号无效','无法读取酷狗曲目','酷狗曲目编号无效','酷狗曲目不可播放','酷狗播放地址不可用或不安全','不支持的请求'];
      send(request && Number.isSafeInteger(request.id) ? request.id : null, { ok: false, error: known.includes(error.message) ? error.message : '酷狗请求失败，请检查网络或稍后重试' });
    }
  });
});
