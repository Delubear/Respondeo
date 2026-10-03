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

// Assembles the Summa question page's display data (part context, SEO description, back-crumb) from
// the content services, keeping that orchestration out of the component.
builder.Services.AddScoped<SummaQuestionPresenter>();

// Pure title/treatise search over the Summa index, kept out of the Summa browse page so it is unit-testable.
builder.Services.AddScoped<SummaSearch>();

// Pure facet/search/sort logic for the prayer treasury, kept out of the Prayers browse page so it is unit-testable.
builder.Services.AddScoped<PrayerBrowse>();

// Pure facet/search/sort logic plus card presentation for the miracles catalog, kept out of the Miracles browse page so it is unit-testable.
builder.Services.AddScoped<MiracleBrowse>();

// Pure facet/search/sort logic for the devotions index, kept out of the Devotions browse page so it is unit-testable.
builder.Services.AddScoped<DevotionBrowse>();

// Pure row projection, tag facet, and search/sort for the Discover articles index, kept out of the Articles browse page so it is unit-testable.
builder.Services.AddScoped<ArticleBrowse>();

// Tracks the visitor's navigation path (persisted in sessionStorage) for breadcrumbs.
builder.Services.AddScoped<IBreadcrumbTrail, BreadcrumbTrail>();

// Tracks which nodes the visitor has opened this journey (persisted in sessionStorage) to mark visited branch cards.
builder.Services.AddScoped<IVisitedNodes, VisitedNodes>();

// Applies and persists the visitor's preferred light/dark theme.
builder.Services.AddScoped<IThemeService, ThemeService>();

// Remembers whether the reader prefers Latin prayer names in devotions (localStorage).
builder.Services.AddScoped<IPrayerLanguageService, PrayerLanguageService>();

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

// Typed wrappers over the small window-level navigation helpers (url.replace, scroll, focus) and the
// Summa cross-reference module, so pages call strongly-typed methods instead of raw JS interop strings.
builder.Services.AddScoped<NavigationInterop>();
builder.Services.AddScoped<SummaRefInterop>();
builder.Services.AddScoped<ImmersiveInterop>();
builder.Services.AddScoped<DialogInterop>();
builder.Services.AddScoped<AccessibilityInterop>();
builder.Services.AddScoped<InstallInterop>();
builder.Services.AddScoped<TallyInterop>();

await builder.Build().RunAsync();
