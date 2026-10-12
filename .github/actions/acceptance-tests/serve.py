"""Static server that resolves URLs the way GitHub Pages does.

Lookup order for a request path: the exact file, then "<path>.html", then
"<path>/index.html", and finally 404.html served with HTTP 404. This matters
because the deploy writes per-route entry pages (e.g. discover.html next to a
discover/ folder), and generic servers redirect /discover to /discover/ instead.
"""

import http.server
import mimetypes
import os
import sys
from urllib.parse import unquote, urlsplit

ROOT = os.path.abspath(sys.argv[1])
PORT = int(sys.argv[2])

mimetypes.add_type("application/wasm", ".wasm")
mimetypes.add_type("application/octet-stream", ".dat")
mimetypes.add_type("application/octet-stream", ".blat")
mimetypes.add_type("application/json", ".json")
mimetypes.add_type("text/javascript", ".js")
mimetypes.add_type("text/javascript", ".mjs")
mimetypes.add_type("text/markdown", ".md")
mimetypes.add_type("application/manifest+json", ".webmanifest")


class PagesHandler(http.server.SimpleHTTPRequestHandler):
	def __init__(self, *args, **kwargs):
		super().__init__(*args, directory=ROOT, **kwargs)

	def log_message(self, format, *args):
		pass

	def _resolve(self):
		rel = unquote(urlsplit(self.path).path).lstrip("/")
		base = os.path.normpath(os.path.join(ROOT, rel))
		if not base.startswith(ROOT):
			return None
		for candidate in (base, base + ".html", os.path.join(base, "index.html")):
			if os.path.isfile(candidate):
				return candidate
		return None

	def _send(self, head_only):
		path = self._resolve()
		status = 200
		if path is None:
			path = os.path.join(ROOT, "404.html")
			status = 404
		with open(path, "rb") as f:
			body = f.read()
		ctype = mimetypes.guess_type(path)[0] or "application/octet-stream"
		self.send_response(status)
		self.send_header("Content-Type", ctype)
		self.send_header("Content-Length", str(len(body)))
		self.end_headers()
		if not head_only:
			self.wfile.write(body)

	def do_GET(self):
		self._send(False)

	def do_HEAD(self):
		self._send(True)


http.server.ThreadingHTTPServer(("", PORT), PagesHandler).serve_forever()
