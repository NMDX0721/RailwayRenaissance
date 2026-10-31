"""Reject comparisons made against another recording or source revision."""
import hashlib
from pathlib import Path
import tempfile
import unittest

from audit_lyricsmith_ui import validate_comparison


class ComparisonIdentityTests(unittest.TestCase):
    def test_current_identity_and_stale_inputs(self):
        with tempfile.TemporaryDirectory() as directory:
            audio = Path(directory) / 'track.mp3'
            sheet = Path(directory) / 'sheet.json'
            audio.write_bytes(b'recording A')
            sheet.write_bytes(b'lyrics revision A')
            report = dict(id='track', audioSha256=hashlib.sha256(audio.read_bytes()).hexdigest(),
                          sourceSha256=hashlib.sha256(sheet.read_bytes()).hexdigest())
            validate_comparison(report, 'track', audio, sheet)
            for key in report:
                with self.subTest(key=key), self.assertRaises(ValueError):
                    validate_comparison({**report, key: 'different'}, 'track', audio, sheet)
            audio.write_bytes(b'recording B')
            with self.assertRaises(ValueError):
                validate_comparison(report, 'track', audio, sheet)


if __name__ == '__main__':
    unittest.main()
