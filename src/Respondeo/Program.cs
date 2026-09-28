using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Respondeo;
using Respondeo.Content.Markdown;
using Respondeo.Content.Shared;
using Respondeo.Content.Summa;
using Respondeo.Content.Discover;
using Respondeo.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Deploy-time feature toggles, bound from the "FeatureFlags" section of wwwroot/appsettings.json.
// Registered as a singleton so a flag can be flipped per-deploy (or per-environment) without a code change.
var featureFlags = builder.Configuration.GetSection(FeatureFlags.SectionName).Get<FeatureFlags>() ?? new FeatureFlags();
builder.Services.AddSingleton(featureFlags);

// Shared Markdown-to-HTML rendering (Markdig lives here,
// behind IContentHtmlRenderer) used by every content pillar so the embed directive vocabulary stays identical across the site.
builder.Services.AddRespondeoContentRendering();

// Content is author-curated Markdown loaded once and cached for the app lifetime.
// The parser and loader implementations are internal to Respondeo.Content.Markdown; the app depends only on IContentService.
builder.Services.AddRespondeoContent();

// The bundled Summa Theologica corpus is shipped as static assets by Respondeo.Content.Summa; the app depends only on ISummaService.
builder.Services.AddRespondeoSumma();

// The hand-authored prayers, data-driven devotions, deeper-dive articles, and the catalog of Catholic miracles are shipped as static content by
// Respondeo.Content.Discover; the app depends only on IDiscoverService and IMiracleService.
builder.Services.AddRespondeoDiscover();

// Tracks the visitor's navigation path (persisted in sessionStorage) for breadcrumbs.
builder.Services.AddScoped<IBreadcrumbTrail, BreadcrumbTrail>();

// Applies and persists the visitor's preferred light/dark theme.
builder.Services.AddScoped<IThemeService, ThemeService>();

// Signals whether a navigation should reset to the top (masthead nav) or scroll to content (cards/articles).
builder.Services.AddScoped<NavigationIntent>();

// Remembers search text and scroll position for the Discover browse lists across Back navigation.
builder.Services.AddScoped<DiscoverBrowseState>();

// Remembers the Summa browse/search view (search text + expanded parts/treatises) across page remounts, resetting itself when the visitor leaves the Summa area.
builder.Services.AddScoped<SummaBrowseState>();

// Remembers the miracles browse view (search text + selected facet filters) across page remounts, resetting itself when the visitor leaves the miracles area.
builder.Services.AddScoped<MiracleBrowseState>();

await builder.Build().RunAsync();
