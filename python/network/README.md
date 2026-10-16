# Single-file HTTP server

Run with Python 3 (no third-party dependencies), from this directory:

```powershell
python .\serve_file.py "C:\path\to\document.pdf"
```

- `http://127.0.0.1:8000/file` serves the file.
- `http://127.0.0.1:8000/redirect` returns a `302` redirect to `/file`.

Both endpoints support `GET` and `HEAD`. Other paths return `404`; the server
does not expose a directory listing or other files. File responses include
`Content-Type` inferred from the filename extension, `Content-Length`, and
`Last-Modified`. Unknown extensions use `application/octet-stream`. Compressed
extensions such as `.txt.gz` also set `Content-Encoding` when recognized.
File contents are streamed without loading the entire file into memory.

Use `--port 9000` to change the port, or `--port 0` to select a free port;
the selected URLs are printed at startup. The server binds only to localhost
by default. To allow access from other machines, use `--host 0.0.0.0` and
connect using the machine's actual address. This development utility has no
authentication or TLS; only expose it on a trusted network. Stop with Ctrl+C.

Run the tests from this directory:

```powershell
python -m unittest discover -s . -p "test_serve_file.py"
```
