'use strict';
// Private loopback fixture used only by the opt-in Windows review.
const http = require('node:http'), fs = require('node:fs');
const audio = fs.readFileSync(process.argv[2]);
const xml='<?xml version="1.0"?><d:multistatus xmlns:d="DAV:"><d:response><d:href>/dav/</d:href><d:propstat><d:prop><d:resourcetype><d:collection/></d:resourcetype></d:prop><d:status>HTTP/1.1 200 OK</d:status></d:propstat></d:response><d:response><d:href>/dav/review.mp3</d:href><d:propstat><d:prop><d:displayname>review.mp3</d:displayname><d:resourcetype/></d:prop><d:status>HTTP/1.1 200 OK</d:status></d:propstat></d:response></d:multistatus>';
http.createServer((req,res)=>{
  const u=new URL(req.url,'http://127.0.0.1');
  if (u.pathname.startsWith('/dav/')) {
    if(req.headers.authorization!=='Basic '+Buffer.from('review:review').toString('base64')) {res.writeHead(401);res.end();return;}
    if(req.method==='PROPFIND'){res.writeHead(207,{'Content-Type':'application/xml'});res.end(xml);return;}
    res.writeHead(200,{'Content-Type':'audio/mpeg','Content-Length':audio.length});res.end(audio);return;
  }
  const ok=u.searchParams.get('u')==='review'&&u.searchParams.has('t')&&u.searchParams.has('s');
  if(u.pathname.endsWith('/stream.view')&&ok){res.writeHead(200,{'Content-Type':'audio/mpeg','Content-Length':audio.length});res.end(audio);return;}
  const result={status:ok?'ok':'failed'};
  if(u.pathname.endsWith('/search3.view'))result.searchResult3={song:[{id:'review:track',title:'Review',duration:60}]};
  if(u.pathname.endsWith('/getPlaylists.view'))result.playlists={playlist:[]};
  res.writeHead(200,{'Content-Type':'application/json'});res.end(JSON.stringify({'subsonic-response':result}));
}).listen(0,'127.0.0.1',function(){process.stdout.write(String(this.address().port)+'\n')});
