'use strict';
const crypto = require('node:crypto');
const readline = require('node:readline');
const { XMLParser } = require('fast-xml-parser');
let config = null;
const parser = new XMLParser({ removeNSPrefix: true, ignoreAttributes: true, isArray: name => name === 'response' || name === 'propstat' });
const send = (id, payload) => process.stdout.write(JSON.stringify({ id, ...payload }) + '\n');
function rootOf(raw) {
  const url = new URL(String(raw || '').trim());
  if (url.username || url.password || url.search || url.hash) throw new Error('服务地址无效');
  const local = ['localhost', '127.0.0.1', '[::1]'].includes(url.hostname);
  if (url.protocol !== 'https:' && !(local && url.protocol === 'http:')) throw new Error('服务器需要 HTTPS');
  if (!url.pathname.endsWith('/')) url.pathname += '/';
  return url;
}
const identity = settings => crypto.createHash('sha256').update(settings.root.toString()).digest('hex').slice(0,16);
const encode = (settings, url) => identity(settings) + '~' + Buffer.from(url.pathname, 'utf8').toString('base64url');
function safeUrl(settings, href) {
  const url = new URL(href, settings.root);
  if (url.origin !== settings.root.origin || !url.pathname.startsWith(settings.root.pathname) || url.search || url.hash || /%2f|%5c/i.test(url.pathname)) throw new Error('文件不属于当前目录');
  const decoded = decodeURIComponent(url.pathname), rootPath = decodeURIComponent(settings.root.pathname);
  if (decoded.includes('\\') || !decoded.startsWith(rootPath) || decoded.split('/').some(part => part === '.' || part === '..')) throw new Error('文件不属于当前目录');
  return url;
}
function decode(settings, value) {
  const match = /^([a-f0-9]{16})~([A-Za-z0-9_-]{1,300})$/.exec(String(value || ''));
  if (!match || match[1] !== identity(settings)) throw new Error('文件属于其他服务器');
  return safeUrl(settings, Buffer.from(match[2], 'base64url').toString('utf8'));
}
async function listing(settings, url) {
  const response = await fetch(url, { method:'PROPFIND', headers:{ Authorization:settings.authorization, Depth:'1', 'Content-Type':'application/xml' }, body:'<?xml version="1.0"?><propfind xmlns="DAV:"><prop><displayname/><resourcetype/></prop></propfind>', redirect:'error', signal:AbortSignal.timeout(20000) });
  if (response.status !== 207) throw new Error('无法读取 WebDAV 目录，请检查账号与权限');
  let size=0, chunks=[];
  for await (const chunk of response.body) { size += chunk.length; if (size > 2*1024*1024) throw new Error('WebDAV 目录过大'); chunks.push(chunk); }
  const xml = Buffer.concat(chunks).toString('utf8');
  if (/<!DOCTYPE|<!ENTITY/i.test(xml)) throw new Error('WebDAV 目录格式无效');
  const entries = parser.parse(xml)?.multistatus?.response;
  if (!Array.isArray(entries)) throw new Error('WebDAV 目录格式无效');
  const items=[];
  for (const entry of entries) {
    try {
      const item = safeUrl(settings, String(entry.href || ''));
      if (item.pathname.replace(/\/$/,'') === url.pathname.replace(/\/$/,'')) continue;
      const props = (entry.propstat || []).find(x => /\s200\s/.test(String(x.status || '')))?.prop;
      if (!props) continue;
      const folder = props.resourcetype && Object.prototype.hasOwnProperty.call(props.resourcetype,'collection');
      const name = String(props.displayname || decodeURIComponent(item.pathname.replace(/\/$/,'').split('/').pop()));
      const ext = name.split('.').pop().toLowerCase();
      if (!folder && !['mp3','wav','ogg'].includes(ext)) continue;
      items.push({ id:encode(settings,item), title:name, folder:!!folder, type:ext });
    } catch (_) { /* Ignore malformed or out-of-root entries. */ }
  }
  return items;
}
async function execute(method,p) {
  switch(method) {
    case 'ping': return { service:'webdav', protocol:1 };
    case 'connect': {
      const root=rootOf(p.host), user=String(p.username||'').trim(), password=String(p.password||'');
      if (!user || !password) throw new Error('请填写服务器、用户名和密码');
      const candidate={ root, authorization:'Basic '+Buffer.from(user+':'+password,'utf8').toString('base64'), user };
      await listing(candidate,root); config=candidate; return { loggedIn:true,nickname:user };
    }
    case 'status': if(!config)return {loggedIn:false}; await listing(config,config.root); return {loggedIn:true,nickname:config.user};
    case 'logout': config=null; return {loggedIn:false};
    case 'search': {
      if(!config)throw new Error('请先连接服务器'); const keyword=String(p.keywords||'').trim().toLowerCase();
      const items=await listing(config,config.root);
      return {tracks:items.filter(x=>!x.folder&&x.title.toLowerCase().includes(keyword)).map(x=>({id:x.id,title:x.title,artist:'',durationMs:0}))};
    }
    case 'playlists': {
      if(!config)throw new Error('请先连接服务器'); const items=await listing(config,config.root);
      return {playlists:items.filter(x=>x.folder).map(x=>({id:x.id,title:x.title,count:0}))};
    }
    case 'playlist_tracks': {
      if(!config)throw new Error('请先连接服务器'); const items=await listing(config,decode(config,p.id));
      return {tracks:items.filter(x=>!x.folder).map(x=>({id:x.id,title:x.title,artist:'',durationMs:0}))};
    }
    case 'resolve': {
      if(!config)throw new Error('请先连接服务器'); const url=decode(config,p.id);
      const type=url.pathname.split('.').pop().toLowerCase(); if(!['mp3','wav','ogg'].includes(type))throw new Error('音频格式不支持');
      return {url:url.toString(),type,authorization:config.authorization,localHttp:url.protocol==='http:',trial:false};
    }
    default: throw new Error('不支持的请求');
  }
}
let pending=Promise.resolve();
readline.createInterface({input:process.stdin,crlfDelay:Infinity}).on('line',line=>{ pending=pending.then(async()=>{
  let request;
  try { if(line.length>16384)throw new Error('无效请求'); request=JSON.parse(line.replace(/^\uFEFF/,''));
    if(!request||!Number.isSafeInteger(request.id)||typeof request.method!=='string')throw new Error('无效请求');
    send(request.id,{ok:true,result:await execute(request.method,request.params||{})});
  } catch(error) { const known=['无效请求','服务地址无效','服务器需要 HTTPS','文件不属于当前目录','文件属于其他服务器','无法读取 WebDAV 目录，请检查账号与权限','WebDAV 目录过大','WebDAV 目录格式无效','请填写服务器、用户名和密码','请先连接服务器','请输入搜索内容','音频格式不支持','不支持的请求']; send(request&&Number.isSafeInteger(request.id)?request.id:null,{ok:false,error:known.includes(error.message)?error.message:'WebDAV 连接失败'}); }
}); });
