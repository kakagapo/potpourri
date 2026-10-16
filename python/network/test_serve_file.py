from contextlib import contextmanager
from functools import partial
import gzip
from http.client import HTTPConnection
from http.server import ThreadingHTTPServer
from pathlib import Path
import subprocess
import sys
from tempfile import TemporaryDirectory
from threading import Thread
import unittest
from urllib.request import urlopen

from serve_file import FileHandler


class QuietFileHandler(FileHandler):
    def log_message(self, format, *args):
        pass


class ServeFileTests(unittest.TestCase):
    @contextmanager
    def serving(self, name="sample.txt", content=b"Hello, world!\n"):
        with TemporaryDirectory() as directory:
            path = Path(directory) / name
            path.write_bytes(content)
            handler = partial(QuietFileHandler, file_path=path)
            with ThreadingHTTPServer(("127.0.0.1", 0), handler) as server:
                thread = Thread(target=server.serve_forever)
                thread.start()
                connection = HTTPConnection(*server.server_address, timeout=5)
                try:
                    yield connection, path
                finally:
                    connection.close()
                    server.shutdown()
                    thread.join()

    def test_file_formats_and_headers(self):
        cases = [
            ("sample.txt", b"hello", "text/plain", None),
            ("sample.html", b"<p>hello</p>", "text/html", None),
            ("sample.json", b'{"hello": true}', "application/json", None),
            ("sample.png", b"\x89PNG\r\n\x1a\n", "image/png", None),
            ("sample.unknown-extension", b"\x00\xff", "application/octet-stream", None),
            ("sample.txt.gz", gzip.compress(b"hello"), "text/plain", "gzip"),
            ("empty.txt", b"", "text/plain", None),
        ]
        for name, content, content_type, encoding in cases:
            with self.subTest(name=name), self.serving(name, content) as (client, _):
                client.request("GET", "/file?download=1")
                response = client.getresponse()
                self.assertEqual(response.status, 200)
                self.assertEqual(response.getheader("Content-Type"), content_type)
                self.assertEqual(response.getheader("Content-Encoding"), encoding)
                self.assertEqual(response.getheader("Content-Length"), str(len(content)))
                self.assertIsNotNone(response.getheader("Last-Modified"))
                self.assertEqual(response.read(), content)

    def test_redirect_and_follow(self):
        with self.serving() as (client, path):
            client.request("GET", "/redirect")
            response = client.getresponse()
            self.assertEqual(response.status, 302)
            self.assertEqual(response.getheader("Location"), "/file")
            self.assertEqual(response.getheader("Content-Length"), "0")
            self.assertEqual(response.read(), b"")
            with urlopen(f"http://{client.host}:{client.port}/redirect", timeout=5) as result:
                self.assertEqual(result.read(), path.read_bytes())
                self.assertTrue(result.url.endswith("/file"))

    def test_head(self):
        with self.serving() as (client, path):
            for endpoint, status in [("/file", 200), ("/redirect", 302)]:
                with self.subTest(endpoint=endpoint):
                    client.request("HEAD", endpoint)
                    response = client.getresponse()
                    self.assertEqual(response.status, status)
                    if endpoint == "/file":
                        self.assertEqual(response.getheader("Content-Type"), "text/plain")
                        self.assertEqual(
                            response.getheader("Content-Length"), str(path.stat().st_size)
                        )
                    self.assertEqual(response.read(), b"")

    def test_other_paths_are_not_served(self):
        with self.serving() as (client, _):
            for endpoint in ["/", "/sample.txt", "/../sample.txt", "/file/extra"]:
                with self.subTest(endpoint=endpoint):
                    client.request("GET", endpoint)
                    response = client.getresponse()
                    self.assertEqual(response.status, 404)
                    response.read()

    def test_removed_file(self):
        with self.serving() as (client, path):
            path.unlink()
            client.request("GET", "/file")
            response = client.getresponse()
            self.assertEqual(response.status, 404)
            response.read()

    def test_cli_rejects_invalid_arguments(self):
        script = str(Path(__file__).with_name("serve_file.py"))
        with TemporaryDirectory() as directory:
            for arguments in [
                [str(Path(directory) / "missing.txt")],
                [directory],
                [directory, "--port", "65536"],
            ]:
                with self.subTest(arguments=arguments):
                    result = subprocess.run(
                        [sys.executable, script, *arguments],
                        capture_output=True, text=True, timeout=5,
                    )
                    self.assertEqual(result.returncode, 2)
                    self.assertIn("error:", result.stderr)


if __name__ == "__main__":
    unittest.main()
