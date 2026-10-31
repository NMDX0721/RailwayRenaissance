'use strict';
console.log = console.info = console.debug = console.warn = console.error = () => {};
const { randomBytes } = require('node:crypto');
const readline = require('node:readline');
const { createBodianApi } = require('bodian-music-api');
const api = createBodianApi({ deviceId: randomBytes(16).toString('hex') });
let key = '';
const send = (id, payload) => process.stdout.write(JSON.stringify({ id, ...payload }) + '\n');
const numberId = id => { const text = String(id || ''); if (!/^\d{1,20}$/.test(text) || /^0+$/.test(text)) throw new Error('无效的波点编号'); return text; };
async function call(method, params = {}) {
  const result = await api.request(method, params);
  if (!result.ok) throw new Error(result.error?.code === 'region-restricted' ? '波点当前地区不可播放' : result.error?.code === 'auth-required' ? '请先登录波点' : '波点请求失败');
  return result.data;
}
const safeCover = value => { try { const u = new URL(value); if (u.protocol === 'http:' && /^[a-z0-9-]+\.kuwo\.cn$/i.test(u.hostname)) u.protocol = 'https:'; return u.protocol === 'https:' && !u.username && !u.password ? u.toString() : ''; } catch { return ''; } };
const track = item => ({ id: String(item.id || ''), title: item.songName || item.name || '', artist: item.artist || '', album: item.album || '', cover: safeCover(item.albumPic || ''), durationMs: Number(item.duration || 0) * 1000 });
async function execute(method, p) {
  switch (method) {
    case 'ping': return { service: 'bodian', protocol: 1 };
    case 'qr_start': {
      const created = await call('login_qr_key'); key = created.key || '';
      if (!key) throw new Error('无法获取波点二维码');
      const qr = await call('login_qr_create', { key });
      if (!qr.imageUrl) throw new Error('无法生成波点二维码');
      return { image: qr.imageUrl };
    }
    case 'qr_check': {
      if (!key) throw new Error('请先生成二维码');
      const result = await call('login_qr_check', { key });
      const code = result.state === 'confirmed' ? 803 : result.state === 'expired' ? 800 : result.state === 'scanned' ? 802 : 801;
      if (code === 803 || code === 800) key = '';
      return { code };
    }
    case 'status': {
      const user = await call('login_status');
      return { loggedIn: !!user, nickname: user?.nickname || '', userId: user?.id || '', sessionOnly: true };
    }
    case 'logout': await call('logout'); key = ''; return { loggedIn: false };
    case 'search': {
      const keywords = String(p.keywords || '').trim().slice(0, 200);
      if (!keywords) throw new Error('请输入搜索内容');
      const result = await call('search', { query: keywords, limit: 30, offset: 0 });
      return { tracks: (result.resultList || []).map(track).filter(x => /^\d+$/.test(x.id)) };
    }
    case 'playlists': {
      const result = await call('user_playlists', { limit: 100, offset: Math.max(0, Number(p.offset) || 0) });
      const owned = Array.isArray(result.owned) ? result.owned : result.owned?.list || [];
      const all = [...owned.map(x => ({ item:x, source:5 })), ...(result.liked ? [{ item:result.liked, source:5 }] : []), ...(result.collected?.playLists || []).map(x => ({ item:x, source:Number(x.sourceType || 4) }))];
      return { playlists: all.filter(x => /^\d+$/.test(String(x.item.id || '')) && [1,2,3,4,5,6].includes(x.source)).map(x => ({ id: String(x.item.id) + '~' + x.source, title: x.item.name || x.item.title || '波点歌单', count: Number(x.item.musicNum || x.item.count || 0) })), more: !!result.collected?.bodianPagination?.hasMore, nextOffset: result.collected?.bodianPagination?.nextOffset || 0 };
    }
    case 'playlist_tracks': {
      const match = /^(\d{1,20})~([1-6])$/.exec(String(p.id || ''));
      if (!match) throw new Error('无效的波点编号');
      const result = await call('playlist_tracks', { id: numberId(match[1]), source: Number(match[2]), limit: 100, offset: Math.max(0, Number(p.offset) || 0) });
      return { tracks: (result.list || []).map(track).filter(x => /^\d+$/.test(x.id)), more: !!result.bodianPagination?.hasMore, nextOffset: result.bodianPagination?.nextOffset || 0 };
    }
    case 'resolve': {
      const result = await call('audio', { id: numberId(p.id), quality: 'standard' });
      const url = result.url || '';
      if (url && new URL(url).protocol !== 'https:') throw new Error('波点播放地址不可用或不安全');
      return { url, trial: !!result.preview, type: 'mp3' };
    }
    case 'lyric': {
      const result = await call('lyrics', { id: numberId(p.id) });
      return { lyric: result.mainText || '', translation: '' };
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
      const known = ['无效请求','无效的波点编号','波点当前地区不可播放','请先登录波点','波点请求失败','无法获取波点二维码','无法生成波点二维码','请先生成二维码','请输入搜索内容','波点播放地址不可用或不安全','不支持的请求'];
      send(request && Number.isSafeInteger(request.id) ? request.id : null, { ok: false, error: known.includes(error.message) ? error.message : '波点请求失败' });
    }
  });
});
