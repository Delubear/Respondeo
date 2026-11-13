# Respondeo

A Catholic resource built with Blazor: a reasoned path into the faith (Inquiry), resources for living it (Discover),
the Bible, the Liturgy of the Hours, and a browsable *Summa Theologiae*. Live at **[respondeo.faith](https://respondeo.faith)**,
and available as an Android app built with .NET MAUI Blazor Hybrid.

## Projects

| Project | Purpose |
| --- | --- |
| `Respondeo.UI` | Razor class library with all shared pages, components, services, and static assets. |
| `Respondeo.Wasm` | The Blazor WebAssembly host (the website and PWA). |
| `Respondeo.Maui` | The .NET MAUI Blazor Hybrid Android app hosting the same UI. |
| `Respondeo.Content` | Authored Inquiry and Discover content, with its parsers and services. |
| `Respondeo.Content.Shared` | Shared content contracts and Markdown rendering. |
| `Respondeo.Content.Summa` | The generated *Summa Theologiae* corpus and its service. |
| `Respondeo.Bible` | The bundled Douay-Rheims Bible text and its service. |
| `Respondeo.LiturgicalCalendar` | The liturgical calendar engine (season, color, and celebration of the day), with the General Roman Calendar and a USCCB (United States) calendar selectable from the calendar dialog. |
| `Respondeo.LiturgyOfTheHours` | Builds the order of Morning, Evening, and Night Prayer. |
| `tools/Respondeo.SummaImporter` | Builds the Summa corpus from `docs/summa.txt`. |
| `tools/Respondeo.SitemapGenerator` | Generates `sitemap.xml` and static SEO pages at deploy time. |
| `tests/Respondeo.UnitTests` | Unit and bUnit component tests. |
| `tests/Respondeo.AcceptanceTests` | Reqnroll and Playwright end-to-end tests. |

## Getting started

```powershell
dotnet run --project src/Respondeo.Wasm/Respondeo.Wasm.csproj
dotnet test tests/Respondeo.UnitTests/Respondeo.UnitTests.csproj
```

See [`tests/Respondeo.AcceptanceTests/README.md`](tests/Respondeo.AcceptanceTests/README.md) for the
browser tests.

To build and install the Android app on a connected device (requires the `maui-android` workload):

```powershell
dotnet build src/Respondeo.Maui -f net11.0-android -c Release -t:Install
```

See [`src/Respondeo.Maui/README.md`](src/Respondeo.Maui/README.md) for details.

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

App-relevant pushes to `master` also build and verify an Android APK; a manual `workflow_dispatch` can request one with
`release_apk` enabled. Public APK releases require the `ANDROID_KEYSTORE_BASE64`, `ANDROID_KEYSTORE_PASSWORD`,
`ANDROID_KEY_ALIAS`, and `ANDROID_KEY_PASSWORD` secrets. The release job downloads the verified APK rather than rebuilding it
and attaches it, with `SHA256SUMS`, to a GitHub Release. Missing signing credentials fail the Android build instead of using a debug key.

PRs validate both Release publications for shared changes. Android-only changes skip browser publication; web-only changes skip Android.
PR APKs are debug-signed validation artifacts, not public releases, and receive no release secrets. See
[`docs/deployment.md`](docs/deployment.md) for the validation and release gates.

## Feature flags

Optional features can be switched off in the `FeatureFlags` section of the shared
[`Configuration/appsettings.json`](src/Respondeo.UI/Configuration/appsettings.json), embedded by both hosts.
The baseline explicitly enables `LiturgicalCalendarFeature`, `LiturgyOfTheHoursFeature`, and `ExternalFeedback`, defined in
[`FeatureFlags.cs`](src/Respondeo.UI/Services/FeatureFlags.cs). Missing or invalid settings fail startup.
See [deployment configuration](docs/deployment.md#feature-flags) for host/environment override precedence.

> Bundled settings are public, including embedded resources; never put secrets in them.
