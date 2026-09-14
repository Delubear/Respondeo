using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Respondeo;
using Respondeo.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Content is author-curated Markdown loaded once and cached for the app lifetime.
// Registered as scoped because it depends on the scoped HttpClient (a singleton
// cannot consume a scoped service in Blazor WebAssembly). In a WASM app there is a
// single client-side scope, so the cache still lives for the app lifetime.
builder.Services.AddScoped<ContentService>();

await builder.Build().RunAsync();
