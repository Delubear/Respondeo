"""Positive and negative artifact fixtures for the packaging release gate."""

import base64
import hashlib
import json
from pathlib import Path
import tempfile
import unittest
from zipfile import ZipFile

from verify_packaging import verify


class PackagingTests(unittest.TestCase):
	def setUp(self):
		self.temp = tempfile.TemporaryDirectory()
		self.addCleanup(self.temp.cleanup)
		self.root = Path(self.temp.name)
		self.repository = self.root / "repository"
		self.web = self.root / "web"
		self.web.mkdir()
		self.sources = {
			"Respondeo.UI/css/base.css": b":root { --paper: #faf9f6; } [data-theme=\"dark\"] { --paper: #15140f; } body { src: url('../fonts/body.woff2'); }",
			"Respondeo.UI/fonts/body.woff2": b"fixture font bytes",
			"Respondeo.UI/js/lazy.js": b"export const value = 42;",
			"Respondeo.Content/discover/miracles/manifest.json": b'{"files":["fixture.md"]}',
			"Respondeo.Content/discover/miracles/fixture.md": b"# Fixture miracle",
			"Respondeo.Content.Summa/sample.txt": b"Summa fixture",
			"Respondeo.Bible/sample.txt": b"Bible fixture",
		}
		for name, body in self.sources.items():
			project, relative = name.split("/", 1)
			path = self.repository / "src" / project / "wwwroot" / relative
			path.parent.mkdir(parents=True, exist_ok=True)
			path.write_bytes(body)
		native = self.repository / "src" / "Respondeo.Maui" / "MauiNativeThemeSync.cs"
		native.parent.mkdir(parents=True)
		native.write_text('LightPaper = Color.FromArgb("#FAF9F6"); DarkPaper = Color.FromArgb("#15140F");', encoding="utf-8")
		interop = self.repository / "src" / "Respondeo.UI" / "Interop.cs"
		interop.write_text('new(js, "./_content/Respondeo.UI/js/lazy.js");', encoding="utf-8")
		self.assets = {"_content/" + name: body for name, body in self.sources.items()}
		lazy = "_content/Respondeo.UI/js/lazy.js"
		self.assets[lazy.replace(".js", ".abcdefghij.js")] = self.assets.pop(lazy)
		self.import_map = {"imports": {"./" + lazy: "./" + lazy.replace(".js", ".abcdefghij.js")}}
		self.assets.update({
			"index.html": self.shell(),
			"Respondeo.styles.css": b"@import '_content/Respondeo.UI/isolation.css';",
			"_content/Respondeo.UI/isolation.css": b"body { color: black; }",
			"_framework/blazor.webassembly.abcdefghij.js": b"// WASM fixture",
			"service-worker.js": b"// published worker fixture",
		})

	def shell(self):
		return ('<html><head><script type="importmap">' + json.dumps(self.import_map) + '</script>'
				'<link rel="stylesheet" href="Respondeo.styles.css">'
				'<link rel="stylesheet" href="_content/Respondeo.UI/css/base.css">'
				'<script src="_framework/blazor.webassembly.abcdefghij.js"></script></head></html>').encode()

	def write_web(self):
		manifest = {"assets": [{"url": name, "hash": "sha256-" + base64.b64encode(hashlib.sha256(body).digest()).decode()}
							   for name, body in self.assets.items()]}
		for name, body in self.assets.items():
			path = self.web / name
			path.parent.mkdir(parents=True, exist_ok=True)
			path.write_bytes(body)
		(self.web / "service-worker-assets.js").write_text("self.assetsManifest = " + json.dumps(manifest) + ";", encoding="utf-8")

	def assert_rejected(self, message):
		self.write_web()
		with self.assertRaisesRegex(ValueError, message):
			verify(self.web, "wasm", self.repository)

	def test_valid_fingerprinted_modules_and_plain_apk_assets(self):
		self.write_web()
		verify(self.web, "wasm", self.repository)
		assets = {"_content/" + name: body for name, body in self.sources.items()}
		assets.update({
			"index.html": b'<html data-host="native"><link rel="stylesheet" href="Respondeo.Maui.styles.css">'
						  b'<script src="_framework/blazor.webview.js"></script></html>',
			"Respondeo.Maui.styles.css": b"body { color: black; }",
			"_framework/blazor.webview.js": b"// WebView fixture",
		})
		apk = self.root / "fixture.apk"
		with ZipFile(apk, "w") as archive:
			for name, body in assets.items():
				archive.writestr("assets/wwwroot/" + name, body)
		verify(apk, "android", self.repository)

	def test_missing_font(self):
		del self.assets["_content/Respondeo.UI/fonts/body.woff2"]
		self.assert_rejected("references missing asset.*body.woff2")

	def test_missing_isolation_import(self):
		del self.assets["_content/Respondeo.UI/isolation.css"]
		self.assert_rejected("references missing asset.*isolation.css")

	def test_missing_dynamic_module(self):
		del self.assets["_content/Respondeo.UI/js/lazy.abcdefghij.js"]
		self.assert_rejected("missing asset.*lazy")

	def test_missing_article_from_manifest(self):
		del self.assets["_content/Respondeo.Content/discover/miracles/fixture.md"]
		self.assert_rejected("references missing asset.*fixture.md")

	def test_changed_shared_bytes_even_with_valid_inventory(self):
		self.assets["_content/Respondeo.UI/js/lazy.abcdefghij.js"] = b"wrong module"
		self.assert_rejected("shared bytes differ")

	def test_wrong_import_map_target(self):
		self.import_map["imports"]["./_content/Respondeo.UI/js/lazy.js"] = "./_content/Respondeo.UI/isolation.css"
		self.assets["index.html"] = self.shell()
		self.assert_rejected("Incorrect module mapping")

	def test_corrupt_service_worker_integrity(self):
		self.write_web()
		(self.web / "_framework" / "blazor.webassembly.abcdefghij.js").write_bytes(b"corrupt")
		with self.assertRaisesRegex(ValueError, "PWA integrity mismatch"):
			verify(self.web, "wasm", self.repository)

	def test_corrupt_import_map_integrity(self):
		self.import_map["integrity"] = {"./_content/Respondeo.UI/js/lazy.js": "sha256-wrong"}
		self.assets["index.html"] = self.shell()
		self.assert_rejected("Import-map integrity mismatch")

	def test_unexpected_remote_asset(self):
		self.assets["Respondeo.styles.css"] = b"@import 'https://other.test/style.css';"
		self.assert_rejected("unexpected external asset")

	def test_wrong_host_marker(self):
		self.assets["index.html"] = self.shell().replace(b"<html>", b'<html data-host="native">')
		self.assert_rejected("incorrect native host marker")

	def test_color_drift(self):
		path = self.repository / "src" / "Respondeo.Maui" / "MauiNativeThemeSync.cs"
		path.write_text('LightPaper = Color.FromArgb("#FFFFFF"); DarkPaper = Color.FromArgb("#15140F");', encoding="utf-8")
		self.assert_rejected("LightPaper differs")

	def test_native_browser_bootstrap_is_rejected(self):
		apk = self.root / "wrong.apk"
		with ZipFile(apk, "w") as archive:
			archive.writestr("assets/wwwroot/index.html", '<html data-host="native">'
							 '<link rel="stylesheet" href="Respondeo.Maui.styles.css">'
							 '<script src="_framework/blazor.webview.js"></script>'
							 '<script>navigator.serviceWorker.register("worker.js");</script></html>')
			archive.writestr("assets/wwwroot/Respondeo.Maui.styles.css", "")
			archive.writestr("assets/wwwroot/_framework/blazor.webview.js", "")
		with self.assertRaisesRegex(ValueError, "browser-only bootstrap"):
			verify(apk, "android", self.repository)

	def test_source_layout_is_not_apk_layout(self):
		apk = self.root / "wrong-layout.apk"
		with ZipFile(apk, "w") as archive:
			archive.writestr("wwwroot/index.html", "<html></html>")
		with self.assertRaisesRegex(ValueError, "missing published index.html"):
			verify(apk, "android", self.repository)

	def test_missing_shell_resource(self):
		del self.assets["Respondeo.styles.css"]
		self.assert_rejected("references missing asset.*Respondeo.styles.css")

	def test_missing_import_map_does_not_hide_unreachable_fingerprinted_module(self):
		self.import_map["imports"] = {}
		self.assets["index.html"] = self.shell()
		self.assert_rejected("references missing asset.*lazy.js")

	def test_shell_asset_missing_from_pwa_inventory(self):
		self.write_web()
		path = self.web / "service-worker-assets.js"
		text = path.read_text(encoding="utf-8")
		manifest = json.loads(text[text.index("{"):text.rindex("}") + 1])
		manifest["assets"] = [entry for entry in manifest["assets"] if entry["url"] != "Respondeo.styles.css"]
		path.write_text("self.assetsManifest = " + json.dumps(manifest) + ";", encoding="utf-8")
		with self.assertRaisesRegex(ValueError, "Shell asset omitted from PWA inventory"):
			verify(self.web, "wasm", self.repository)


if __name__ == "__main__":
	unittest.main()
