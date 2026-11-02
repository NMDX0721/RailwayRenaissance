"""Regression checks for false acceptance in automatic evidence selection."""
import copy
import json
import unittest
from audit_blind_consensus import BASE, evaluate_line


class ConsensusTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.native=json.loads((BASE/'lyricsmith/starlit_rails.native-audit.json').read_text(encoding='utf-8'))
        p=json.loads((BASE/'phonetic/starlit_rails.local.sections.provider.boundaries.json').read_text(encoding='utf-8'))
        cls.rounds={r['role']:r['lines'] for r in p['rounds']}
        cls.windows=json.loads((BASE/'qwen/starlit_rails.original.blind-asr.json').read_text(encoding='utf-8'))['windows']

    def args(self,n):
        return copy.deepcopy((self.native['lines'][n-1],self.rounds['original'][n-1],self.rounds['vocals'][n-1],self.windows))

    def test_measured_scopes(self):
        for number,scope in [(4,'word_onsets_and_line_end_corroborated'),(14,'unresolved'),(26,'line_only_corroborated'),(41,'word_onsets_and_line_end_corroborated'),(46,'unresolved')]:
            with self.subTest(number=number):self.assertEqual(evaluate_line(*self.args(number))['scope'],scope)

    def test_no_exact_blind_text_rejects(self):
        line,original,vocals,windows=self.args(41)
        for w in windows:w['text']='unrelated recognized words'
        self.assertEqual(evaluate_line(line,original,vocals,windows)['scope'],'unresolved')

    def test_missing_native_words_downgrades(self):
        line,original,vocals,windows=self.args(4);line['nativeWordFiles']=[]
        self.assertEqual(evaluate_line(line,original,vocals,windows)['scope'],'line_only_corroborated')

    def test_weak_phonetic_word_rejects(self):
        line,original,vocals,windows=self.args(4);vocals['words'][0]['score']=.01
        self.assertEqual(evaluate_line(line,original,vocals,windows)['scope'],'unresolved')

    def test_word_drift_downgrades(self):
        line,original,vocals,windows=self.args(4);line['words'][1]['start']+=.8
        self.assertEqual(evaluate_line(line,original,vocals,windows)['scope'],'line_only_corroborated')

    def test_wrong_old_onset_needs_exclusion(self):
        line,original,vocals,windows=self.args(41)
        windows=[w for w in windows if w['start']>=184]
        self.assertEqual(evaluate_line(line,original,vocals,windows)['scope'],'unresolved')


if __name__=='__main__':unittest.main()
