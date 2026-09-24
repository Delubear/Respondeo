# Respondeo

A Blazor WebAssembly app that presents a graph of Markdown-authored content nodes.

## Projects

| Project | Purpose |
| --- | --- |
| `Respondeo` | The Blazor WebAssembly front end (pages, components, styling). |
| `Respondeo.Content.Abstractions` | Shared content contracts (`ContentNode` and related types). |
| `Respondeo.Content.Markdown` | Markdown/YAML content, the parser, and the shipped content library. |
| `Respondeo.Content.Summa` | The Summa Theologiae corpus (generated JSON) and its models/service. |
| `Respondeo.SummaImporter` | Console tool that parses `summa.txt` into the generated corpus. |
| `Respondeo.UnitTests` | Unit and bUnit component tests. |
| `Respondeo.AcceptanceTests` | Reqnroll + Playwright end-to-end tests. |

## Prerequisites

This repository stores the large Summa corpus (`src/Respondeo.Content.Summa/wwwroot/summa/**`)
and its source text (`summa.txt`) in **[Git LFS](https://git-lfs.com/)**. Install and enable
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
`src/Respondeo.Content.Summa/wwwroot/summa/`, produced from the public-domain source text `summa.txt`
by the `Respondeo.SummaImporter` tool:

```powershell
# Regenerate the corpus (clears and rewrites the summa output tree)
dotnet run --project src/Respondeo.SummaImporter -- docs/summa.txt src/Respondeo.Content.Summa\wwwroot
```

Both the generated JSON and `summa.txt` are tracked in Git LFS (see **Prerequisites** above), so
regenerating and committing them keeps history lean — only LFS pointers change in the main pack.
Part identity is decoupled from presentation: files and tokens use stable neutral keys
(`p1`, `p2a`, `p2b`, `p3`, `sup`), while URL slugs and display labels are mapped at render time in
[`SummaParts`](src/Respondeo.Content.Summa/Summa/SummaParts.cs), so changing a slug or label needs no
corpus regeneration.

