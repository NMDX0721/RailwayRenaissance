import itertools
import unittest
import numpy as np
from korean_ctc_path import align


def collapse(path,blank):
    result=[];previous=None
    for label in path:
        if label!=previous and label!=blank:result.append(label)
        previous=label
    return result


class CtcPathTests(unittest.TestCase):
    def test_against_exhaustive_paths(self):
        rng=np.random.default_rng(812)
        for targets in [[1],[1,2],[1,1],[2,1,2]]:
            scores=rng.normal(size=(5,3));scores-=np.log(np.exp(scores).sum(axis=1,keepdims=True))
            best=max(sum(scores[t,token] for t,token in enumerate(path)) for path in itertools.product(range(3),repeat=5) if collapse(path,0)==targets)
            spans,score=align(scores,targets,0)
            self.assertAlmostEqual(score,best,places=6)
            self.assertEqual([s['token'] for s in spans],targets)
            self.assertTrue(all(s['end']>s['start'] for s in spans))
            if targets==[1,1]:self.assertLess(spans[0]['end'],spans[1]['start'])

    def test_insufficient_frames(self):
        with self.assertRaisesRegex(ValueError,'No valid'):align(np.zeros((1,3)),[1,1],0)


if __name__=='__main__':unittest.main()
