"""Exercise the real shell-composition target with isolated inputs."""

from html.parser import HTMLParser
from pathlib import Path
import shutil
import subprocess
import tempfile
import unittest
import xml.etree.ElementTree as ET


ROOT = Path(__file__).resolve().parents[2]


class ShellTags(HTMLParser):
	def __init__(self, html):
		super().__init__()
		self.tags = []
		self.feed(html)

	def handle_starttag(self, name, attrs):
		self.tags.append((name, dict(attrs)))

	def handle_startendtag(self, name, attrs):
		self.handle_starttag(name, attrs)


class HostShellTests(unittest.TestCase):
	def setUp(self):
		self.temporary = tempfile.TemporaryDirectory(prefix="respondeo-shell-")
		self.addCleanup(self.temporary.cleanup)
		self.root = Path(self.temporary.name)
		self.fragments = self.root / "fragments.xml"
		shutil.copyfile(ROOT / "build" / "ShellFragments.xml", self.fragments)
		self.project = self.root / "fixture.proj"
		project = ET.Element("Project")
		properties = ET.SubElement(project, "PropertyGroup")
		for name, value in (("BaseIntermediateOutputPath", "obj"), ("Configuration", "Release"), ("TargetFramework", "net10.0")):
			ET.SubElement(properties, name).text = value
		ET.SubElement(project, "Target", Name="AssignTargetPaths")
		discovery = ET.SubElement(project, "Target", Name="ResolveProjectStaticWebAssets", BeforeTargets="AssignTargetPaths")
		ET.SubElement(discovery, "Error", Condition="!Exists('$(HostShellPath)')", Text="Asset discovery ran before shell generation")
		ET.SubElement(project, "Import", Project=str(ROOT / "build" / "ComposeHostShell.targets"))
		overrides = ET.SubElement(project, "PropertyGroup")
		ET.SubElement(overrides, "HostShellFragments").text = str(self.fragments)
		ET.ElementTree(project).write(self.project, encoding="utf-8")
		self.template = self.root / "index.template.html"
		self.output = self.root / "obj" / "Release" / "net10.0" / "host-shell" / "index.html"

	def select_host(self, native=False):
		host = "Respondeo.Maui" if native else "Respondeo.Wasm"
		shutil.copyfile(ROOT / "src" / host / "index.template.html", self.template)

	def generate(self, native=False, success=True):
		result = subprocess.run([
			"dotnet", "msbuild", str(self.project), "-t:AssignTargetPaths", "-v:quiet", "-nologo",
			"-p:UseMaui=" + str(native).lower(),
		], cwd=ROOT, text=True, capture_output=True, timeout=60, check=False)
		if success:
			self.assertEqual(0, result.returncode, result.stdout + result.stderr)
			return self.output.read_text(encoding="utf-8")
		self.assertNotEqual(0, result.returncode, "An invalid shell was accepted")
		return result.stdout + result.stderr

	def test_exact_fragment_content_order_and_host_specific_urls(self):
		for native in (False, True):
			with self.subTest(native=native):
				self.select_host(native)
				html = self.generate(native)
				self.assertNotIn("<!-- shared:", html)
				for fragment in ET.parse(self.fragments).getroot():
					expected = fragment.text.strip("\n")
					if native:
						expected = expected.replace("#[.{fingerprint}]", "")
					self.assertIn(expected, html)
				tags = ShellTags(html).tags
				styles = [attrs["href"] for name, attrs in tags if name == "link" and attrs.get("rel") == "stylesheet"]
				self.assertEqual(["_content/Respondeo.UI/css/" + name + ".css"
								  for name in ("base", "components", "shell", "layout", "content")], styles[:5])
				scripts = [attrs["src"] for name, attrs in tags if name == "script" and "src" in attrs]
				suffix = ".js" if native else "#[.{fingerprint}].js"
				self.assertEqual(["_content/Respondeo.UI/js/" + name + suffix for name in (
					"site", "summa-refs", "scroll-store", "browse-state", "install", "offline-packs", "masthead-drag")], scripts[:7])
				self.assertLess(html.index("window.respondeoTheme"), html.index(scripts[0]))
				self.assertLess(html.index("window.respondeoTextSize"), html.index("</head>"))
				if native:
					self.assertIn('data-host="native"', html)
					self.assertIn('src="_framework/blazor.webview.js" autostart="false"', html)
					for browser_only in ("serviceWorker", "cloudflareinsights", "blazor.webassembly", "og:title", "--blazor-load-percentage"):
						self.assertNotIn(browser_only, html)
				else:
					for browser_only in ("serviceWorker", "cloudflareinsights", "blazor.webassembly", "og:title", "--blazor-load-percentage"):
						self.assertIn(browser_only, html)
					self.assertNotIn('data-host="native"', html)
					self.assertNotIn("blazor.webview", html)

	def test_incremental_generation_retains_timestamp_and_matches_clean_output(self):
		self.select_host()
		first = self.generate()
		timestamp = self.output.stat().st_mtime_ns
		self.assertEqual(first, self.generate())
		self.assertEqual(timestamp, self.output.stat().st_mtime_ns)
		self.output.unlink()
		self.assertEqual(first, self.generate())
		self.assertFalse(self.output.read_bytes().startswith(b"\xef\xbb\xbf"))

	def test_one_shared_change_reaches_both_hosts_on_the_next_build(self):
		self.select_host()
		self.generate()
		self.fragments.write_text(self.fragments.read_text(encoding="utf-8").replace("Reloading usually", "Refreshing usually"), encoding="utf-8")
		for native in (False, True):
			with self.subTest(native=native):
				self.select_host(native)
				self.assertIn("Refreshing usually", self.generate(native))

	def test_template_change_reaches_the_next_incremental_build(self):
		self.select_host()
		self.generate()
		self.template.write_text(self.template.read_text(encoding="utf-8").replace("<title>Respondeo", "<title>Changed"), encoding="utf-8")
		self.assertIn("<title>Changed", self.generate())

	def test_missing_and_duplicate_markers_fail_instead_of_emitting_partial_shells(self):
		self.select_host()
		original = self.template.read_text(encoding="utf-8")
		for replacement in ("", "    <!-- shared:fonts -->\n    <!-- shared:fonts -->"):
			with self.subTest(replacement=replacement):
				self.template.write_text(original.replace("    <!-- shared:fonts -->", replacement), encoding="utf-8")
				self.assertIn("exactly one marker", self.generate(success=False))
				self.assertFalse(self.output.exists())

	def test_unknown_marker_is_rejected(self):
		self.select_host()
		with self.template.open("a", encoding="utf-8") as template:
			template.write("\n    <!-- shared:unknown -->\n")
		self.assertIn("unknown shared-fragment marker", self.generate(success=False))
		self.assertFalse(self.output.exists())

	def test_duplicate_fragment_names_are_rejected(self):
		self.select_host()
		fragments = ET.parse(self.fragments)
		fragments.getroot().append(ET.fromstring('<fragment name="fonts">duplicate</fragment>'))
		fragments.write(self.fragments, encoding="utf-8")
		self.assertIn("unique names", self.generate(success=False))

	def test_missing_input_fails_without_writing_output(self):
		self.select_host()
		self.fragments.unlink()
		self.generate(success=False)
		self.assertFalse(self.output.exists())


if __name__ == "__main__":
	unittest.main()
