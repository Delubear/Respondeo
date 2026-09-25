using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Respondeo;
using Respondeo.Content.Markdown;
using Respondeo.Content.Summa;
using Respondeo.Content.Miracles;
using Respondeo.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Content is author-curated Markdown loaded once and cached for the app lifetime.
// The parser and loader implementations are internal to Respondeo.Content.Markdown; the app depends only on IContentService.
builder.Services.AddRespondeoContent();

// The bundled Summa Theologica corpus is shipped as static assets by Respondeo.Content.Summa;
// the app depends only on ISummaService.
builder.Services.AddRespondeoSumma();

// The hand-authored catalog of Catholic miracles is shipped as static Markdown by
// Respondeo.Content.Miracles; the app depends only on IMiracleService.
builder.Services.AddRespondeoMiracles();

// Tracks the visitor's navigation path (persisted in sessionStorage) for breadcrumbs.
builder.Services.AddScoped<IBreadcrumbTrail, BreadcrumbTrail>();

// Applies and persists the visitor's preferred light/dark theme.
builder.Services.AddScoped<IThemeService, ThemeService>();

// Signals whether a navigation should reset to the top (masthead nav) or scroll to content (cards/articles).
builder.Services.AddScoped<NavigationIntent>();

// Remembers the Summa browse/search view (search text + expanded parts/treatises) across page
// remounts, resetting itself when the visitor leaves the Summa area.
builder.Services.AddScoped<SummaBrowseState>();

// Remembers the miracles browse view (search text + selected facet filters) across page remounts,
// resetting itself when the visitor leaves the miracles area.
builder.Services.AddScoped<MiracleBrowseState>();

await builder.Build().RunAsync();
