# Respondeo

A Catholic resource built with Blazor WebAssembly: a reasoned path into the faith (Inquiry), resources
for living it (Discover), and a browsable *Summa Theologiae*. Live at **[respondeo.faith](https://respondeo.faith)**.

## Projects

| Project | Purpose |
| --- | --- |
| `Respondeo` | The Blazor WebAssembly front end. |
| `Respondeo.Content` | Authored Inquiry and Discover content, with its parsers and services. |
| `Respondeo.Content.Shared` | Shared content contracts and Markdown rendering. |
| `Respondeo.Content.Summa` | The generated *Summa Theologiae* corpus and its service. |
| `Respondeo.LiturgicalCalendar` | The liturgical calendar engine (season, color, and celebration of the day). |
| `tools/Respondeo.SummaImporter` | Builds the Summa corpus from `docs/summa.txt`. |
| `tools/Respondeo.SitemapGenerator` | Generates `sitemap.xml` and static SEO pages at deploy time. |
| `tests/Respondeo.UnitTests` | Unit and bUnit component tests. |
| `tests/Respondeo.AcceptanceTests` | Reqnroll and Playwright end-to-end tests. |

## Getting started

The Summa corpus and its source text are stored in [Git LFS](https://git-lfs.com/). Run `git lfs install`
before cloning (or `git lfs pull` afterward) to get the real files instead of pointer stubs.

```powershell
dotnet run --project src/Respondeo/Respondeo.csproj
dotnet test tests/Respondeo.UnitTests/Respondeo.UnitTests.csproj
```

See [`tests/Respondeo.AcceptanceTests/README.md`](tests/Respondeo.AcceptanceTests/README.md) for the
browser tests.

## Authoring content

- **Inquiry:** [`src/Respondeo.Content/INQUIRY.md`](src/Respondeo.Content/INQUIRY.md)
- **Discover** (prayers, devotions, articles, miracles, saints):
  [`src/Respondeo.Content/DISCOVER.md`](src/Respondeo.Content/DISCOVER.md)

Templates are in [`docs/templates/`](docs/templates), and authoring prompts are in
[`.github/prompts/`](.github/prompts). New files must be added to their folder's manifest.

To regenerate the Summa corpus:

```powershell
dotnet run --project tools/Respondeo.SummaImporter -- docs/summa.txt src/Respondeo.Content.Summa\wwwroot
```

## Deployment

Pushes to `master` deploy to GitHub Pages through [`.github/workflows/deploy.yml`](.github/workflows/deploy.yml).
The custom domain `respondeo.faith` uses Cloudflare DNS in DNS-only mode, pointing at GitHub Pages.

The app is an installable PWA that works fully offline. See [`docs/deployment.md`](docs/deployment.md)
for DNS, the full feature-flag reference, and PWA details, and [`docs/seo.md`](docs/seo.md) for
metadata, the sitemap, and static SEO pages.

## Feature flags

Optional features can be switched off in the `FeatureFlags` section of
[`wwwroot/appsettings.json`](src/Respondeo/wwwroot/appsettings.json). Every flag defaults to `true`:
`AquinasPortrait`, `SummaPillar`, `DiscoverPillar`, `MiraclesFeature`, `SaintsFeature`,
`PrayerFeature`, `DevotionsFeature`, `ArticlesFeature`, and `ExternalFeedback`. See
[`docs/deployment.md`](docs/deployment.md#feature-flags) for what each one does.

> `appsettings.json` is publicly downloadable in a WebAssembly app, so never put secrets in it.
