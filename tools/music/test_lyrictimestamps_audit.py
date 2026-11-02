import copy
import unittest
from audit_lyrictimestamps import audit


class NativeAuditTests(unittest.TestCase):
    def setUp(self):
        self.words = [dict(word='one', start=1, end=2, confidence=.9, line_index=0),
                      dict(word='two', start=3, end=4, confidence=.8, line_index=1)]
        self.rounds = [dict(role='original', lines=[dict(start=1, end=2), dict(start=3, end=4)])]

    def test_complete_export_and_conflict(self):
        self.assertFalse(any(r['flags'] for r in audit(self.words, ['one', 'two'], 5, self.rounds)))
        changed = copy.deepcopy(self.rounds)
        changed[0]['lines'][1]['start'] = 2
        self.assertIn('no_local_start_agreement_within_250ms', audit(self.words, ['one', 'two'], 5, changed)[1]['flags'])

    def test_reject_missing_text_bad_time_and_index(self):
        cases = [self.words[:1]]
        for field, value in [('word', 'wrong'), ('start', float('nan')), ('start', 1.5),
                             ('end', 3), ('confidence', 2), ('line_index', 7)]:
            candidate = copy.deepcopy(self.words)
            candidate[1][field] = value
            cases.append(candidate)
        for candidate in cases:
            with self.subTest(candidate=candidate), self.assertRaises(ValueError):
                audit(candidate, ['one', 'two'], 5, self.rounds)

    def test_visible_evidence_never_fabricates_confidence(self):
        visible = copy.deepcopy(self.words)
        for word in visible:
            del word['confidence']
        with self.assertRaises(ValueError):
            audit(visible, ['one', 'two'], 5, self.rounds)
        rows = audit(visible, ['one', 'two'], 5, self.rounds, require_confidence=False)
        self.assertTrue(all('provider_confidence_not_visible' in row['flags'] for row in rows))
        self.assertTrue(all('confidence' not in word for row in rows for word in row['words']))


if __name__ == '__main__':
    unittest.main()
