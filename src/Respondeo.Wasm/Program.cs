using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Respondeo;
using Respondeo.Content.Summa;
using Respondeo.Content;
using Respondeo.LiturgicalCalendar;
using Respondeo.LiturgyOfTheHours;
using Respondeo.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Deploy-time feature toggles, bound from the "FeatureFlags" section of wwwroot/appsettings.json.
builder.Services.AddFeatureFlags(builder.Configuration);

// Content libraries: the hand-authored library, the liturgical calendar engine, and the bundled Summa corpus.
builder.Services
    .AddRespondeoContent()
    .AddRespondeoLiturgy()
    .AddRespondeoHours()
    .AddRespondeoSumma();

// App services from the Respondeo.UI library (see Services/AppServiceRegistration.cs there).
builder.Services.AddRespondeoUI();

await builder.Build().RunAsync();
