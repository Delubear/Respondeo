using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Respondeo;
using Respondeo.Content.Summa;
using Respondeo.Content;
using Respondeo.LiturgicalCalendar;
using Respondeo.LiturgyOfTheHours;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Deploy-time feature toggles, bound from the "FeatureFlags" section of wwwroot/appsettings.json.
var featureFlags = builder.Configuration.GetSection(FeatureFlags.SectionName).Get<FeatureFlags>() ?? new FeatureFlags();
builder.Services.AddSingleton(featureFlags);

// Content libraries: the hand-authored library, the liturgical calendar engine, and the bundled Summa corpus.
builder.Services
    .AddRespondeoContent()
    .AddRespondeoLiturgy()
    .AddRespondeoHours()
    .AddRespondeoSumma();

// App services, grouped by concern (see Services/ServiceCollectionExtensions.cs).
builder.Services
    .AddRespondeoBrowse()
    .AddRespondeoReaderState()
    .AddRespondeoToday()
    .AddRespondeoInterop();

var host = builder.Build();

// Translation catalogs (books, names, chapter counts, abbreviations) come from the bundled manifests.
await Respondeo.Bible.BibleCatalog.LoadAsync(new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

await host.RunAsync();
