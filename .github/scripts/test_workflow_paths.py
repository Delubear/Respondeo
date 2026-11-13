import unittest

from workflow_paths import classify_paths, select_checks


class WorkflowPathsTests(unittest.TestCase):
	def test_documentation_skips_both_hosts(self):
		for path in (
			"README.md", "src/Respondeo.Maui/README.md", "docs/deployment.md",
			"docs/deployment-streamlining-plan.md", "docs/guides/setup.md",
			"src/Respondeo.Content/DISCOVER.md", "src/Respondeo.Content/INQUIRY.md",
			".github/copilot-instructions.md", ".github/instructions/content-authoring.instructions.md",
			".github/prompts/add-prayer.prompt.md",
		):
			with self.subTest(path=path):
				self.assertEqual(classify_paths([path]), {
					"code-changed": False, "web-changed": False, "apk-changed": False,
				})

	def test_shared_changes_validate_both_hosts(self):
		for path in (
			"src/Respondeo.UI/Pages/Home.razor", "src/Respondeo.UI/wwwroot/js/reel.js",
			"src/Respondeo.Content/wwwroot/discover/prayers/hail-mary.md",
			"src/Respondeo.Content/wwwroot/inquiry/god.md", "docs/summa.txt",
			"Directory.Packages.props", "Directory.Build.props", "global.json",
			".github/actions/publish-android/action.yml", ".github/scripts/workflow_paths.py",
			".github/workflows/pr.yml", "tests/Respondeo.UnitTests/Services/ThemeServiceTests.cs",
			".github/actions/android-device-tests/action.yml",
			"tests/Respondeo.AcceptanceTests/AndroidLifecycleTests.cs",
			"tests/Respondeo.AcceptanceTests/Support/AndroidDevice.cs",
			"tests/Respondeo.AcceptanceTests/AndroidDeviceHelperTests.cs",
			"tests/Respondeo.AcceptanceTests/AppPackageHttpHandlerTests.cs",
			"tests/Respondeo.AcceptanceTests/ApkUpdateReleaseTests.cs",
			"tests/Respondeo.AcceptanceTests/Respondeo.AcceptanceTests.csproj",
		):
			with self.subTest(path=path):
				self.assertEqual(classify_paths([path]), {
					"code-changed": True, "web-changed": True, "apk-changed": True,
				})

	def test_android_only_changes_skip_web_publish(self):
		self.assertEqual(classify_paths(["src/Respondeo.Maui/MainPage.xaml"]), {
			"code-changed": True, "web-changed": False, "apk-changed": True,
		})

	def test_web_only_changes_skip_android_publish(self):
		for path in (
			"src/Respondeo.Wasm/Program.cs", "tests/Respondeo.AcceptanceTests/NavigationTests.cs",
			"tools/Respondeo.SitemapGenerator/Program.cs", ".github/scripts/prepare-pages-site.sh",
		):
			with self.subTest(path=path):
				self.assertEqual(classify_paths([path]), {
					"code-changed": True, "web-changed": True, "apk-changed": False,
				})

	def test_mixed_host_changes_validate_both(self):
		self.assertEqual(classify_paths([
			"src/Respondeo.Wasm/Program.cs", "src/Respondeo.Maui/MainPage.xaml", "README.md",
		]), {"code-changed": True, "web-changed": True, "apk-changed": True})

	def test_empty_change_set_skips_checks(self):
		self.assertEqual(classify_paths([]), {
			"code-changed": False, "web-changed": False, "apk-changed": False,
		})

	def test_manual_pr_validation_includes_android_by_default(self):
		self.assertEqual(select_checks([], "workflow_dispatch"), {
			"code-changed": True, "web-changed": True, "apk-changed": True,
		})

	def test_manual_deploy_requires_explicit_apk_selection(self):
		self.assertEqual(select_checks([], "workflow_dispatch", manual_apk=False), {
			"code-changed": True, "web-changed": True, "apk-changed": False,
		})

	def test_initial_push_validates_both_hosts(self):
		self.assertEqual(select_checks([], "push", before="0" * 40), {
			"code-changed": True, "web-changed": True, "apk-changed": True,
		})

	def test_normal_push_and_pr_use_the_same_classification(self):
		paths = ["src/Respondeo.Maui/MainPage.xaml"]
		for event in ("push", "pull_request"):
			with self.subTest(event=event):
				self.assertEqual(select_checks(paths, event), {
					"code-changed": True, "web-changed": False, "apk-changed": True,
				})


if __name__ == "__main__":
	unittest.main()
