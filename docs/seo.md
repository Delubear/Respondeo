# Search engine optimization (SEO)

)

Because the app is a client-rendered Blazor WebAssembly site, discoverability relies on a mix of
static shell metadata (for scrapers that do not run JavaScript) and per-page metadata (for crawlers
that do). All values share a single source of truth in
[`SiteMeta.cs`](../src/Respondeo.Wasm/SiteMeta.cs) so the static tags, per-page overrides, and generated
sitemap agree on the canonical origin, name, and default description.

- **Static baseline.** [`index.template.html`](../src/Respondeo.Wasm/index.template.html) supplies the generated `wwwroot/index.html` with a default
  meta description, a canonical link, and Open Graph / Twitter Card tags. These are what non-JS
  social scrapers (Slack, Facebook, X, etc.) see, so every share has a sane title, description, and
  preview image regardless of route.
- **Per-page metadata.** The reusable
  [`Components/SeoHead.razor`](../src/Respondeo.Wasm/Components/SeoHead.razor) component emits per-route
  `<meta name="description">`, a canonical URL (resolved against the production origin so it stays
  correct on localhost/preview hosts), and Open Graph / Twitter overrides via `HeadContent`.
  It is applied on
  [`Home.razor`](../src/Respondeo.Wasm/Pages/Home.razor),
  [`Articles.razor`](../src/Respondeo.Wasm/Pages/Articles.razor),
  [`Summa.razor`](../src/Respondeo.Wasm/Pages/Summa.razor),
  [`Miracles.razor`](../src/Respondeo.Wasm/Pages/Miracles.razor),
  [`Saints.razor`](../src/Respondeo.Wasm/Pages/Saints.razor),
  [`SummaQuestion.razor`](../src/Respondeo.Wasm/Pages/SummaQuestion.razor),
  [`MiracleDetail.razor`](../src/Respondeo.Wasm/Pages/MiracleDetail.razor),
  [`SaintDetail.razor`](../src/Respondeo.Wasm/Pages/SaintDetail.razor), and
  [`Node.razor`](../src/Respondeo.Wasm/Pages/Node.razor).
- **Structured data (JSON-LD).** `SeoHead` also emits schema.org JSON-LD: a `WebSite` node on the
  home page and an `Article` node on content detail pages (set `Article="true"`), built from
  `SiteMeta.WebSiteJsonLd()` / `SiteMeta.ArticleJsonLd(...)`.
- **robots.txt.** [`wwwroot/robots.txt`](../src/Respondeo.Wasm/wwwroot/robots.txt) allows all crawlers and
  points them at the sitemap.
- **Sitemap.** `wwwroot/sitemap.xml` is a **generated CI artifact, not source** ΓÇö it is not tracked
  in git (see [`.gitignore`](../.gitignore)). It is produced by
  [`tools/Respondeo.SitemapGenerator`](../tools/Respondeo.SitemapGenerator), which enumerates every
  route from the content manifests and the shared `SummaParts` slug mapping (currently ~701 URLs).
  The generation is wired into the app build by
  [`build/GenerateSitemap.targets`](../src/Respondeo.Wasm/build/GenerateSitemap.targets) (imported from
  [`Respondeo.Wasm.csproj`](../src/Respondeo.Wasm/Respondeo.Wasm.csproj)): it builds the generator in-process with the
  `<MSBuild>` task and runs the compiled DLL via `dotnet exec`, so the sitemap stays current without
  a nested `dotnet run`. The target is **opt-in**: it runs only when `GenerateSitemapOnBuild=true`,
  which the deploy pipeline passes on its `dotnet publish` step (`-p:GenerateSitemapOnBuild=true`), so
  the file is written fresh into the published output at deploy time. Regular local/dev builds skip it
  entirely. To produce it locally (e.g. to inspect the output), run the generator directly or build
  with `-p:GenerateSitemapOnBuild=true`.
