using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Configuration;
using Respondeo;
using Respondeo.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddSingleton(BuildInfo.Load(typeof(Program).Assembly));
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

var configuration = new ConfigurationBuilder()
    .AddRequiredEmbeddedSettings(typeof(Program).Assembly, "Respondeo.appsettings.json")
    .AddConfiguration(builder.Configuration)
    .Build();
builder.Services.AddRespondeoApplication(configuration);

await builder.Build().RunAsync();
