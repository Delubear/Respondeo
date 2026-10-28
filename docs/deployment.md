# Deployment

The site is published to **GitHub Pages** by the `Deploy to GitHub Pages` workflow
([`.github/workflows/deploy.yml`](../.github/workflows/deploy.yml)) on every push to `master` that
changes non-docs files.

It is served from the custom apex domain **`respondeo.faith`**, so the app lives at the domain root
and the authored `<base href="/" />` is used as-is (no base-href rewrite). The deploy writes a
`CNAME` file into the published output on every run to keep the custom-domain binding. GitHub
automatically redirects the old project URL (`delubear.github.io/Respondeo/`) to the custom domain.

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
[`index.html`](../src/Respondeo/wwwroot/index.html)) and does not require proxied traffic. Set its site
token in `index.html` from **Cloudflare -> Web Analytics**.

## Feature flags

Optional site features are gated behind deploy-time flags so they can be turned off without a code
change. The flags live in the **`FeatureFlags`** section of
[`wwwroot/appsettings.json`](../src/Respondeo/wwwroot/appsettings.json), are bound to
[`Services/FeatureFlags.cs`](../src/Respondeo/Services/FeatureFlags.cs) in
[`Program.cs`](../src/Respondeo/Program.cs), and default to `true` (feature on) when absent.

| Flag | Effect when `false` |
| --- | --- |
| `AquinasPortrait` | Hides the St. Thomas Aquinas portrait in the masthead. |
| `SummaPillar` | Hides the Summa Theologiae pill in the pillar switcher. |
| `BiblePillar` | Hides the Holy Bible pill in the pillar switcher. |
| `DiscoverPillar` | Hides the entire Discover pill in the pillar switcher (supersedes the per-feature flags). |
| `MiraclesFeature` | Hides the Miracles area within Discover. |
| `SaintsFeature` | Hides the Saints area within Discover. |
| `PrayerFeature` | Hides the Prayers area within Discover. |
| `DevotionsFeature` | Hides the Devotions area within Discover. |
| `ArticlesFeature` | Hides the Articles area within Discover. |
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

- **Dev vs published worker.** [`wwwroot/service-worker.js`](../src/Respondeo/wwwroot/service-worker.js)
  is a no-op used during development (so changes are never cached). At publish time the SDK swaps in
  [`wwwroot/service-worker.published.js`](../src/Respondeo/wwwroot/service-worker.published.js), which
  precaches the SDK-generated `service-worker-assets.js` manifest and serves `index.html` for
  navigation requests (so deep links work offline). This wiring lives in the PWA section of
  [`Respondeo.csproj`](../src/Respondeo/Respondeo.csproj).
- **Install.** Visit the site and use the browser's *Install app* / *Add to Home Screen* option.
  On supported browsers a dismissible in-app banner
  ([`Components/InstallPrompt.razor`](../src/Respondeo/Components/InstallPrompt.razor)) also appears when
  the app is installable: it captures the browser's `beforeinstallprompt` event (via
  `respondeoInstall` in [`js/site.js`](../src/Respondeo/wwwroot/js/site.js)) and triggers the native
  install prompt on click. Dismissing it ("Not now") is remembered in `localStorage`, so it does not
  reappear on future visits. iOS Safari has no such API, so users there install via the Share sheet.
  Installability metadata (name, icons, colors) is in
  [`wwwroot/manifest.webmanifest`](../src/Respondeo/wwwroot/manifest.webmanifest).
- **Online-only extras.** Analytics (Cloudflare Web Analytics) is cross-origin and not precached;
  offline it simply no-ops. The display fonts (EB Garamond, Cinzel) are self-hosted under
  `wwwroot/fonts/`, so they are precached and render identically offline.
- **Updates.** A new deploy changes the asset hashes, so the browser installs a fresh service
  worker that precaches the new build and then parks in the *waiting* state. Relying on the reader
  to close every tab is unreliable (an installed PWA often keeps the old worker alive in the
  background), so the app surfaces a small **"A new version is available. [Reload]"** prompt instead.
  The detection and prompt wiring live inline in
  [`wwwroot/index.html`](../src/Respondeo/wwwroot/index.html): it watches for a waiting worker (and for
  one that finishes installing while the page is open), and only prompts when an existing worker is
  already in control, so a first-ever install shows no false prompt. Clicking **Reload** posts a
  `skip-waiting` message that
  [`service-worker.published.js`](../src/Respondeo/wwwroot/service-worker.published.js) handles by
  calling `self.skipWaiting()`; the resulting `controllerchange` triggers a single reload into the
  new build. The banner styles (`.pwa-update`) are in
  [`wwwroot/css/`](../src/Respondeo/wwwroot/css/).
- **Expected console warnings.** Once installed, DevTools may log preload messages for the framework
  files, e.g. *"a preload ... is found, but is not used because it is a cross-world service worker
  resource mismatch"* and *"preloaded ... but not used within a few seconds"*. These are benign:
  Blazor emits `<link rel="preload">` hints for the cold-network load, but the service worker serves
  those files from cache instead, so the preloads go unused. They are informational only and do not
  affect users or offline behavior.
- **Note.** The offline install includes the full Summa corpus, so the first install downloads a
  larger payload in exchange for complete offline access.
