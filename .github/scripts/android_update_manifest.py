"""Describe the already-verified APK for the native, user-approved update flow."""

import argparse
import hashlib
import json
from pathlib import Path


MAX_APK_BYTES = 512 * 1024 * 1024


def generate(apk: Path, version_code: int) -> dict:
	if not 1 <= version_code <= 2100000000:
		raise ValueError("Android version code must be between 1 and 2100000000")
	version = f"1.0.{version_code}"
	if apk.name != f"respondeo-{version}.apk":
		raise ValueError("The APK filename must match its release version")
	size = apk.stat().st_size
	if not 0 < size <= MAX_APK_BYTES:
		raise ValueError("The APK size must be between 1 byte and 512 MiB")
	with apk.open("rb") as stream:
		digest = hashlib.file_digest(stream, "sha256").hexdigest()
	return {
		"versionCode": version_code,
		"version": version,
		"packageId": "faith.respondeo.app",
		"url": f"https://github.com/Delubear/Respondeo/releases/download/v{version}/{apk.name}",
		"sha256": digest,
		"sizeBytes": size,
	}


if __name__ == "__main__":
	parser = argparse.ArgumentParser(description=__doc__)
	parser.add_argument("apk", type=Path)
	parser.add_argument("version_code", type=int)
	args = parser.parse_args()
	manifest = generate(args.apk, args.version_code)
	args.apk.with_name("android-update.json").write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")
