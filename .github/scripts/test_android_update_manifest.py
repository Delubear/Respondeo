"""Exercise metadata generation without using real APKs or the network."""

import hashlib
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch

from android_update_manifest import generate, MAX_APK_BYTES


class AndroidUpdateManifestTests(unittest.TestCase):
	def setUp(self):
		temporary = tempfile.TemporaryDirectory(prefix="respondeo-update-")
		self.addCleanup(temporary.cleanup)
		self.apk = Path(temporary.name) / "respondeo-1.0.42.apk"
		self.apk.write_bytes(b"Invented APK fixture")

	def test_exact_metadata_matches_verified_artifact(self):
		self.assertEqual({
			"versionCode": 42,
			"version": "1.0.42",
			"packageId": "faith.respondeo.app",
			"url": "https://github.com/Delubear/Respondeo/releases/download/v1.0.42/respondeo-1.0.42.apk",
			"sha256": hashlib.sha256(self.apk.read_bytes()).hexdigest(),
			"sizeBytes": len(self.apk.read_bytes()),
		}, generate(self.apk, 42))

	def test_rejects_invalid_version_codes(self):
		for code in (0, -1, 2100000001):
			with self.subTest(code=code), self.assertRaises(ValueError):
				generate(self.apk, code)

	def test_rejects_mismatched_filename(self):
		with self.assertRaises(ValueError):
			generate(self.apk, 43)

	def test_rejects_empty_artifact(self):
		self.apk.write_bytes(b"")
		with self.assertRaises(ValueError):
			generate(self.apk, 42)

	def test_rejects_oversized_artifact_before_reading(self):
		with patch.object(Path, "stat") as stat:
			stat.return_value.st_size = MAX_APK_BYTES + 1
			with self.assertRaises(ValueError):
				generate(self.apk, 42)


if __name__ == "__main__":
	unittest.main()
