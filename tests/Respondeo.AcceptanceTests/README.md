# Respondeo.AcceptanceTests (BDD / E2E)

Business-readable acceptance tests written in Gherkin ([Reqnroll](https://reqnroll.net)) and
driven through a real browser with [Playwright](https://playwright.dev/dotnet/).

## What they cover

- **Home.feature** — the start page shows entry-point cards and navigating opens a node.
- **Breadcrumb.feature** — the breadcrumb grows as you go deeper and resets when you return to Start.
- **Sections.feature** — collapsible sections open/close and deep-link via the URL.
- **StageNavigation.feature** — stage URLs load directly, unknown paths show the not-found page, and the breadcrumb is rooted at the stage.
- **Summa.feature** — browsing the Summa remembers search, expanded parts, and scroll position when pressing Back, and resets when leaving the Summa area.
- **Theme.feature** — the light/dark theme toggle switches and persists.

These are intentionally few and focused on the core journey. Unit and component tests
(in `Respondeo.UnitTests`) cover the fine-grained logic.

## Running locally

1. Serve the app somewhere (dev server or a published build):

   ```powershell
   # Option A: dev server
   dotnet run --project ..\..\src\Respondeo\Respondeo.csproj

   # Option B: serve a publish output (matches CI)
   dotnet publish ..\..\src\Respondeo\Respondeo.csproj -c Release -o publish
   dotnet tool install --global dotnet-serve
   dotnet serve -d publish\wwwroot -p 5000 --fallback-file index.html
   ```

2. Point the tests at it and run:

   ```powershell
   $env:RESPONDEO_BASE_URL = "http://localhost:5000"
   dotnet test
   ```

   Set `$env:RESPONDEO_HEADED = "1"` to watch the browser.

## Playwright browsers

The first run needs browsers installed:

```powershell
dotnet build
pwsh bin\Debug\net10.0\playwright.ps1 install --with-deps chromium
```

CI performs this automatically before running the suite.
