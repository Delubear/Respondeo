"""Verify published assets without executing application code or contacting the network."""

import argparse
import base64
import hashlib
from html.parser import HTMLParser
import json
from pathlib import Path, PurePosixPath
import re
from urllib.parse import unquote, urljoin, urlsplit
from zipfile import ZipFile


SHARED_PROJECTS = ("Respondeo.UI", "Respondeo.Content", "Respondeo.Content.Summa", "Respondeo.Bible")
CSS_URL = re.compile(r"url\(\s*['\"]?([^'\")\s]+)['\"]?\s*\)|@import\s+['\"]([^'\"]+)['\"]")
JS_IMPORT = re.compile(r"(?:\bimport\s*\(\s*|\bfrom\s+|\bimport\s+)['\"]([^'\"]+)['\"]")
MODULE_PATH = re.compile(r"['\"](\.?/?_content/Respondeo\.UI/[^'\"]+\.js)['\"]")


def require(condition, message):
	if not condition:
		raise ValueError(message)


class Artifact:
	def __init__(self, path, host):
		self.host = host
		self.archive = ZipFile(path) if host == "android" else None
		self.root = Path(path)
		if self.archive:
			entries = [item.filename for item in self.archive.infolist() if not item.is_dir()]
			require(len(entries) == len(set(entries)), "APK contains duplicate archive entries")
			prefix = "assets/wwwroot/"
			self.names = {name[len(prefix):] for name in entries if name.startswith(prefix)}
		else:
			self.names = {p.relative_to(self.root).as_posix() for p in self.root.rglob("*") if p.is_file()}
		require("index.html" in self.names, f"{host}: missing published index.html")
		maps = re.findall(r'<script\s+type="importmap">(.*?)</script>', self.text("index.html"), re.S)
		require(len(maps) <= 1, "Multiple import maps are not supported by this packaging contract")
		self.import_map = json.loads(maps[0]) if maps else {}
		require(not self.import_map.get("scopes"), "Scoped import maps require an explicit packaging contract update")
		self.imports = {urljoin("https://packaging.test/", key): value for key, value in self.import_map.get("imports", {}).items()}

	def close(self):
		if self.archive:
			self.archive.close()

	def read(self, name):
		require(name in self.names, f"{self.host}: missing asset {name}")
		return self.archive.read("assets/wwwroot/" + name) if self.archive else self.root.joinpath(*PurePosixPath(name).parts).read_bytes()

	def text(self, name):
		return self.read(name).decode("utf-8-sig")

	def resolve(self, parent, reference):
		if reference.startswith(("data:", "#")):
			return None
		url = urlsplit(urljoin("https://packaging.test/" + parent, reference))
		require(url.netloc == "packaging.test" and url.scheme == "https", f"{self.host}: unexpected external asset {reference} in {parent}")
		name = unquote(url.path.lstrip("/"))
		require(name in self.names, f"{self.host}: {parent} references missing asset {name}")
		return name

	def module(self, parent, reference):
		url = urljoin("https://packaging.test/" + parent, reference)
		return self.resolve("index.html", self.imports[url]) if url in self.imports else self.resolve(parent, reference)


class Shell(HTMLParser):
	def __init__(self, html):
		super().__init__()
		self.references = []
		self.native = False
		self.feed(html)

	def handle_starttag(self, tag, attributes):
		attrs = dict(attributes)
		if tag == "html":
			self.native = attrs.get("data-host") == "native"
		if tag == "script" and attrs.get("src"):
			self.references.append(attrs["src"])
		if tag == "link" and attrs.get("href") and set(attrs.get("rel", "").split()) & {
			"stylesheet", "preload", "modulepreload", "icon", "apple-touch-icon", "manifest"
		}:
			self.references.append(attrs["href"])


def check_references(artifact):
	html = artifact.text("index.html")
	require("#[.{fingerprint}" not in html, f"{artifact.host}: unresolved fingerprint placeholder")
	shell = Shell(html)
	require(shell.native == (artifact.host == "android"), f"{artifact.host}: incorrect native host marker")
	references = [artifact.resolve("index.html", ref) for ref in shell.references]
	styles = "Respondeo.Maui.styles.css" if artifact.host == "android" else "Respondeo.styles.css"
	require(styles in references, f"{artifact.host}: missing CSS-isolation entry point")
	if artifact.host == "android":
		require("_framework/blazor.webview.js" in references, "Android WebView bootstrap is missing")
		require(not re.search(r"serviceWorker\s*\.\s*register\s*\(|cloudflareinsights|blazor\.webassembly", html),
				"Android shell contains browser-only bootstrap, service-worker registration, or analytics")
		require("service-worker.js" not in artifact.names, "Android unexpectedly packages a service worker")
	else:
		require(any(ref and re.fullmatch(r"_framework/blazor\.webassembly\.[a-z0-9]+\.js", ref) for ref in references),
				"WASM bootstrap is not fingerprinted")
		require("service-worker.js" in artifact.names, "Published PWA worker is missing")
	# Walk all packaged CSS/JS, not just eagerly linked files: this includes isolation imports and lazy modules.
	for name in sorted(artifact.names):
		if name.endswith(".css"):
			text = re.sub(r"/\*.*?\*/", "", artifact.text(name), flags=re.S)
			for match in CSS_URL.finditer(text):
				artifact.resolve(name, match[1] or match[2])
		elif name.startswith("_content/Respondeo.UI/") and name.endswith(".js"):
			for match in JS_IMPORT.finditer(artifact.text(name)):
				artifact.module(name, match[1])
	for reference, expected in artifact.import_map.get("integrity", {}).items():
		name = artifact.module("index.html", reference)
		digest = base64.b64encode(hashlib.sha256(artifact.read(name)).digest()).decode("ascii")
		require(expected == "sha256-" + digest, f"Import-map integrity mismatch: {reference}")
	for reference in artifact.imports:
		artifact.module("index.html", reference)
	for name in sorted(artifact.names):
		if name.startswith("_content/Respondeo.Content/") and name.endswith("/manifest.json"):
			manifest = json.loads(artifact.text(name))
			require(isinstance(manifest.get("files"), list) and manifest["files"], f"Empty content manifest: {name}")
			for entry in manifest["files"]:
				require(isinstance(entry, str), f"Unexpected manifest entry in {name}")
				artifact.resolve(name, entry)


def check_integrity(artifact):
	text = artifact.text("service-worker-assets.js")
	manifest = json.loads(text[text.index("{"):text.rindex("}") + 1])
	entries = manifest["assets"]
	require(entries, "PWA integrity inventory is empty")
	require(len({entry["url"] for entry in entries}) == len(entries), "Duplicate PWA inventory URLs")
	names = set()
	for entry in entries:
		name = artifact.resolve("index.html", entry["url"])
		digest = base64.b64encode(hashlib.sha256(artifact.read(name)).digest()).decode("ascii")
		require(entry["hash"] == "sha256-" + digest, f"PWA integrity mismatch: {name}")
		names.add(name)
	shell = Shell(artifact.text("index.html"))
	for reference in shell.references:
		require(artifact.resolve("index.html", reference) in names, f"Shell asset omitted from PWA inventory: {reference}")


def shared_sources(repository):
	for project in SHARED_PROJECTS:
		root = repository / "src" / project / "wwwroot"
		require(root.is_dir(), f"Missing canonical source assets: {root}")
		for path in sorted(root.rglob("*")):
			if path.is_file():
				yield "_content/" + project + "/" + path.relative_to(root).as_posix(), path


def check_shared(artifact, repository):
	count = 0
	for logical, path in shared_sources(repository):
		expected = hashlib.sha256(path.read_bytes()).digest()
		file = PurePosixPath(logical)
		pattern = re.compile(re.escape(str(file.with_suffix(""))) + r"\.[a-z0-9]+" + re.escape(file.suffix))
		candidates = [name for name in artifact.names if name == logical or pattern.fullmatch(name)]
		require(candidates, f"{artifact.host}: missing shared asset {logical}")
		for candidate in candidates:
			require(hashlib.sha256(artifact.read(candidate)).digest() == expected,
					f"{artifact.host}: shared bytes differ from source: {candidate}")
		if artifact.host == "android" and file.suffix in (".css", ".js"):
			require(logical in artifact.names, f"Android plain-name asset missing: {logical}")
		if file.suffix == ".js":
			resolved = artifact.module("index.html", "./" + logical)
			require(hashlib.sha256(artifact.read(resolved)).digest() == expected, f"Incorrect module mapping for {logical}")
		count += 1
	# Literal module URLs in .NET interop are runtime imports, not HTML references.
	for path in (repository / "src" / "Respondeo.UI").rglob("*"):
		if path.suffix in (".cs", ".razor"):
			for match in MODULE_PATH.finditer(path.read_text(encoding="utf-8-sig")):
				artifact.module("index.html", match[1])
	return count


def check_paper_colors(repository):
	css = (repository / "src" / "Respondeo.UI" / "wwwroot" / "css" / "base.css").read_text(encoding="utf-8-sig")
	native = (repository / "src" / "Respondeo.Maui" / "MauiNativeThemeSync.cs").read_text(encoding="utf-8-sig")
	for selector, field in ((r":root", "LightPaper"), (r'\[data-theme="dark"\]', "DarkPaper")):
		block = re.search(selector + r"\s*\{([^}]+)\}", css)
		require(block is not None, f"Missing CSS theme selector: {selector}")
		color = re.search(r"--paper:\s*(#[0-9a-fA-F]{6})\s*;", block[1])
		native_color = re.search(field + r'\s*=\s*Color\.FromArgb\("(#[0-9a-fA-F]{6})"\)', native)
		require(color and native_color and color[1].lower() == native_color[1].lower(), f"Native {field} differs from CSS --paper")


def verify(path, host, repository):
	artifact = Artifact(path, host)
	try:
		check_references(artifact)
		count = check_shared(artifact, repository)
		if host == "wasm":
			check_integrity(artifact)
		check_paper_colors(repository)
		print(f"{host}: verified shell, CSS/imports, content manifests, and {count} shared asset hashes")
	finally:
		artifact.close()


if __name__ == "__main__":
	parser = argparse.ArgumentParser(description=__doc__)
	parser.add_argument("--web", type=Path, help="Published wwwroot directory")
	parser.add_argument("--apk", type=Path, help="Final signed APK")
	parser.add_argument("--repository", type=Path, default=Path(__file__).resolve().parents[2])
	args = parser.parse_args()
	if not (args.web or args.apk):
		parser.error("Select --web and/or --apk")
	if args.web:
		verify(args.web, "wasm", args.repository)
	if args.apk:
		verify(args.apk, "android", args.repository)
