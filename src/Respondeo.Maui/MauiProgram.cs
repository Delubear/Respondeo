using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Respondeo.Content;
using Respondeo.Content.Summa;
using Respondeo.LiturgicalCalendar;
using Respondeo.LiturgyOfTheHours;
using Respondeo.Services;

namespace Respondeo.Maui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();

        // Registered before AddRespondeoUI so it wins over the RCL's TryAddSingleton browser default.
        builder.Services.AddSingleton<IHostCapabilities, MauiHostCapabilities>();

        builder.Services.AddMauiBlazorWebView();

        // BlazorWebView serves the bundled static assets from this virtual origin.
        builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri("https://0.0.0.0/") });

        // Feature toggles, bound from the "FeatureFlags" section of the embedded appsettings.json (same shape as the web host's).
        using (var settings = typeof(MauiProgram).Assembly.GetManifestResourceStream("Respondeo.Maui.appsettings.json"))
        {
            if (settings is not null)
            {
                builder.Configuration.AddJsonStream(settings);
            }
        }

        builder.Services.AddFeatureFlags(builder.Configuration);

        builder.Services
            .AddRespondeoContent()
            .AddRespondeoLiturgy()
            .AddRespondeoHours()
            .AddRespondeoSumma();

        builder.Services.AddRespondeoUI();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
