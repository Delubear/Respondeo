# Respondeo

A Blazor WebAssembly app that presents a graph of Markdown-authored content nodes.

## Projects

| Project | Purpose |
| --- | --- |
| `Respondeo` | The Blazor WebAssembly front end (pages, components, styling). |
| `Respondeo.Content.Abstractions` | Shared content contracts (`ContentNode` and related types). |
| `Respondeo.Content.Rendering` | Shared rendering layer (Markdown-to-HTML and Summa reference/token expansion). |
| `Respondeo.Content.Markdown` | Markdown/YAML content, the parser, and the shipped content library. |
| `Respondeo.Content.Summa` | The Summa Theologiae corpus (generated JSON) and its models/service. |
| `Respondeo.Content.Discover` | The Discover pillar (miracles, prayers, devotions, articles) and its models/services. |
| `Respondeo.SummaImporter` | Developer tool (under `tools/`) that parses `docs/summa.txt` into the generated corpus. |
| `Respondeo.SitemapGenerator` | Build/deploy tool (under `tools/`) that generates `sitemap.xml` from the content manifests. |
| `Respondeo.UnitTests` | Unit and bUnit component tests. |
| `Respondeo.AcceptanceTests` | Reqnroll + Playwright end-to-end tests. |

## Prerequisites

This repository stores the large Summa corpus (`src/Respondeo.Content.Summa/wwwroot/summa/**`)
and its source text (`docs/summa.txt`) in **[Git LFS](https://git-lfs.com/)**. Install and enable
LFS **before cloning** so you get the real files instead of small pointer stubs:

```powershell
# Install once per machine (or via your package manager), then enable it for your user:
git lfs install

# Now clone as usual — LFS content is fetched automatically:
git clone https://github.com/Delubear/Respondeo
```

Already cloned before installing LFS? Run `git lfs install` then `git lfs pull` to replace
the pointer files with their real content.

## Getting started

```powershell
# Restore, build, and run the app
dotnet run --project src/Respondeo/Respondeo.csproj

# Run the unit and component tests
dotnet test tests/Respondeo.UnitTests/Respondeo.UnitTests.csproj
```

See [`tests/Respondeo.AcceptanceTests/README.md`](tests/Respondeo.AcceptanceTests/README.md) for running the browser-based acceptance tests.

## Authoring content

Content lives in `src/Respondeo.Content.Markdown/wwwroot/content/` as Markdown files, each with a
YAML front-matter header followed by a Markdown body. New files must also be listed in
`manifest.json` so `ContentService` can load them.

The full authoring guide — front matter, links, and the media directives (YouTube, PDF, buttons) —
lives in **[`src/Respondeo.Content.Markdown/README.md`](src/Respondeo.Content.Markdown/README.md)**. Two
ready-to-copy templates sit alongside the content to start from:

- [`example.md`](src/Respondeo.Content.Markdown/wwwroot/content/example.md) — a full node stub showing
  every front-matter field and an example of each parser directive.
- [`example-section.md`](src/Respondeo.Content.Markdown/wwwroot/content/example-section.md) — a minimal
  section stub for use as a collapsible section of a parent page.

## Summa corpus

The Summa Theologiae browser is backed by a generated corpus under
`src/Respondeo.Content.Summa/wwwroot/summa/`, produced from the public-domain source text
`docs/summa.txt` by the `Respondeo.SummaImporter` tool:

```powershell
# Regenerate the corpus (clears and rewrites the summa output tree)
dotnet run --project tools/Respondeo.SummaImporter -- docs/summa.txt src/Respondeo.Content.Summa\wwwroot
```

Both the generated JSON and `docs/summa.txt` are tracked in Git LFS (see **Prerequisites** above), so
regenerating and committing them keeps history lean — only LFS pointers change in the main pack.
Part identity is decoupled from presentation: files and tokens use stable neutral keys
(`p1`, `p2a`, `p2b`, `p3`, `sup`), while URL slugs and display labels are mapped at render time in
[`SummaParts`](src/Respondeo.Content.Summa/Summa/SummaParts.cs), so changing a slug or label needs no
corpus regeneration.

When citing the Summa from Markdown content, follow the citation-style guidance in the content
authoring guide: **[`src/Respondeo.Content.Markdown/README.md`](src/Respondeo.Content.Markdown/README.md)**.

> **Future pillar idea — the Catechism of the Catholic Church.** Its numbered, cross-referenced
> structure would map onto the Summa-style browsing layout very well and would make a great addition
> as its own pillar. It is **not included today because the current English translation is still
> under copyright**, so it cannot be shipped in this repository. If a public-domain or suitably
> licensed text becomes available, it could be modelled after `Respondeo.Content.Summa`.

## Deployment

The site is published to **GitHub Pages** by the `Deploy to GitHub Pages` workflow
([`.github/workflows/deploy.yml`](.github/workflows/deploy.yml)) on every push to `master` that
changes non-docs files.

It is served from the custom apex domain **`respondeo.faith`**, so the app lives at the domain root
and the authored `<base href="/" />` is used as-is (no base-href rewrite). The deploy writes a
`CNAME` file into the published output on every run to keep the custom-domain binding. GitHub
automatically redirects the old project URL (`delubear.github.io/Respondeo/`) to the custom domain.

### DNS

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
[`index.html`](src/Respondeo/wwwroot/index.html)) and does not require proxied traffic. Set its site
token in `index.html` from **Cloudflare -> Web Analytics**.

## Feature flags

Optional site features are gated behind deploy-time flags so they can be turned off without a code
change. The flags live in the **`FeatureFlags`** section of
[`wwwroot/appsettings.json`](src/Respondeo/wwwroot/appsettings.json), are bound to
[`Services/FeatureFlags.cs`](src/Respondeo/Services/FeatureFlags.cs) in
[`Program.cs`](src/Respondeo/Program.cs), and default to `true` (feature on) when absent.

| Flag | Effect when `false` |
| --- | --- |
| `AquinasPortrait` | Hides the St. Thomas Aquinas portrait in the masthead. |
| `SummaPillar` | Hides the Summa Theologiae pill in the pillar switcher. |
| `DiscoverPillar` | Hides the entire Discover pill in the pillar switcher (supersedes the per-feature flags). |
| `MiraclesFeature` | Hides the Miracles area within Discover. |
| `PrayerFeature` | Hides the Prayers area within Discover. |
| `DevotionsFeature` | Hides the Devotions area within Discover. |
| `ArticlesFeature` | Hides the Articles area within Discover. |

To disable a feature for a deploy, set its flag to `false` in `appsettings.json`, or add a
per-environment override at `wwwroot/appsettings.{Environment}.json` (it merges over the base file).

> **This is a WebAssembly app, so `appsettings.json` is published as a static asset and is publicly
> downloadable in the browser.** It is safe for UI toggles like these but must never contain secrets,
> API keys, or connection strings. Note also that hiding a pillar only removes its nav pill — the
> underlying route (e.g. `/summa`) remains reachable by direct URL.

## Progressive Web App (offline)

Respondeo is an installable PWA and works **fully offline** once installed. Because the whole app -
including the Summa corpus and Markdown content - ships as static files, the service worker can
precache everything, so there is nothing left to fetch at runtime.

- **Dev vs published worker.** [`wwwroot/service-worker.js`](src/Respondeo/wwwroot/service-worker.js)
  is a no-op used during development (so changes are never cached). At publish time the SDK swaps in
  [`wwwroot/service-worker.published.js`](src/Respondeo/wwwroot/service-worker.published.js), which
  precaches the SDK-generated `service-worker-assets.js` manifest and serves `index.html` for
  navigation requests (so deep links work offline). This wiring lives in the PWA section of
  [`Respondeo.csproj`](src/Respondeo/Respondeo.csproj).
- **Install.** Visit the site and use the browser's *Install app* / *Add to Home Screen* option.
  On supported browsers a dismissible in-app banner
  ([`Components/InstallPrompt.razor`](src/Respondeo/Components/InstallPrompt.razor)) also appears when
  the app is installable: it captures the browser's `beforeinstallprompt` event (via
  `respondeoInstall` in [`js/site.js`](src/Respondeo/wwwroot/js/site.js)) and triggers the native
  install prompt on click. Dismissing it ("Not now") is remembered in `localStorage`, so it does not
  reappear on future visits. iOS Safari has no such API, so users there install via the Share sheet.
  Installability metadata (name, icons, colours) is in
  [`wwwroot/manifest.webmanifest`](src/Respondeo/wwwroot/manifest.webmanifest).
- **Online-only extras.** Analytics (Cloudflare Web Analytics) is cross-origin and not precached;
  offline it simply no-ops. The display fonts (EB Garamond, Cinzel) are self-hosted under
  `wwwroot/fonts/`, so they are precached and render identically offline.
- **Updates.** A new deploy changes the asset hashes, so the browser installs a fresh service
  worker that precaches the new build and then parks in the *waiting* state. Relying on the reader
  to close every tab is unreliable (an installed PWA often keeps the old worker alive in the
  background), so the app surfaces a small **"A new version is available. [Reload]"** prompt instead.
  The detection and prompt wiring live inline in
  [`wwwroot/index.html`](src/Respondeo/wwwroot/index.html): it watches for a waiting worker (and for
  one that finishes installing while the page is open), and only prompts when an existing worker is
  already in control, so a first-ever install shows no false prompt. Clicking **Reload** posts a
  `skip-waiting` message that
  [`service-worker.published.js`](src/Respondeo/wwwroot/service-worker.published.js) handles by
  calling `self.skipWaiting()`; the resulting `controllerchange` triggers a single reload into the
  new build. The banner styles (`.pwa-update`) are in
  [`wwwroot/css/app.css`](src/Respondeo/wwwroot/css/app.css).
- **Expected console warnings.** Once installed, DevTools may log preload messages for the framework
  files, e.g. *"a preload ... is found, but is not used because it is a cross-world service worker
  resource mismatch"* and *"preloaded ... but not used within a few seconds"*. These are benign:
  Blazor emits `<link rel="preload">` hints for the cold-network load, but the service worker serves
  those files from cache instead, so the preloads go unused. They are informational only and do not
  affect users or offline behaviour.
- **Note.** The offline install includes the full Summa corpus, so the first install downloads a
  larger payload in exchange for complete offline access.

## Search engine optimization (SEO)

Because the app is a client-rendered Blazor WebAssembly site, discoverability relies on a mix of
static shell metadata (for scrapers that do not run JavaScript) and per-page metadata (for crawlers
that do). All values share a single source of truth in
[`SiteMeta.cs`](src/Respondeo/SiteMeta.cs) so the static tags, per-page overrides, and generated
sitemap agree on the canonical origin, name, and default description.

- **Static baseline.** [`wwwroot/index.html`](src/Respondeo/wwwroot/index.html) carries a default
  meta description, a canonical link, and Open Graph / Twitter Card tags. These are what non-JS
  social scrapers (Slack, Facebook, X, etc.) see, so every share has a sane title, description, and
  preview image regardless of route.
- **Per-page metadata.** The reusable
  [`Components/SeoHead.razor`](src/Respondeo/Components/SeoHead.razor) component emits per-route
  `<meta name="description">`, a canonical URL (resolved against the production origin so it stays
  correct on localhost/preview hosts), and Open Graph / Twitter overrides via `HeadContent`.
  It is applied on
  [`Home.razor`](src/Respondeo/Pages/Home.razor),
  [`Articles.razor`](src/Respondeo/Pages/Articles.razor),
  [`Summa.razor`](src/Respondeo/Pages/Summa.razor),
  [`Miracles.razor`](src/Respondeo/Pages/Miracles.razor),
  [`SummaQuestion.razor`](src/Respondeo/Pages/SummaQuestion.razor),
  [`MiracleDetail.razor`](src/Respondeo/Pages/MiracleDetail.razor), and
  [`Node.razor`](src/Respondeo/Pages/Node.razor).
- **Structured data (JSON-LD).** `SeoHead` also emits schema.org JSON-LD: a `WebSite` node on the
  home page and an `Article` node on content detail pages (set `Article="true"`), built from
  `SiteMeta.WebSiteJsonLd()` / `SiteMeta.ArticleJsonLd(...)`.
- **robots.txt.** [`wwwroot/robots.txt`](src/Respondeo/wwwroot/robots.txt) allows all crawlers and
  points them at the sitemap.
- **Sitemap.** `wwwroot/sitemap.xml` is a **generated CI artifact, not source** — it is not tracked
  in git (see [`.gitignore`](.gitignore)). It is produced by
  [`tools/Respondeo.SitemapGenerator`](tools/Respondeo.SitemapGenerator), which enumerates every
  route from the content manifests and the shared `SummaParts` slug mapping (currently ~701 URLs).
  The generation is wired into the app build by
  [`build/GenerateSitemap.targets`](src/Respondeo/build/GenerateSitemap.targets) (imported from
  [`Respondeo.csproj`](src/Respondeo/Respondeo.csproj)): it builds the generator in-process with the
  `<MSBuild>` task and runs the compiled DLL via `dotnet exec`, so the sitemap stays current without
  a nested `dotnet run`. The target is **opt-in**: it runs only when `GenerateSitemapOnBuild=true`,
  which the deploy pipeline passes on its `dotnet publish` step (`-p:GenerateSitemapOnBuild=true`), so
  the file is written fresh into the published output at deploy time. Regular local/dev builds skip it
  entirely. To produce it locally (e.g. to inspect the output), run the generator directly or build
  with `-p:GenerateSitemapOnBuild=true`.

