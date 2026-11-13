# Deployment

The site is published to **GitHub Pages** by the `Deploy to GitHub Pages` workflow
([`.github/workflows/deploy.yml`](../.github/workflows/deploy.yml)) on pushes to `master` that change web-relevant files.
App-relevant pushes also build and release an Android APK. Manual runs always validate/deploy the website and release an APK
only when `release_apk` is selected; this preserves the existing release cadence.

It is served from the custom apex domain **`respondeo.faith`**, so the app lives at the domain root
and the authored `<base href="/" />` is used as-is (no base-href rewrite). The deploy writes a
`CNAME` file into the published output on every run to keep the custom-domain binding. GitHub
automatically redirects the old project URL (`delubear.github.io/Respondeo/`) to the custom domain.

## Validation and release gates

PRs and deployment runs share path selection in `.github/actions/changed-paths` and SDK/workload setup in
`.github/actions/setup-build`. Path rules are implemented and tested in `.github/scripts/workflow_paths.py` and
`.github/scripts/test_workflow_paths.py`. Documentation-only runs skip expensive builds, not the path-rule checks.

| Change | Shared unit/component tests | WASM publish and browser acceptance | Android Release publish, packaging, and emulator smoke |
| --- | --- | --- | --- |
| Shared UI/content, dependency, build, or workflow configuration | Yes | Yes | Yes |
| MAUI host only | Yes | No | Yes |
| WASM host, browser acceptance tests, or sitemap tooling only | Yes | Yes | No |
| READMEs, authoring instructions/prompts, or Markdown under docs | No | No | No |

Authored Inquiry/Discover Markdown is application content, not documentation, and validates both hosts.
Native acceptance tests/helpers, package transport tests, and the acceptance project file also validate both hosts;
browser-only acceptance files retain the web-only path. Other changes conservatively validate both hosts. Manual PR workflow runs validate both hosts.

The PR workflow uses read-only repository/PR permissions and does not receive release signing or licensed-content secrets.
Its Android action produces an `android-validation` artifact using a development debug key. Add **Android Release Validation**
to the repository's required PR checks alongside unit/component and browser acceptance checks to enforce it before merge.
This branch-protection setting must be configured in GitHub; editing workflow files alone does not require the check.

The trusted deployment workflow builds the signed public APK in `android-build`, with read-only repository permissions.
All four secrets are mandatory: `ANDROID_KEYSTORE_BASE64`, `ANDROID_KEYSTORE_PASSWORD`, `ANDROID_KEY_ALIAS`, and
`ANDROID_KEY_PASSWORD`. Never rotate the signing identity casually: a differently signed APK cannot update an existing installation.
The temporary keystore is removed after the build, including on failure.

The shared `.github/actions/publish-android` action checks that exactly one signed APK was produced and verifies its ZIP archive,
Android signature, ZIP alignment, and build identity before uploading it with `build-manifest.json` and `SHA256SUMS`.
The `android-release` job has release-write permission but no signing credentials or build step. It downloads that run's
`android-release` artifact, checks both checksums, and attaches the unchanged APK, manifest, and checksum file to the GitHub Release.
Applicable browser acceptance checks must also pass; skipped browser checks are allowed for MAUI-only changes.
Failed or canceled checks block APK publication.

Pages similarly deploys the published output already exercised by browser acceptance tests, rather than rebuilding it.
An Android build failure does not block a validated website deployment. Pages permissions are limited to the Pages deployment job.
APK structure/signature checks do not replace device testing for startup, app switching, system bars, or upgrade persistence.

### Native lifecycle gate

Both Android publication callers run `.github/actions/android-device-tests` after packaging/transport verification and before APK upload.
The action boots an accelerated API 36 x86_64 Pixel 7 emulator and runs only `Category=AndroidDevice&Category!=AndroidUpgrade`
from the existing acceptance project. It installs the exact signed artifact without rebuilding or resigning it, then rechecks `SHA256SUMS`.
The release keystore is already removed before emulator tooling starts. Browser acceptance explicitly excludes all `AndroidDevice` cases.
The Linux runner must expose `/dev/kvm`; the action grants device access directly and fails explicitly if acceleration is unavailable or inaccessible.

The focused smoke checks airplane-mode cold startup, real offline Miracles catalog/article loading, rendered theme/text-size persistence,
a pinned page, remembered browse position across two bottom-bar round trips, background/resume in both themes, and Android Back/relaunch from Home.
It measures settled content geometry, not intermediate frames: a successful scroll check cannot prove the masthead never flashed.
The original OS app-switch/reel rasterization issue remains a physical-device check, not an emulator acceptance claim.

Each Android job retains `<APK artifact name>-device-results`, including TRX output, device API/fingerprint/WebView version, selected APK SHA-256,
build identity, accessibility XML, screenshots, and app logs. Missing inputs and unexpected device failures fail explicitly.
The first hosted run still needs confirmation; local Windows emulator success is not evidence that the GitHub-hosted gate has run.

Upgrade verification is separate and opt-in because it requires a selected prior APK signed with the same stable key and a strictly newer candidate.
It uses `adb install -r`, never uninstall/clear-data/downgrade recovery. It verifies theme, rendered reading size, and a pinned page,
retaining both artifact identities and observed version codes. A controlled development-key version pair tests the harness only;
it does not prove a public release will update an existing release-signed installation or preserve every progress format.
See [native test commands](../tests/Respondeo.AcceptanceTests/README.md#native-lifecycle-and-upgrade).

### Physical-device release checklist

Use the actual final public APK and record its SHA-256/build details, phone model, Android API level, and WebView version.
Keep a short screen recording or screenshots/logs when a check fails. Do not clear existing data to make an upgrade pass.

- [ ] From a prior public same-key APK, create representative prayer/devotion/Hours progress, pins, and reader preferences.
  Install the newer public APK in place; verify those exact values and resume points remain.
- [ ] Cold-start offline and open Miracles, a prayer, the Summa, and the Bible; confirm real content, not just headings.
- [ ] Open a lower Miracles card and repeatedly use the bottom-bar arrow; confirm the remembered card position without a masthead flash.
  Repeat quickly, including Android Back/gesture behavior while reading and in dialogs.
- [ ] On Home in light and dark themes, swipe the reel, open Recents, switch to another app, lock/unlock, and return.
  Look specifically for the original flickering/rasterized cards and intermediate-page exposure.
- [ ] Check black status/navigation gutters and legible light icons in both themes, with gesture and three-button navigation where available.
- [ ] Force-stop/restart and repeat background/resume; verify preferences, pins, and reading progress persist.

These manual checks remain required even when all browser, package, and emulator gates pass.

### Packaging contracts

The shared browser acceptance action validates the downloaded site before testing it. The Android publish action validates the final signed APK
and runs package-transport tests before uploading it. Both use `.github/scripts/verify_packaging.py`; validation failures block the relevant release.

The verifier checks shell scripts/styles/fonts/icons, CSS URLs and isolation imports, literal shared JavaScript imports and .NET interop module URLs,
Discover/Inquiry manifest entries, and all four shared projects' static-asset bytes against their sources. Browser fingerprinted names are resolved
through emitted import maps or verified by SHA-256, not compared literally to native names. PWA inventory URLs and integrity hashes are checked as well.

Intentional differences are narrow: the website requires fingerprinted WASM bootstrap and a service worker; Android requires its native marker,
WebView bootstrap, and plain-name CSS/JS assets. Android must not package the PWA worker or include browser bootstrap, worker registration,
or Cloudflare analytics in its shell. Native light/dark paper constants must match the shared CSS tokens. Feature defaults remain the shared embedded
baseline described below; Phase 3 registration tests verify required settings and override precedence.

.NET 11 supplies the WASM bootstrap as a package static asset with an optional fingerprint. The WASM project makes that fingerprint required
before the SDK generates HTML, compressed assets, and the PWA inventory. GitHub Pages needs the fingerprinted physical file, not just an
ASP.NET Core endpoint alias; keep the packaging check strict rather than accepting an unversioned bootstrap.

The browser gate uses Playwright 1.64.1 and its bundled Chromium to support .NET 11's WASM exception handling. The former Chromium 131
cannot start this runtime. Browser cache keys include `Directory.Packages.props`, where the Playwright version is pinned.

The native tests compile the actual `AppPackageHttpHandler` source and replace only its package-file opener. In-memory tests cover URI decoding,
query stripping, byte delivery, missing files, cancellation, and unexpected errors. Artifact tests read real Inquiry, Miracles, and Prayers manifests
and every listed article from `assets/wwwroot/` in the selected APK, through that handler and without a network handler.
These checks do not exercise Android's `FileSystem` API, WebView rendering, lifecycle, or upgrade persistence. Static import checks cover literal URLs,
not arbitrary computed runtime paths; they complement, rather than replace, browser journeys and device verification.

From the repository root, validate previously published artifacts with:

```powershell
python .github\scripts\verify_packaging.py --web publish\wwwroot --apk apk-out\faith.respondeo.app-Signed.apk
$env:RESPONDEO_APK_PATH = (Resolve-Path apk-out\faith.respondeo.app-Signed.apk).Path
dotnet test tests\Respondeo.AcceptanceTests -c Release --filter 'Category=AndroidArtifact|FullyQualifiedName~AppPackageHttpHandlerTests'
```

Run the path-rule, build-identity, and packaging regression tests locally with the repository .NET SDK installed:

```powershell
python -m unittest discover -s .github\scripts -p 'test_*.py'
```

## Shared HTML startup

Edit [`build/ShellFragments.xml`](../build/ShellFragments.xml) for the ordered shared font preloads, stylesheet links, scripts,
inline theme/text-size initialization, and error UI. The fragment bodies are HTML inside XML CDATA; HTML entities stay unchanged.
Each host's `index.template.html` places these blocks with `<!-- shared:name -->` markers, each required exactly once.
Keep browser-only SEO, PWA registration/update UI, analytics, and download progress in
[`Respondeo.Wasm/index.template.html`](../src/Respondeo.Wasm/index.template.html). Keep the native marker, native loading UI, and WebView bootstrap in
[`Respondeo.Maui/index.template.html`](../src/Respondeo.Maui/index.template.html).

Both projects import [`build/ComposeHostShell.targets`](../build/ComposeHostShell.targets). Before build preparation and target-path assignment,
it composes `obj/<Configuration>/<TargetFramework>/host-shell/index.html` and links that generated content as `wwwroot/index.html`.
The SDK discovers it even on the first clean build; generated files are build outputs, not checked-in or hand-edited source.
Unchanged content is not rewritten. Missing inputs, duplicate/missing markers, and unknown markers fail the build.

The preference initializer stays inline in the head, ahead of the deferred scripts, preserving first-paint defaults and persisted settings.
The shared script list retains WASM fingerprint placeholders; composition removes those placeholders only for Android's plain-name assets.
The existing browser SDK fingerprinting, publish-time stylesheet rewriting, and PWA integrity resync still process the generated shell normally.
Neither deployment needs runtime template fetching or a new startup request. The public entry point and Android `HostPage` stay `wwwroot/index.html`.

`test_host_shell.py` exercises the real MSBuild target, including propagation of a shared edit to both hosts, unchanged-output timestamps,
clean/incremental equivalence, ordering, host isolation, and invalid input failures. `StartupPreferencesTests` uses the served shell to check
missing/persisted settings and blocked storage before deferred scripts execute. Run these with the existing contract and acceptance commands above.

## Build identity and content pinning

Both hosts import `build/BuildManifest.targets`. Each host embeds its own `Respondeo.BuildManifest.json` resource and packages
`wwwroot/build-manifest.json`; publishing also exports a sidecar at the publish root. Open **Thanks & sources** from the footer,
then expand **Version & build details** at the bottom of that page. This disclosure reads the embedded resource, not a network request.
It therefore identifies the actual installed APK or cached PWA, even when a newer website deployment exists.

The manifest contains only these public metadata fields:

| Field | Meaning |
| --- | --- |
| `version` | CI version `1.0.<run number>`, matching the APK display version. |
| `sourceRevision` | The workflow's source commit SHA. |
| `host` | `wasm` or `android`. |
| `buildKind` | `ci` or `development`. |
| `contentSha256` | Deterministic SHA-256 of the selected shared content inputs. |
| `hoursMode` | `licensed` or `sample`. |
| `hoursRevision` | Pinned licensed-content commit, or `sample`. |

Deployment resolves the licensed Hours repository commit once in the common build job, then checks out that exact commit for
Android. Both hosts receive the same revision. Without the token, both use the invented samples. Invalid requested paths or
empty selections fail instead of silently substituting samples. CI licensed builds also fail without a pinned commit.
`build/HoursContent.props` supplies the same selected JSON files to Hours resource embedding and manifest generation;
the legacy single-file `HoursLicensedContentPath` remains supported.

The digest covers the Content, Summa, and Bible libraries' `wwwroot` trees, Hours `Data` JSON, liturgical calendar text files,
and the selected Hours JSON (excluding schema files). Sort logical paths ordinally, use repository-relative paths for shared
files and `hours/content/<relative path>` for selected Hours files, then hash this UTF-8 inventory:

```text
respondeo-content-v1\n
<logical path>\0<lowercase SHA-256 of exact file bytes>\n
```

Here `\n` and `\0` denote LF and NUL, not literal backslashes. Absolute checkout paths, timestamps, build outputs, and host identity
are excluded. Exact-byte hashing deliberately detects line-ending changes; cross-machine comparisons require identical checkout
bytes. This is a shared-content identity, not a claim of byte-for-byte reproducible binaries or identical host configuration.

The deployment workflow compares the actual published web manifest and the APK build manifest against the common content digest.
Release notes include the APK's manifest and artifact hashes; website artifacts include a final `SHA256SUMS` inventory.
The service worker caches the web manifest alongside the app shell. Local builds explicitly report `0.0.0-dev` and
`development`, including for source revision; local licensed content without a supplied revision reports `development` for that
revision too. Build properties `BuildVersion`, `BuildSourceRevision`, `BuildKind`, and `HoursContentRevision` support explicit
verification builds without changing Android package IDs, version-code rules, signing keys, or reader storage keys.

## DNS

DNS for `respondeo.faith` is managed in **Cloudflare** (DNS-only / grey cloud) and points at GitHub
Pages:

| Type  | Name  | Value                                            |
| ----- | ----- | ------------------------------------------------ |
| A     | `@`   | `185.199.108.153`                                |
| A     | `@`   | `185.199.109.153`                                |
| A     | `@`   | `185.199.110.153`                                |
| A     | `@`   | `185.199.111.153`                                |
| CNAME | `www` | `delubear.github.io`                             |

The custom domain and **Enforce HTTPS** are set under the repo's **Settings -> Pages**. If the
domain is ever changed, update both the DNS records above and the `Add custom domain (CNAME)` step
in the deploy workflow.

Cloudflare records stay on **DNS-only (grey cloud)** because proxying (orange cloud) interferes with
GitHub Pages' automatic certificate provisioning/renewal. **Cloudflare Web Analytics** still works in
this mode: it is a client-side beacon (`beacon.min.js` in
[`index.template.html`](../src/Respondeo.Wasm/index.template.html)) and does not require proxied traffic. Set its site
token in that browser template from **Cloudflare -> Web Analytics**.

## Feature flags

Both hosts call `AddRespondeoApplication(configuration)` in the shared UI layer. It registers feature flags,
content, calendar, Hours, Summa, and UI services in order, preserving the reader-preferred calendar override.
Each host still owns its HttpClient transport, platform adapters, build identity, and logging. Native adapters
registered before this call win over the shared browser `TryAdd` defaults.

The **`FeatureFlags`** baseline lives in
[`Configuration/appsettings.json`](../src/Respondeo.UI/Configuration/appsettings.json), linked as an embedded resource
in both host assemblies. There are no hand-maintained host copies. All flags are explicitly `true` in that baseline;
a missing embedded resource, missing flag, or invalid boolean fails startup rather than silently enabling features.
The flags are represented by [`Services/FeatureFlags.cs`](../src/Respondeo.UI/Services/FeatureFlags.cs).

| Flag | Effect when `false` |
| --- | --- |
| `LiturgicalCalendarFeature` | Hides the Today in the Church guide and General Roman/United States calendar-profile selector. The calendar engine remains active for the Liturgy of the Hours and other date-based displays. |
| `LiturgyOfTheHoursFeature` | Hides the Liturgy of the Hours area within Discover. |
| `ExternalFeedback` | Skips the feedback choice dialog so the trigger goes straight to opening a GitHub issue (instead of also offering the hosted Tally form). |

To disable a feature for both deployments, change the shared baseline and rebuild. For an intentional browser-only
difference, add `src/Respondeo.Wasm/wwwroot/appsettings.json` or `appsettings.{Environment}.json` with only the changed keys.
WASM precedence is embedded baseline, then browser host settings, then environment settings (later values win).
These optional browser files are loaded by the normal WASM bootstrap and do not affect Android.
Android uses only the baseline today; an intentional native override must be added to `builder.Configuration` after
`AddRequiredEmbeddedSettings` and before `AddRespondeoApplication` in `MauiProgram.cs`. Flags are a startup snapshot,
not runtime switches. Do not copy the baseline to introduce an override.

> **Bundled configuration is public**, whether embedded in an assembly or shipped as a browser static asset.
> It is safe for UI toggles like these but must never contain secrets,
> API keys, or connection strings. Note also that hiding a pillar only removes its nav pill ΓÇö the
> underlying route (e.g. `/summa`) remains reachable by direct URL.

## Progressive Web App (offline)

Respondeo is an installable PWA and works **fully offline** once installed. Because the whole app -
including the Summa corpus and Markdown content - ships as static files, the service worker can
precache everything, so there is nothing left to fetch at runtime.

- **Dev vs published worker.** [`wwwroot/service-worker.js`](../src/Respondeo.Wasm/wwwroot/service-worker.js)
  is a no-op used during development (so changes are never cached). At publish time the SDK swaps in
  [`wwwroot/service-worker.published.js`](../src/Respondeo.Wasm/wwwroot/service-worker.published.js), which
  precaches the SDK-generated `service-worker-assets.js` manifest and serves `index.html` for
  navigation requests (so deep links work offline). This wiring lives in the PWA section of
  [`Respondeo.Wasm.csproj`](../src/Respondeo.Wasm/Respondeo.Wasm.csproj).
- **Install.** Visit the site and use the browser's *Install app* / *Add to Home Screen* option.
  On supported browsers a dismissible in-app banner
  ([`Components/InstallPrompt.razor`](../src/Respondeo.Wasm/Components/InstallPrompt.razor)) also appears when
  the app is installable: it captures the browser's `beforeinstallprompt` event (via
  `respondeoInstall` in [`js/site.js`](../src/Respondeo.Wasm/wwwroot/js/site.js)) and triggers the native
  install prompt on click. Dismissing it ("Not now") is remembered in `localStorage`, so it does not
  reappear on future visits. iOS Safari has no such API, so users there install via the Share sheet.
  Installability metadata (name, icons, colors) is in
  [`wwwroot/manifest.webmanifest`](../src/Respondeo.Wasm/wwwroot/manifest.webmanifest).
- **Online-only extras.** Analytics (Cloudflare Web Analytics) is cross-origin and not precached;
  offline it simply no-ops. The display fonts (EB Garamond, Cinzel) are self-hosted under
  `wwwroot/fonts/`, so they are precached and render identically offline.
- **Updates.** A new deploy changes the asset hashes, so the browser installs a fresh service
  worker that precaches the new build and then parks in the *waiting* state. Relying on the reader
  to close every tab is unreliable (an installed PWA often keeps the old worker alive in the
  background), so the app surfaces a small **"A new version is available. [Reload]"** prompt instead.
  The detection and prompt wiring live inline in
  [`index.template.html`](../src/Respondeo.Wasm/index.template.html): it watches for a waiting worker (and for
  one that finishes installing while the page is open), and only prompts when an existing worker is
  already in control, so a first-ever install shows no false prompt. Clicking **Reload** posts a
  `skip-waiting` message that
  [`service-worker.published.js`](../src/Respondeo.Wasm/wwwroot/service-worker.published.js) handles by
  calling `self.skipWaiting()`; the resulting `controllerchange` triggers a single reload into the
  new build. The banner styles (`.pwa-update`) are in
  [`wwwroot/css/`](../src/Respondeo.Wasm/wwwroot/css/).
- **Expected console warnings.** Once installed, DevTools may log preload messages for the framework
  files, e.g. *"a preload ... is found, but is not used because it is a cross-world service worker
  resource mismatch"* and *"preloaded ... but not used within a few seconds"*. These are benign:
  Blazor emits `<link rel="preload">` hints for the cold-network load, but the service worker serves
  those files from cache instead, so the preloads go unused. They are informational only and do not
  affect users or offline behavior.
- **Note.** The offline install includes the full Summa corpus, so the first install downloads a
  larger payload in exchange for complete offline access.
