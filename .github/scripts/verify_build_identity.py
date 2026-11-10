"""Check a published identity before releasing it or comparing hosts."""

import argparse
import json
from pathlib import Path
import re


def verify(manifest, host, version, source, content=None):
	expected = {"host": host, "version": version, "sourceRevision": source, "buildKind": "ci"}
	for key, value in expected.items():
		if manifest.get(key) != value:
			raise ValueError(f"Build identity mismatch for {key}")
	if not re.fullmatch(r"[0-9a-f]{64}", manifest.get("contentSha256", "")):
		raise ValueError("Build identity has no valid content SHA-256")
	if content and manifest["contentSha256"] != content:
		raise ValueError("Android and browser content identities do not match")
	mode, revision = manifest.get("hoursMode"), manifest.get("hoursRevision", "")
	if mode == "sample":
		if revision != "sample":
			raise ValueError("Sample Hours content has an unexpected revision")
	elif mode != "licensed" or not re.fullmatch(r"[0-9a-fA-F]{40}(?:[0-9a-fA-F]{24})?", revision):
		raise ValueError("Licensed Hours content must identify its pinned commit")


if __name__ == "__main__":
	parser = argparse.ArgumentParser(description=__doc__)
	parser.add_argument("manifest", type=Path)
	parser.add_argument("--host", required=True, choices=("wasm", "android"))
	parser.add_argument("--version", required=True)
	parser.add_argument("--source", required=True)
	parser.add_argument("--content")
	args = parser.parse_args()
	identity = json.loads(args.manifest.read_text(encoding="utf-8"))
	verify(identity, args.host, args.version, args.source, args.content)
	print(f"content-sha256={identity['contentSha256']}")
	print(f"hours-revision={identity['hoursRevision']}")
