# Respondeo.AcceptanceTests (BDD / E2E)

Business-readable acceptance tests written in Gherkin ([Reqnroll](https://reqnroll.net)) and
driven through a real browser with [Playwright](https://playwright.dev/dotnet/).

## What they cover

- **Home.feature** — the start page shows entry-point cards and navigating opens a node.
- **BuildIdentityTests** — build details stay off the global footer and collapsed at the bottom of Thanks & sources.
  A published PWA reloads and navigates there offline, displaying the same full build identity as its packaged manifest.
- **StartupPreferencesTests** — the served shell initializes theme/text size before deferred scripts, including persisted settings,
  missing settings, and blocked local storage; shared error markup retains its accessible role and controls.
- **AppPackageHttpHandlerTests** — the exact native transport implementation handles in-memory package bytes, decoded paths,
  missing files, cancellation, and unexpected errors without networking.
- **ApkUpdateReleaseTests** — the native update parser and downloader reject invalid release identities, corrupt/truncated/oversized downloads,
  HTTP failures, and cancellation; verified bytes and progress are checked without network access. These do not exercise Android's permission or installer UI.
- **AndroidPackageTransportTests** (`Category=AndroidArtifact`) — an explicitly selected final APK supplies real manifests and all listed articles
  through the native transport. ZIP-backed tests do not exercise Android's package API or a running WebView.
- **AndroidLifecycleTests** (`Category=AndroidDevice`) — a dedicated emulator installs the selected APK and checks offline startup/content,
  rendered theme/text-size persistence, pins, repeated bottom-bar navigation with remembered scroll, and background/resume.
  The separately selected `AndroidUpgrade` case verifies an increasing-version, same-key in-place upgrade without clearing app data.
- **AndroidDeviceHelperTests** — deterministic coordinate, visibility, and label matching checks need no device or browser.
- **NativeNavigationTests** — native Back/link events mask the scrolling shell before route handling, including masthead exposure
  during scroll restoration; native page-entry motion is disabled while web fades and reduced-motion behavior are preserved.
  These DOM timing checks do not replace physical Android WebView verification.
- **StageReelSlidingTests** — the flat reel shows only the active card, without 3D transforms or card animations;
  arrows, keyboard, swipe handling, and dots work across screen sizes and reduced motion.
- **Breadcrumb.feature** — the breadcrumb grows as you go deeper and resets when you return to Start.
- **Sections.feature** — collapsible sections open/close and deep-link via the URL.
- **StageNavigation.feature** — stage URLs load directly, unknown paths show the not-found page, and the breadcrumb is rooted at the stage.
- **Summa.feature** — browsing the Summa remembers search, expanded parts, and scroll position when pressing Back, and resets when leaving the Summa area.
- **SummaQuestion.feature** — a question's articles open from the table of contents, from deep links, and toggle closed, keeping the URL in sync.
- **Theme.feature** — the light/dark theme toggle switches and persists.
- **Discover.feature** — the Discover pillar lists its sub-areas and opens prayers and articles.
- **Devotions.feature** — the devotions browser lists devotions, and opening one shows its intro and enters the praying guide.
- **PrayerGuide.feature** — the Guide to Prayers explains the prayer notation and inline prayer references render in full.
- **Saints.feature** — the saints browser lists saint cards and opening one shows the saint's profile.
- **ImmersivePraying.feature** — single prayers, devotions, and the Liturgy of the Hours: the pinned bottom bar (leave, EN/LA, text size, theme, guide), the site chrome stepping aside on phones, and Next/Back keeping the current step in view.
- **Bible.feature** — the Bible reader.
- **DeepLinks.feature** — deep links load their target directly.
- **Miracles.feature** — the miracles browser and articles.
- **QuickMenuPins.feature** — pinned help orbs in the quick menu.
- **Scrolling.feature** — navigation lands at the expected scroll position.
- **Smoke.feature** — a quick check that the app starts and renders.

These are intentionally few and focused on the core journey. Unit and component tests
(in `Respondeo.UnitTests`) cover the fine-grained logic.

## Running locally

1. Serve the app somewhere (dev server or a published build):

   ```powershell
   # Option A: dev server
   dotnet run --project ..\..\src\Respondeo.Wasm\Respondeo.Wasm.csproj

   # Option B: serve a publish output (matches CI)
   dotnet publish ..\..\src\Respondeo.Wasm\Respondeo.Wasm.csproj -c Release -o publish
   dotnet tool install --global dotnet-serve
   dotnet serve -d publish\wwwroot -p 5000 --fallback-file index.html
   ```

2. Point the tests at it and run:

   ```powershell
   $env:RESPONDEO_BASE_URL = "http://localhost:5000"
   dotnet test --filter 'Category!=AndroidArtifact&Category!=AndroidDevice'
   ```

   Set `$env:RESPONDEO_HEADED = "1"` to watch the browser.

   `BuildIdentityTests` requires Option B's published service worker and a secure origin (localhost or HTTPS).
   For a loopback base URL, it uses `respondeo.localhost` on the same port so the app's bare-localhost worker opt-out
   does not suppress production caching. Chromium resolves this trustworthy local subdomain to loopback.
   For the development server, also exclude published-worker tests:
   `dotnet test --filter 'Category!=Published&Category!=AndroidArtifact&Category!=AndroidDevice'`.

## Native package transport

These tests need no browser, HTTP server, emulator, or network. From the repository root:

```powershell
$env:RESPONDEO_APK_PATH = (Resolve-Path apk-out\faith.respondeo.app-Signed.apk).Path
dotnet test tests\Respondeo.AcceptanceTests -c Release --filter 'Category=AndroidArtifact|FullyQualifiedName~AppPackageHttpHandlerTests'
```

The APK category fails explicitly if `RESPONDEO_APK_PATH` is absent or invalid; it never silently skips artifact verification.
CI runs it in the Android publish action after signing and before artifact upload. The browser action excludes it.

## Native lifecycle and upgrade

Use an explicitly selected, disposable emulator, not a personal phone or an AVD containing valuable state.
The harness installs/replaces `faith.respondeo.app`, changes reader preferences/pins, backgrounds the app, and temporarily enables airplane mode.
It restores the original airplane/Wi-Fi settings, but leaves app state as tested. Physical-device serials are rejected.
No APK is rebuilt/resigned by the tests, no app data is cleared, and failed installs never retry with uninstall or downgrade flags.

From the repository root, with the emulator already booted:

```powershell
$env:RESPONDEO_ADB_PATH = 'C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe'
$env:RESPONDEO_ANDROID_SERIAL = 'emulator-5554'
$env:RESPONDEO_ANDROID_ALLOW_MUTATION = '1'
$env:RESPONDEO_ANDROID_RESULTS = (Join-Path $PWD 'TestResults\android')
$env:RESPONDEO_APK_PATH = (Resolve-Path apk-out\faith.respondeo.app-Signed.apk).Path
dotnet test tests\Respondeo.AcceptanceTests -c Release --filter 'Category=AndroidDevice&Category!=AndroidUpgrade'
```

To test an upgrade, start with an emulator that has no newer app installed. Select a real prior compatible APK signed with the same key:

```powershell
$env:RESPONDEO_ANDROID_BASELINE_APK = (Resolve-Path previous-release.apk).Path
dotnet test tests\Respondeo.AcceptanceTests -c Release --filter 'Category=AndroidUpgrade'
```

The candidate must have a strictly greater version code. Android itself enforces signing continuity during `adb install -r`.
Evidence includes both APK hashes/manifests for upgrades, observed version codes, API/fingerprint/WebView information, XML trees, PNG screenshots,
and app logs. Known UIAutomator startup/termination retries are bounded and recorded; other command failures fail the test.
Screens/logs are captured independently so unavailable accessibility does not suppress all diagnostics.
Release builds do not enable WebView debugging for these checks.

The native smoke measures actual lede enlargement (over 10%) and requires it to remain within two device pixels after lifecycle changes.
The repeated Miracles round trip requires the catalog anchor to return within 12 device pixels after both Android Back and the bottom-bar link.
It waits for that anchor, not the catalog heading, which can be offscreen at the restored position. These are settled-layout checks, not frame-level flicker detection.
`AndroidUpgrade` covers theme/text size and pins, not every stored progress format. See the
[physical-device release checklist](../../docs/deployment.md#physical-device-release-checklist) for the remaining release checks.

## Playwright browsers

The first run needs browsers installed:

```powershell
dotnet build
pwsh bin\Debug\net11.0\playwright.ps1 install --with-deps chromium
```

CI performs this automatically before running the suite.
