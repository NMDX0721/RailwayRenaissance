"""Preserve native files and recover a complete candidate from observed UI, never Assets."""
import collections
import hashlib
import json
from pathlib import Path
import re
import shutil
from audit_lyricsmith_ui import ROOT

FILES = ['starlit_rails.lrc', 'starlit_rails (1).lrc', 'starlit_rails.srt', 'starlit_rails (1).srt']
FOLDER = ROOT / 'tools/music/alignment_review/lyricsmith'


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def seconds(value):
    parts = value.replace(',', '.').split(':')
    return sum(float(p)*60**i for i,p in enumerate(reversed(parts)))


def parse(path):
    text = path.read_text(encoding='utf-8-sig')
    if path.suffix == '.srt':
        result=[]
        for block in re.split(r'\n\s*\n',text.strip()):
            rows=block.splitlines()
            index=next(i for i,row in enumerate(rows) if '-->' in row)
            start,end=map(seconds,rows[index].split('-->'))
            result.append(dict(start=start,end=end,text='\n'.join(rows[index+1:])))
        return result
    result=[]
    for row in text.splitlines():
        match=re.fullmatch(r'\[([\d:.]+)\](.*)',row)
        if not match:continue
        chunks=list(re.finditer(r'<([\d:.]+)>([^<]*)',match[2]));words=[]
        for i,chunk in enumerate(chunks):
            word=chunk[2].strip()
            if word:
                if i+1==len(chunks):raise ValueError('Enhanced LRC lacks terminal timestamp')
                words.append(dict(text=word,start=seconds(chunk[1]),end=seconds(chunks[i+1][1])))
        result.append(dict(start=seconds(match[1]),end=words[-1]['end'],text=re.sub(r'<[^>]*>','',match[2]),words=words))
    return result


def stamp(value, srt=False):
    ms=round(value*1000)
    if srt:return f'{ms//3600000:02}:{ms//60000%60:02}:{ms//1000%60:02},{ms%1000:03}'
    return f'{ms//60000:02}:{ms//1000%60:02}.{ms%1000:03}'


def main():
    originals=FOLDER/'native';originals.mkdir(exist_ok=True)
    provenance=[];all_cues=[]
    for name in FILES:
        source=Path('E:/统一下载地址/Edge')/name
        target=originals/name
        if target.exists() and digest(target)!=digest(source):
            raise ValueError('Previously archived export changed; preserve separate versions')
        shutil.copy2(source,target)
        cues=parse(target)
        counts=collections.Counter((c['start'],c['end'],c['text']) for c in cues)
        provenance.append(dict(file=str(target.relative_to(ROOT)),sha256=digest(target),cueCount=len(cues),uniqueCount=len(counts),exactDuplicateCount=sum(n-1 for n in counts.values())))
        all_cues.extend(dict(c,file=name) for c in cues)
    source=ROOT/'Assets/Resources/Music/LyricSheets/starlit_rails.json'
    authored=json.loads(source.read_text(encoding='utf-8'))['plainText'].splitlines()
    visible=json.loads((FOLDER/'starlit_rails.visible-words.json').read_text(encoding='utf-8'))
    prior=json.loads((FOLDER/'starlit_rails.ui-candidate.review.json').read_text(encoding='utf-8'))
    if [r['text'] for r in visible['lines']]!=authored:raise ValueError('Visible source mismatch')
    evidence=[];lrc=['[ti:starlit_rails]','[by:automatic candidate recovered from observed LyricSmith UI; not listening verified]'];srt=[]
    for number,line in enumerate(visible['lines'],1):
        words=[dict(text=w['text'],start=w['start'],end=round(w['start']+w['duration'],3)) for w in line['words']]
        start,end=words[0]['start'],words[-1]['end']
        if ''.join(w['text'] for w in words)!=re.sub(r'\s+','',line['text']):raise ValueError('Changed word coverage')
        support=[];word_support=[]
        for cue in all_cues:
            if cue['text']!=line['text'] or abs(cue['start']-start)>.011 or abs(cue['end']-end)>.011:continue
            support.append(cue['file'])
            native=cue.get('words',[])
            if len(native)==len(words) and all(a['text']==b['text'] and abs(a['start']-b['start'])<=.011 and abs(a['end']-b['end'])<=.011 for a,b in zip(native,words)):
                word_support.append(cue['file'])
        checks=[]
        if not support:checks.append('no_native_export_for_this_occurrence')
        if not word_support:checks.append('word_endpoints_only_observed_in_rendered_ui')
        if any(w['end']<=w['start'] or w['start']<0 or w['end']>prior['duration'] for w in words):checks.append('invalid_word_interval')
        if any(b['start']<a['start'] or b['start']<a['end']-.002 for a,b in zip(words,words[1:])):checks.append('word_order_or_overlap')
        local=prior['lines'][number-1]['localComparison']
        if min(abs(c['startDifference']) for c in local)>.25:checks.append('local_start_conflict')
        evidence.append(dict(line=number,text=line['text'],start=start,end=end,words=words,nativeFiles=sorted(set(support)),nativeWordFiles=sorted(set(word_support)),flags=checks,localComparison=local))
        lrc.append(f'[{stamp(start)}]'+''.join(f'<{stamp(w["start"])}>'+w['text']+(' ' if i+1<len(words) else '') for i,w in enumerate(words))+f'<{stamp(end)}>')
        srt.append(f'{number}\n{stamp(start,True)} --> {stamp(end,True)}\n{line["text"]}')
    if any(b['start']<a['end']-.002 for a,b in zip(evidence,evidence[1:])):raise ValueError('Recovered source intervals overlap')
    audit=dict(id='starlit_rails',audioSha256=prior['audioSha256'],sourceSha256=digest(source),duration=prior['duration'],
               nativeFiles=provenance,lineCount=len(evidence),wordCount=sum(len(l['words']) for l in evidence),
               nativeSupportedLines=[l['line'] for l in evidence if l['nativeFiles']],
               nativeMissingLines=[l['line'] for l in evidence if not l['nativeFiles']],
               humanListeningVerified=False,published=False,lines=evidence)
    (FOLDER/'starlit_rails.native-audit.json').write_text(json.dumps(audit,ensure_ascii=False,indent=2),encoding='utf-8')
    review = dict(audit, model='LyricSmith native exports + rendered recovery / automatic candidate',
                  status='automatic_candidate_not_published', issues=[dict(line=l['line'],flags=l['flags']) for l in evidence if l['flags']])
    (FOLDER/'starlit_rails.recovered-candidate.review.json').write_text(json.dumps(review,ensure_ascii=False,indent=2),encoding='utf-8')
    (FOLDER/'starlit_rails.recovered-candidate.lrc').write_text('\n'.join(lrc)+'\n',encoding='utf-8')
    (FOLDER/'starlit_rails.recovered-candidate.srt').write_text('\n\n'.join(srt)+'\n',encoding='utf-8')
    print('LYRICSMITH_NATIVE_AUDIT_OK',json.dumps(dict(files=provenance,nativeCovered=len(audit['nativeSupportedLines']),missing=audit['nativeMissingLines'],recoveredLines=len(evidence)),ensure_ascii=False))


if __name__=='__main__':main()
