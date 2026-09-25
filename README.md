# Respondeo

A Blazor WebAssembly app that presents a graph of Markdown-authored content nodes.

## Projects

| Project | Purpose |
| --- | --- |
| `Respondeo` | The Blazor WebAssembly front end (pages, components, styling). |
| `Respondeo.Content.Abstractions` | Shared content contracts (`ContentNode` and related types). |
| `Respondeo.Content.Markdown` | Markdown/YAML content, the parser, and the shipped content library. |
| `Respondeo.Content.Summa` | The Summa Theologiae corpus (generated JSON) and its models/service. |
| `Respondeo.SummaImporter` | Console tool that parses `docs/summa.txt` into the generated corpus. |
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
dotnet run --project src/Respondeo.SummaImporter -- docs/summa.txt src/Respondeo.Content.Summa\wwwroot
```

Both the generated JSON and `docs/summa.txt` are tracked in Git LFS (see **Prerequisites** above), so
regenerating and committing them keeps history lean — only LFS pointers change in the main pack.
Part identity is decoupled from presentation: files and tokens use stable neutral keys
(`p1`, `p2a`, `p2b`, `p3`, `sup`), while URL slugs and display labels are mapped at render time in
[`SummaParts`](src/Respondeo.Content.Summa/Summa/SummaParts.cs), so changing a slug or label needs no
corpus regeneration.

When citing the Summa from Markdown content, follow the citation-style guidance in the content
authoring guide: **[`src/Respondeo.Content.Markdown/README.md`](src/Respondeo.Content.Markdown/README.md)**.

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
  Installability metadata (name, icons, colours) is in
  [`wwwroot/manifest.webmanifest`](src/Respondeo/wwwroot/manifest.webmanifest).
- **Online-only extras.** GoatCounter analytics is cross-origin and not precached; offline it
  simply no-ops. The display fonts (EB Garamond, Cinzel) are self-hosted under `wwwroot/fonts/`, so
  they are precached and render identically offline.
- **Updates.** A new deploy changes the asset hashes, so the service worker updates its cache; users
  pick up the new version after the worker updates (typically one reload).
- **Note.** The offline install includes the full Summa corpus, so the first install downloads a
  larger payload in exchange for complete offline access.



