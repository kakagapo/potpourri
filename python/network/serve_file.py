"""Serve one file at /file and redirect /redirect to it."""

import argparse
from functools import partial
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
import mimetypes
import os
from pathlib import Path
import shutil


class FileHandler(BaseHTTPRequestHandler):
    def __init__(self, *args, file_path, **kwargs):
        self.file_path = file_path
        super().__init__(*args, **kwargs)

    def do_GET(self):
        self._respond(send_body=True)

    def do_HEAD(self):
        self._respond(send_body=False)

    def _respond(self, send_body):
        path = self.path.partition("?")[0]
        if path == "/redirect":
            self.send_response(302)
            self.send_header("Location", "/file")
            self.send_header("Content-Length", "0")
            self.end_headers()
            return

        if path != "/file":
            self.send_error(404, "Not found")
            return

        try:
            source = self.file_path.open("rb")
        except FileNotFoundError:
            self.send_error(404, "File no longer exists")
            return
        except PermissionError:
            self.send_error(403, "File is not readable")
            return
        except OSError:
            self.send_error(500, "Unable to open file")
            return

        with source:
            metadata = os.fstat(source.fileno())
            content_type, encoding = mimetypes.guess_type(str(self.file_path))
            self.send_response(200)
            self.send_header("Content-Type", content_type or "application/octet-stream")
            if encoding:
                self.send_header("Content-Encoding", encoding)
            self.send_header("Content-Length", str(metadata.st_size))
            self.send_header(
                "Last-Modified", self.date_time_string(metadata.st_mtime)
            )
            self.end_headers()
            if send_body:
                shutil.copyfileobj(source, self.wfile)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("file", type=Path, help="File to serve")
    parser.add_argument("--host", default="127.0.0.1", help="Bind address (default: 127.0.0.1)")
    parser.add_argument("--port", type=int, default=8000, help="Port (default: 8000)")
    args = parser.parse_args()

    if not 0 <= args.port <= 65535:
        parser.error("--port must be between 0 and 65535")
    try:
        file_path = args.file.resolve(strict=True)
        if not file_path.is_file():
            parser.error("file must be a regular file")
        with file_path.open("rb"):
            pass
    except OSError as error:
        parser.error(f"cannot read file: {error}")

    handler = partial(FileHandler, file_path=file_path)
    try:
        server = ThreadingHTTPServer((args.host, args.port), handler)
    except OSError as error:
        parser.error(f"cannot start server: {error}")

    with server:
        host, port = server.server_address
        print(f"Serving {file_path}", flush=True)
        print(f"File: http://{host}:{port}/file", flush=True)
        print(f"Redirect: http://{host}:{port}/redirect", flush=True)
        try:
            server.serve_forever()
        except KeyboardInterrupt:
            print("\nStopped.")


if __name__ == "__main__":
    main()
