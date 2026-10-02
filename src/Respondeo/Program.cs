using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Respondeo;
using Respondeo.Content.Summa;
using Respondeo.Services;
using Respondeo.Content;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Deploy-time feature toggles, bound from the "FeatureFlags" section of wwwroot/appsettings.json.
// Registered as a singleton so a flag can be flipped per-deploy (or per-environment) without a code change.
var featureFlags = builder.Configuration.GetSection(FeatureFlags.SectionName).Get<FeatureFlags>() ?? new FeatureFlags();
builder.Services.AddSingleton(featureFlags);

// The hand-authored content library ships both pillars as static assets: the Inquiry pillar
// (author-curated Markdown nodes behind IContentService) and the Discover pillar (prayers,
// devotions, articles, and the catalog of Catholic miracles). The app depends only on the
// public content interfaces; the parser and loader implementations stay internal.
builder.Services.AddRespondeoContent();

// The bundled Summa Theologica corpus is shipped as static assets by Respondeo.Content.Summa; the app depends only on ISummaService.
builder.Services.AddRespondeoSumma();

// Tracks the visitor's navigation path (persisted in sessionStorage) for breadcrumbs.
builder.Services.AddScoped<IBreadcrumbTrail, BreadcrumbTrail>();

// Tracks which nodes the visitor has opened this journey (persisted in sessionStorage) to mark visited branch cards.
builder.Services.AddScoped<IVisitedNodes, VisitedNodes>();

// Applies and persists the visitor's preferred light/dark theme.
builder.Services.AddScoped<IThemeService, ThemeService>();

// Persists the reader's place in a devotion (localStorage) so it can be resumed after leaving.
builder.Services.AddScoped<IDevotionProgressService, DevotionProgressService>();

// Signals whether a navigation should reset to the top (masthead nav) or scroll to content (cards/articles).
builder.Services.AddScoped<NavigationIntent>();

// Remembers transient browse view state (search text + facet filters) for every browse list across page
// remounts. Each page scopes a section by its area path, so leaving a sub-area (Articles, Prayers,
// Devotions, Miracles, Summa) resets just that section rather than all areas together.
builder.Services.AddScoped<BrowseState>();

// Typed wrapper over the window.respondeoBrowseState JS module (scroll + accordion memory) so the
// browse pages call strongly-typed methods instead of raw JS interop strings.
builder.Services.AddScoped<BrowseStateInterop>();

await builder.Build().RunAsync();
