"""Transport regression: truncated bodies must retain bytes and resume exact ranges."""
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch
import download_alignment_model as downloader


class Response:
    status=206
    def __init__(self,data,start,end):
        self.headers={'Content-Range':f'bytes {start}-{end}/{end+1}'}
        self.data=data;self.reads=0
    def __enter__(self):return self
    def __exit__(self,*args):pass
    def read1(self,n):
        self.reads+=1
        if self.reads>1:raise ConnectionError('simulated early disconnect')
        return self.data[:25]


class ResumeTests(unittest.TestCase):
    def test_truncated_body_and_existing_prefix(self):
        payload=bytes(range(100));requested=[]
        def request(url,**headers):
            start,end=map(int,headers['Range'][6:].split('-'));requested.append(start)
            return Response(payload[start-1000:end-999],start,end)
        with tempfile.TemporaryDirectory() as folder:
            target=Path(folder);(target/'1000').write_bytes(payload[:10])
            with patch.object(downloader,'request',request),patch.object(downloader.time,'sleep'):
                result=downloader.ranged_part('https://example.invalid/model',target,1000,1099)
            self.assertEqual(result.read_bytes(),payload)
            self.assertEqual(requested,[1010,1035,1060,1085])

    def test_server_ignoring_range_is_rejected(self):
        response=Response(b'bad',0,2);response.status=200
        with tempfile.TemporaryDirectory() as folder:
            with patch.object(downloader,'request',return_value=response),patch.object(downloader.time,'sleep'):
                with self.assertRaisesRegex(RuntimeError,'stalled'):
                    downloader.ranged_part('https://example.invalid/model',Path(folder),0,99)


if __name__=='__main__':unittest.main()
