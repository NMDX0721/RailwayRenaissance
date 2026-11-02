// Logic regression only: media, canvas and DOM layout are mocked, not browser QA.
const fs=require('node:fs'),vm=require('node:vm'),assert=require('node:assert/strict');
const html=fs.readFileSync('tools/music/alignment_review/index.html','utf8');
const script=html.split('<script>')[1].split('</script>')[0];
new vm.Script(script);
class Element {
 constructor(){this.value='';this.checked=false;this.children=[];this.style={};this.dataset={};this.classList={toggle(){}};this.paused=true;this.currentTime=0;this.files=[];this.tagName='DIV'}
 get valueAsNumber(){return this.value===''?NaN:Number(this.value)}
 setAttribute(){} append(e){this.children.push(e)} replaceChildren(){this.children=[]} click(){this.onclick?.({})}
 getBoundingClientRect(){return {width:800,height:175,left:0}}
 getContext(){return new Proxy({},{get:()=>()=>{}})}
 setPointerCapture(){} pause(){this.paused=true} play(){this.paused=false;return Promise.resolve()}
}
const elements=new Map(),storage=new Map();
for(const m of html.matchAll(/id="([^"]+)"[^>]*>/g)){const e=new Element();e.value=m[0].match(/value="([^"]+)"/)?.[1]||'';elements.set(m[1],e)}
elements.get('speed').value='1';
const context=vm.createContext({console,document:{getElementById:id=>elements.get(id),createElement:()=>new Element(),querySelectorAll:()=>[],addEventListener(){}},window:{addEventListener(){}},localStorage:{setItem:(k,v)=>storage.set(k,v),getItem:k=>storage.get(k)},URL:{createObjectURL:()=> 'blob:test',revokeObjectURL(){}},Blob,ResizeObserver:class{observe(){}},devicePixelRatio:1,requestAnimationFrame:()=>1,cancelAnimationFrame(){},setTimeout:()=>1,clearTimeout(){}});
vm.runInContext(script,context);
const run=s=>vm.runInContext(s,context);
assert.equal(run('Object.keys(waveforms).length'),4);
assert.ok(run('Object.values(waveforms).every(w=>w.peaks.length>1000&&w.peak>0)'));
run('preview()');assert.equal(run('stopAt'),Infinity);
run('audio.currentTime=current().end+1;updateClock()');assert.equal(run('audio.paused'),false);
run("$('bounded').checked=true;preview()");assert.equal(run('stopAt'),run('Math.min(report.duration,current().end+3)'));
run('audio.currentTime=stopAt+.02;updateClock()');assert.equal(run('audio.paused'),true);
run("$('repeat').checked=true;preview();audio.currentTime=stopAt+.02;updateClock()");assert.equal(run('audio.currentTime'),run('previewStart'));
run("$('repeat').checked=false;current().reviewed=true;current().check.checked=true;setBound('start',current().start+.1)");assert.equal(run('current().reviewed'),false);
const length=run('current().end-current().start');
run("beginDrag({button:0,preventDefault(){},stopPropagation(){},pointerId:1,clientX:300,currentTarget:$('region')},'move');moveDrag({pointerId:1,clientX:350});endDrag({pointerId:1})");
assert.ok(Math.abs(run('current().end-current().start')-length)<1e-8);
run('persist()');const saved=run('JSON.stringify(progress())');run('render()');assert.equal(run('JSON.stringify(progress().lines)'),JSON.stringify(JSON.parse(saved).lines));
assert.throws(()=>run("restore({...progress(),reviewKey:'wrong'})"),/不匹配/);
assert.throws(()=>run("restore({...progress(),lines:progress().lines.map((r,i)=>i? r:{...r,start:'bad'})})"),/不正确/);
run("$('start').value='';$('start').oninput()");assert.equal(run("$('export').disabled"),true);assert.equal(run('progress().lines[selected].start'),null);
run('rows.forEach((r,i)=>{r.start=i*2;r.end=i*2+1;r.reviewed=true});validate()');assert.equal(run("$('export').disabled"),false);
run('audioVerified.set(report.id,false);validate()');assert.equal(run("$('export').disabled"),true);
run("download=(data,name)=>{window.downloaded={data,name}};$('save').click()");assert.equal(run('window.downloaded.data.humanListeningVerified'),false);
assert.equal(run('window.downloaded.data.status'),'partial_listening_review');
console.log('REVIEW_UI_LOGIC_OK: waveform payload; continuous/bounded/loop preview; drag; edit invalidation; autosave/restore; malformed import; empty time; audio mismatch guard; partial backup');
