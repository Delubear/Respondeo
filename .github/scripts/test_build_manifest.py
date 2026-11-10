"""Exercise the real MSBuild generator using isolated, invented content only."""

import hashlib
import json
from pathlib import Path
import shutil
import subprocess
import tempfile
import unittest
import xml.etree.ElementTree as ET

from verify_build_identity import verify


ROOT = Path(__file__).resolve().parents[2]
SOURCE = "a" * 40
HOURS_REVISION = "b" * 40


class BuildManifestTests(unittest.TestCase):
	def setUp(self):
		self.temporary = tempfile.TemporaryDirectory(prefix="respondeo-identity-")
		self.addCleanup(self.temporary.cleanup)
		self.root = Path(self.temporary.name)
		self.content = self.root / "src" / "Respondeo.Content" / "wwwroot" / "fixture.md"
		self.content.parent.mkdir(parents=True)
		self.content.write_bytes(b"Invented fixture\n")
		self.hours = self.root / "hours"
		self.hours.mkdir()
		(self.hours / "texts.json").write_bytes(b'{"fixture":true}\n')
		(self.hours / "texts.schema.json").write_bytes(b'{"ignored":true}\n')
		project = ET.Element("Project")
		ET.SubElement(project, "Import", Project=str(ROOT / "build" / "BuildManifest.targets"))
		properties = ET.SubElement(project, "PropertyGroup")
		ET.SubElement(properties, "_ManifestRoot").text = str(self.root)
		self.project = self.root / "fixture.proj"
		ET.ElementTree(project).write(self.project, encoding="utf-8")
		self.output = self.root / "obj" / "Release" / "net10.0" / "build-manifest.json"

	def generate(self, success=True, **properties):
		selected = {
			"Configuration": "Release",
			"TargetFramework": "net10.0",
			"BaseIntermediateOutputPath": "obj/",
			"HoursContentDir": str(self.hours),
			"BuildKind": "development",
			"BuildVersion": "0.0.0-dev",
			"BuildSourceRevision": "development",
		}
		selected.update(properties)
		command = ["dotnet", "msbuild", str(self.project), "-t:CreateBuildManifest", "-v:quiet", "-nologo"]
		command.extend(f"-p:{key}={value}" for key, value in selected.items() if value is not None)
		result = subprocess.run(command, cwd=ROOT, text=True, capture_output=True, timeout=60, check=False)
		if success:
			self.assertEqual(0, result.returncode, result.stdout + result.stderr)
			return json.loads(self.output.read_text(encoding="utf-8"))
		self.assertNotEqual(0, result.returncode, "An invalid content selection was accepted")
		return result.stdout + result.stderr

	def test_exact_canonical_digest_and_stable_output(self):
		manifest = self.generate()
		inventory = "respondeo-content-v1\n"
		for name, file in (
			("hours/content/texts.json", self.hours / "texts.json"),
			("src/Respondeo.Content/wwwroot/fixture.md", self.content),
		):
			inventory += name + "\0" + hashlib.sha256(file.read_bytes()).hexdigest() + "\n"
		self.assertEqual(hashlib.sha256(inventory.encode()).hexdigest(), manifest["contentSha256"])
		stamp = self.output.stat().st_mtime_ns
		self.assertEqual(manifest, self.generate())
		self.assertEqual(stamp, self.output.stat().st_mtime_ns)
		self.assertEqual(("sample", "sample"), (manifest["hoursMode"], manifest["hoursRevision"]))
		self.assertEqual(("0.0.0-dev", "development"), (manifest["version"], manifest["sourceRevision"]))

	def test_host_version_and_commit_do_not_change_content_digest(self):
		web = self.generate(BuildKind="ci", BuildVersion="1.0.42", BuildSourceRevision=SOURCE)
		native = self.generate(UseMaui="true", BuildKind="ci", BuildVersion="1.0.43", BuildSourceRevision="c" * 40)
		self.assertEqual("android", native["host"])
		self.assertEqual("wasm", web["host"])
		self.assertEqual(web["contentSha256"], native["contentSha256"])

	def test_changing_either_shared_or_hours_bytes_changes_digest(self):
		original = self.generate()["contentSha256"]
		self.content.write_bytes(b"Different fixture\n")
		changed = self.generate()["contentSha256"]
		self.assertNotEqual(original, changed)
		(self.hours / "texts.json").write_bytes(b'{"fixture":false}\n')
		self.assertNotEqual(changed, self.generate()["contentSha256"])

	def test_absolute_checkout_and_hours_directory_paths_do_not_affect_digest(self):
		original = self.generate()["contentSha256"]
		relocated = self.root / "other-checkout"
		shutil.copytree(self.root / "src", relocated / "src")
		other_hours = relocated / "licensed"
		shutil.copytree(self.hours, other_hours)
		self.project.write_text(self.project.read_text().replace(str(self.root), str(relocated)), encoding="utf-8")
		self.assertEqual(original, self.generate(HoursContentDir=str(other_hours))["contentSha256"])

	def test_schema_files_and_build_outputs_do_not_affect_digest(self):
		original = self.generate()["contentSha256"]
		(self.hours / "texts.schema.json").write_bytes(b"different schema")
		(self.root / "obj" / "transient.txt").write_bytes(b"different output")
		self.assertEqual(original, self.generate()["contentSha256"])

	def test_ci_requires_source_commit_and_release_version(self):
		self.assertIn("CI builds require", self.generate(False, BuildKind="ci"))
		self.assertIn("CI builds require", self.generate(False, BuildKind="ci", BuildSourceRevision=SOURCE))

	def test_licensed_ci_requires_and_records_pinned_commit(self):
		properties = dict(BuildKind="ci", BuildVersion="1.0.42", BuildSourceRevision=SOURCE, HoursLicensedContentDir=str(self.hours))
		self.assertIn("pinned commit", self.generate(False, **properties))
		manifest = self.generate(HoursContentRevision=HOURS_REVISION, **properties)
		self.assertEqual(("licensed", HOURS_REVISION), (manifest["hoursMode"], manifest["hoursRevision"]))
		self.assertNotIn(str(self.hours), json.dumps(manifest))
		self.assertEqual({"version", "sourceRevision", "host", "buildKind", "contentSha256", "hoursMode", "hoursRevision"}, set(manifest))

	def test_invalid_or_empty_licensed_selection_never_falls_back_to_samples(self):
		missing = str(self.root / "missing")
		self.assertIn("directory does not exist", self.generate(False, HoursLicensedContentDir=missing, HoursContentDir=missing))
		empty = self.root / "empty"
		empty.mkdir()
		self.assertIn("contains no JSON", self.generate(False, HoursLicensedContentDir=str(empty), HoursContentDir=str(empty)))
		self.assertIn("file does not exist", self.generate(False, HoursLicensedContentPath=str(self.root / "missing.json")))

	def test_legacy_single_file_selection_is_recorded_as_licensed(self):
		manifest = self.generate(HoursLicensedContentPath=str(self.hours / "texts.json"))
		self.assertEqual(("licensed", "development"), (manifest["hoursMode"], manifest["hoursRevision"]))
		self.assertEqual(self.generate()["contentSha256"], manifest["contentSha256"])

	def test_project_relative_licensed_directory_and_single_file_match_absolute_inputs(self):
		expected = self.generate()["contentSha256"]
		for selection in ({"HoursLicensedContentDir": "hours"}, {"HoursLicensedContentPath": "hours/texts.json"}):
			with self.subTest(selection=selection):
				manifest = self.generate(HoursContentDir=None, **selection)
				self.assertEqual("licensed", manifest["hoursMode"])
				self.assertEqual(expected, manifest["contentSha256"])

	def test_release_verifier_rejects_identity_and_content_drift(self):
		manifest = self.generate(BuildKind="ci", BuildVersion="1.0.42", BuildSourceRevision=SOURCE)
		verify(manifest, "wasm", "1.0.42", SOURCE, manifest["contentSha256"])
		for changes in ({"host": "android"}, {"sourceRevision": "c" * 40}, {"buildKind": "development"},
						{"contentSha256": "0" * 64}, {"hoursMode": "licensed"}, {"hoursRevision": "not-a-commit"}):
			with self.subTest(changes=changes), self.assertRaises(ValueError):
				verify(manifest | changes, "wasm", "1.0.42", SOURCE, manifest["contentSha256"])


if __name__ == "__main__":
	unittest.main()
