from types import SimpleNamespace
import json
import unittest
from align_ost_phonemes import ROOT, span_stays_ordered
from publish_phonetic_draft import valid_word_intervals


class RefinementOrderTests(unittest.TestCase):
    def test_repair_cannot_cross_neighbor_words(self):
        span = lambda a,b: [SimpleNamespace(start=a,end=b)]
        self.assertTrue(span_stays_ordered(span(10,20),10,20))
        self.assertFalse(span_stays_ordered(span(9,20),10,20))
        self.assertFalse(span_stays_ordered(span(10,21),10,20))
        self.assertFalse(span_stays_ordered([],10,20))

    def test_actual_repair_preserves_other_refinements(self):
        folder = ROOT/'tools/music/alignment_review/phonetic'
        parent = json.loads((folder/'starlit_rails.local.sections.provider.boundaries.json').read_text(encoding='utf-8'))
        fixed = json.loads((folder/'starlit_rails.local.sections.provider.boundaries.ordered.json').read_text(encoding='utf-8'))
        changed = []
        for before,after in zip(parent['rounds'],fixed['rounds']):
            for old,new in zip(before['lines'],after['lines']):
                self.assertTrue(valid_word_intervals(new))
                self.assertEqual(old['text'],new['text'])
                if valid_word_intervals(old):
                    self.assertEqual(old,new)
                else:
                    changed.append((before['role'],old['line']))
        self.assertEqual(changed,[('original',25),('original',41),('vocals',24),('vocals',25)])

    def test_nasal_candidate_changes_only_model_input_and_word_fit(self):
        folder = ROOT/'tools/music/alignment_review/phonetic'
        before = json.loads((folder/'starlit_rails.local.sections.provider.boundaries.ordered.json').read_text(encoding='utf-8'))
        after = json.loads((folder/'starlit_rails.local.sections.provider.boundaries.ordered.nasal.json').read_text(encoding='utf-8'))
        for old_run,new_run in zip(before['rounds'],after['rounds']):
            for index,(old,new) in enumerate(zip(old_run['lines'],new_run['lines'])):
                if index != 23:
                    self.assertEqual(old,new)
                    continue
                self.assertEqual((old['text'],old['start'],old['end']),(new['text'],new['start'],new['end']))
                self.assertTrue(valid_word_intervals(new))
                self.assertEqual(new['flags'],[])
                self.assertEqual(new['words'][1]['text'],'듣는')
                self.assertEqual(new['words'][1]['pronunciationInput'],'든는')


if __name__ == '__main__':
    unittest.main()
