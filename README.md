# Respondeo

A Blazor WebAssembly app that presents a graph of Markdown-authored content nodes.

## Projects

| Project | Purpose |
| --- | --- |
| `Respondeo` | The Blazor WebAssembly front end (pages, components, styling). |
| `Respondeo.Content.Abstractions` | Shared content contracts (`ContentNode` and related types). |
| `Respondeo.Content.Markdown` | Markdown/YAML content, the parser, and the shipped content library. |
| `Respondeo.UnitTests` | Unit and bUnit component tests. |
| `Respondeo.AcceptanceTests` | Reqnroll + Playwright end-to-end tests. |

## Getting started

```powershell
# Restore, build, and run the app
dotnet run --project Respondeo/Respondeo.csproj

# Run the unit and component tests
dotnet test Respondeo.UnitTests/Respondeo.UnitTests.csproj
```

See [`Respondeo.AcceptanceTests/README.md`](Respondeo.AcceptanceTests/README.md) for running the browser-based acceptance tests.

## Authoring content

Content lives in `Respondeo.Content.Markdown/wwwroot/content/` as Markdown files, each with a
YAML front-matter header followed by a Markdown body. New files must also be listed in
`manifest.json` so `ContentService` can load them.

The full authoring guide — front matter, links, and the media directives (YouTube, PDF, buttons) —
lives in **[`Respondeo.Content.Markdown/README.md`](Respondeo.Content.Markdown/README.md)**. Two
ready-to-copy templates sit alongside the content to start from:

- [`example.md`](Respondeo.Content.Markdown/wwwroot/content/example.md) — a full node stub showing
  every front-matter field and an example of each parser directive.
- [`example-section.md`](Respondeo.Content.Markdown/wwwroot/content/example-section.md) — a minimal
  section stub for use as a collapsible section of a parent page.
