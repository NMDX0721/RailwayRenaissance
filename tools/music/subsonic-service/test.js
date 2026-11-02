'use strict';
const assert = require('node:assert/strict');
const http = require('node:http');
const { spawn } = require('node:child_process');
const readline = require('node:readline');
const server = http.createServer((req,res) => {
  const u = new URL(req.url, 'http://127.0.0.1');
  const ok = u.searchParams.get('u') === 'tester' && u.searchParams.has('t') && u.searchParams.has('s');
  const result = { status: ok ? 'ok' : 'failed' };
  if (u.pathname.endsWith('/search3.view')) result.searchResult3 = { song: [{ id: 'song:1', title: 'test', artist: 'artist', duration: 60 }] };
  if (u.pathname.endsWith('/getPlaylists.view')) result.playlists = { playlist: [{ id:'list:1', name:'list', songCount:1 }] };
  if (u.pathname.endsWith('/getPlaylist.view')) result.playlist = { entry: [{ id:'song:1', title:'test', duration:60 }] };
  res.setHeader('content-type','application/json'); res.end(JSON.stringify({ 'subsonic-response': result }));
});
server.listen(0, '127.0.0.1', async () => {
  const child = spawn(process.execPath, [require('node:path').join(__dirname, 'bridge.js')], { stdio:['pipe','pipe','ignore'] });
  let id = 0; const waiting = new Map();
  readline.createInterface({ input: child.stdout }).on('line', line => { const r=JSON.parse(line); waiting.get(r.id)?.(r); waiting.delete(r.id); });
  const call = (method, params={}) => new Promise(resolve => { const n=++id; waiting.set(n,resolve); child.stdin.write(JSON.stringify({id:n,method,params})+'\n'); });
  try {
    const host = `http://127.0.0.1:${server.address().port}`;
    assert.equal((await call('connect',{host,username:'bad',password:'secret'})).ok,false);
    assert.equal((await call('status')).result.loggedIn,false);
    assert.equal((await call('connect',{host,username:'tester',password:'secret'})).result.loggedIn,true);
    const search=await call('search',{keywords:'test'}); assert.equal(search.result.tracks.length,1);
    const lists=await call('playlists'); assert.equal(lists.result.playlists.length,1);
    const songs=await call('playlist_tracks',{id:lists.result.playlists[0].id}); assert.equal(songs.result.tracks[0].id,search.result.tracks[0].id);
    const audio=await call('resolve',{id:search.result.tracks[0].id}); assert.equal(audio.result.localHttp,true); assert.equal(new URL(audio.result.url).searchParams.has('p'),false);
    assert.equal((await call('resolve',{id:'0000000000000000~dGVzdA'})).ok,false);
    assert.equal((await call('logout')).result.loggedIn,false);
    process.stdout.write('SUBSONIC_MOCK_OK: authentication, search, playlists, source identity, stream and logout\n');
  } catch (e) { process.stderr.write('SUBSONIC_MOCK_FAILED\n'); process.exitCode=1; }
  finally { child.kill(); server.close(); }
});
