using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Respondeo;
using Respondeo.Content.Markdown;
using Respondeo.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Content is author-curated Markdown loaded once and cached for the app lifetime.
// The parser and loader implementations are internal to Respondeo.Content.Markdown; the app depends only on IContentService.
builder.Services.AddRespondeoContent();

// Tracks the visitor's navigation path (persisted in sessionStorage) for breadcrumbs.
builder.Services.AddScoped<IBreadcrumbTrail, BreadcrumbTrail>();

// Applies and persists the visitor's preferred light/dark theme.
builder.Services.AddScoped<IThemeService, ThemeService>();

// Signals whether a navigation should reset to the top (masthead nav) or scroll to content (cards/articles).
builder.Services.AddScoped<NavigationIntent>();

await builder.Build().RunAsync();
