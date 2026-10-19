# Search engine optimization (SEO)

)

Because the app is a client-rendered Blazor WebAssembly site, discoverability relies on a mix of
static shell metadata (for scrapers that do not run JavaScript) and per-page metadata (for crawlers
that do). All values share a single source of truth in
[`SiteMeta.cs`](../src/Respondeo/SiteMeta.cs) so the static tags, per-page overrides, and generated
sitemap agree on the canonical origin, name, and default description.

- **Static baseline.** [`wwwroot/index.html`](../src/Respondeo/wwwroot/index.html) carries a default
  meta description, a canonical link, and Open Graph / Twitter Card tags. These are what non-JS
  social scrapers (Slack, Facebook, X, etc.) see, so every share has a sane title, description, and
  preview image regardless of route.
- **Per-page metadata.** The reusable
  [`Components/SeoHead.razor`](../src/Respondeo/Components/SeoHead.razor) component emits per-route
  `<meta name="description">`, a canonical URL (resolved against the production origin so it stays
  correct on localhost/preview hosts), and Open Graph / Twitter overrides via `HeadContent`.
  It is applied on
  [`Home.razor`](../src/Respondeo/Pages/Home.razor),
  [`Articles.razor`](../src/Respondeo/Pages/Articles.razor),
  [`Summa.razor`](../src/Respondeo/Pages/Summa.razor),
  [`Miracles.razor`](../src/Respondeo/Pages/Miracles.razor),
  [`Saints.razor`](../src/Respondeo/Pages/Saints.razor),
  [`SummaQuestion.razor`](../src/Respondeo/Pages/SummaQuestion.razor),
  [`MiracleDetail.razor`](../src/Respondeo/Pages/MiracleDetail.razor),
  [`SaintDetail.razor`](../src/Respondeo/Pages/SaintDetail.razor), and
  [`Node.razor`](../src/Respondeo/Pages/Node.razor).
- **Structured data (JSON-LD).** `SeoHead` also emits schema.org JSON-LD: a `WebSite` node on the
  home page and an `Article` node on content detail pages (set `Article="true"`), built from
  `SiteMeta.WebSiteJsonLd()` / `SiteMeta.ArticleJsonLd(...)`.
- **robots.txt.** [`wwwroot/robots.txt`](../src/Respondeo/wwwroot/robots.txt) allows all crawlers and
  points them at the sitemap.
- **Sitemap.** `wwwroot/sitemap.xml` is a **generated CI artifact, not source** ΓÇö it is not tracked
  in git (see [`.gitignore`](../.gitignore)). It is produced by
  [`tools/Respondeo.SitemapGenerator`](../tools/Respondeo.SitemapGenerator), which enumerates every
  route from the content manifests and the shared `SummaParts` slug mapping (currently ~701 URLs).
  The generation is wired into the app build by
  [`build/GenerateSitemap.targets`](../src/Respondeo/build/GenerateSitemap.targets) (imported from
  [`Respondeo.csproj`](../src/Respondeo/Respondeo.csproj)): it builds the generator in-process with the
  `<MSBuild>` task and runs the compiled DLL via `dotnet exec`, so the sitemap stays current without
  a nested `dotnet run`. The target is **opt-in**: it runs only when `GenerateSitemapOnBuild=true`,
  which the deploy pipeline passes on its `dotnet publish` step (`-p:GenerateSitemapOnBuild=true`), so
  the file is written fresh into the published output at deploy time. Regular local/dev builds skip it
  entirely. To produce it locally (e.g. to inspect the output), run the generator directly or build
  with `-p:GenerateSitemapOnBuild=true`.
