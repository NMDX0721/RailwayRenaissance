"""Small standard CTC Viterbi alignment, preserving blank/repeated-label constraints."""
import numpy as np


def align(log_prob, targets, blank):
    if not targets or len(log_prob)==0:raise ValueError('Empty CTC input')
    labels=np.full(2*len(targets)+1,blank,dtype=np.int64)
    labels[1::2]=targets
    count=len(labels);previous=np.full(count,-np.inf,dtype=np.float64)
    previous[0]=log_prob[0,blank];previous[1]=log_prob[0,targets[0]]
    back=np.zeros((len(log_prob),count),dtype=np.int8)
    skip=np.zeros(count,dtype=bool)
    skip[2:]=(labels[2:]!=blank)&(labels[2:]!=labels[:-2])
    for frame in range(1,len(log_prob)):
        one=np.r_[-np.inf,previous[:-1]]
        two=np.r_[-np.inf,-np.inf,previous[:-2]]
        two[~skip]=-np.inf
        candidates=np.stack([previous,one,two])
        steps=candidates.argmax(axis=0)
        previous=candidates[steps,np.arange(count)]+log_prob[frame,labels]
        back[frame]=steps
    state=count-1 if previous[-1]>previous[-2] else count-2
    score=float(previous[state])
    if not np.isfinite(score):raise ValueError('No valid CTC path')
    states=np.empty(len(log_prob),dtype=np.int64)
    for frame in range(len(log_prob)-1,-1,-1):
        states[frame]=state
        if frame:state-=int(back[frame,state])
    spans=[]
    for index,token in enumerate(targets):
        frames=np.flatnonzero(states==index*2+1)
        if not len(frames):raise ValueError('Unvisited target label')
        spans.append(dict(token=int(token),start=int(frames[0]),end=int(frames[-1])+1,
                          score=float(np.exp(log_prob[frames,token]).mean())))
    return spans,score
