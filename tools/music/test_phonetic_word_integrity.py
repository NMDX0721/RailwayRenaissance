import copy
import json
from pathlib import Path
import unittest
from publish_phonetic_draft import ROOT, valid_word_intervals


class WordIntegrityTests(unittest.TestCase):
    def test_actual_unflagged_candidates_are_rejected(self):
        path = ROOT/'tools/music/alignment_review/phonetic/starlit_rails.local.sections.provider.boundaries.json'
        report = json.loads(path.read_text(encoding='utf-8'))
        runs = {r['role']:r['lines'] for r in report['rounds']}
        for role,number in [('original',25),('original',41),('vocals',24),('vocals',25)]:
            self.assertFalse(valid_word_intervals(runs[role][number-1]))
        self.assertTrue(valid_word_intervals(runs['vocals'][40]))

    def test_missing_empty_and_invalid_intervals(self):
        line = dict(start=1,end=5,words=[dict(start=1,end=2),dict(start=3,end=4)])
        self.assertTrue(valid_word_intervals(line))
        for field,value in [('start',1.5),('start',float('nan')),('end',6),('end',3)]:
            changed = copy.deepcopy(line)
            changed['words'][1][field] = value
            self.assertFalse(valid_word_intervals(changed))
        self.assertFalse(valid_word_intervals({**line,'words':[]}))
        self.assertFalse(valid_word_intervals({**line,'start':float('nan')}))
        self.assertFalse(valid_word_intervals({**line,'end':float('inf')}))


if __name__ == '__main__':
    unittest.main()
