"""Select host validation from the changed paths reported by dorny/paths-filter."""

import json
import os
from pathlib import PurePosixPath


def classify_paths(paths):
	result = {"code-changed": False, "web-changed": False, "apk-changed": False}
	for path in paths:
		name = PurePosixPath(path).name
		documentation = (
			name in {"README.md", "DISCOVER.md", "INQUIRY.md", "TODO.md"}
			or (path.startswith("docs/") and path.endswith(".md"))
			or path == ".github/copilot-instructions.md"
			or path.startswith((".github/instructions/", ".github/prompts/"))
		)
		if documentation:
			continue
		result["code-changed"] = True
		if not path.startswith("src/Respondeo.Maui/"):
			result["web-changed"] = True
		native_acceptance = path.startswith((
			"tests/Respondeo.AcceptanceTests/Android",
			"tests/Respondeo.AcceptanceTests/Support/Android",
		)) or path in {
			"tests/Respondeo.AcceptanceTests/AppPackageHttpHandlerTests.cs",
			"tests/Respondeo.AcceptanceTests/ApkUpdateReleaseTests.cs",
			"tests/Respondeo.AcceptanceTests/Respondeo.AcceptanceTests.csproj",
		}
		if native_acceptance or (not path.startswith((
			"src/Respondeo.Wasm/",
			"tests/Respondeo.AcceptanceTests/",
			"tools/Respondeo.SitemapGenerator/",
		)) and path != ".github/scripts/prepare-pages-site.sh"):
			result["apk-changed"] = True
	return result


def select_checks(paths, event_name, before="", manual_apk=True):
	if event_name == "workflow_dispatch":
		return {"code-changed": True, "web-changed": True, "apk-changed": manual_apk}
	if before == "0" * 40:
		return {"code-changed": True, "web-changed": True, "apk-changed": True}
	return classify_paths(paths)


if __name__ == "__main__":
	selected = select_checks(
		json.loads(os.environ["CHANGED_FILES"]),
		os.environ["EVENT_NAME"],
		os.environ.get("BEFORE", ""),
		os.environ["MANUAL_APK"] == "true",
	)
	with open(os.environ["GITHUB_OUTPUT"], "a", encoding="utf-8") as output:
		for name, enabled in selected.items():
			print(f"{name}={str(enabled).lower()}", file=output)
