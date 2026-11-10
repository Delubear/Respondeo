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

| Change | Shared unit/component tests | WASM publish and browser acceptance | Android Release publish and package verification |
| --- | --- | --- | --- |
| Shared UI/content, dependency, build, or workflow configuration | Yes | Yes | Yes |
| MAUI host only | Yes | No | Yes |
| WASM host, browser acceptance tests, or sitemap tooling only | Yes | Yes | No |
| READMEs, authoring instructions/prompts, or Markdown under docs | No | No | No |

Authored Inquiry/Discover Markdown is application content, not documentation, and validates both hosts.
Other changes conservatively validate both hosts. Manual PR workflow runs validate both hosts.

The PR workflow uses read-only repository/PR permissions and does not receive release signing or licensed-content secrets.
Its Android action produces an `android-validation` artifact using a development debug key. Add **Android Release Validation**
to the repository's required PR checks alongside unit/component and browser acceptance checks to enforce it before merge.
This branch-protection setting must be configured in GitHub; editing workflow files alone does not require the check.

The trusted deployment workflow builds the signed public APK in `android-build`, with read-only repository permissions.
All four secrets are mandatory: `ANDROID_KEYSTORE_BASE64`, `ANDROID_KEYSTORE_PASSWORD`, `ANDROID_KEY_ALIAS`, and
`ANDROID_KEY_PASSWORD`. Never rotate the signing identity casually: a differently signed APK cannot update an existing installation.
The temporary keystore is removed after the build, including on failure.

The shared `.github/actions/publish-android` action checks that exactly one signed APK was produced and verifies its ZIP archive,
Android signature, and ZIP alignment before uploading it with `SHA256SUMS`. The `android-release` job has release-write permission
but no signing credentials or build step. It downloads that run's `android-release` artifact, checks the checksum, and attaches the
unchanged APK and checksum file to the GitHub Release. Applicable browser acceptance checks must also pass; skipped browser checks
are allowed for MAUI-only changes. Failed or canceled checks block APK publication.

Pages similarly deploys the published output already exercised by browser acceptance tests, rather than rebuilding it.
An Android build failure does not block a validated website deployment. Pages permissions are limited to the Pages deployment job.
APK structure/signature checks do not replace device testing for startup, app switching, system bars, or upgrade persistence.

Run the path-rule regression tests locally with:

```powershell
python -m unittest discover -s .github\scripts -p test_workflow_paths.py
```

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
[`index.html`](../src/Respondeo.Wasm/wwwroot/index.html)) and does not require proxied traffic. Set its site
token in `index.html` from **Cloudflare -> Web Analytics**.

## Feature flags

Optional site features are gated behind deploy-time flags so they can be turned off without a code
change. The flags live in the **`FeatureFlags`** section of
[`wwwroot/appsettings.json`](../src/Respondeo.Wasm/wwwroot/appsettings.json) (the Android app reads its own embedded
`appsettings.json`), are bound to [`Services/FeatureFlags.cs`](../src/Respondeo.UI/Services/FeatureFlags.cs),
and default to `true` (feature on) when absent.

| Flag | Effect when `false` |
| --- | --- |
| `LiturgyOfTheHoursFeature` | Hides the Liturgy of the Hours area within Discover. |
| `ExternalFeedback` | Skips the feedback choice dialog so the trigger goes straight to opening a GitHub issue (instead of also offering the hosted Tally form). |

To disable a feature for a deploy, set its flag to `false` in `appsettings.json`, or add a
per-environment override at `wwwroot/appsettings.{Environment}.json` (it merges over the base file).

> **This is a WebAssembly app, so `appsettings.json` is published as a static asset and is publicly
> downloadable in the browser.** It is safe for UI toggles like these but must never contain secrets,
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
  [`wwwroot/index.html`](../src/Respondeo.Wasm/wwwroot/index.html): it watches for a waiting worker (and for
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
