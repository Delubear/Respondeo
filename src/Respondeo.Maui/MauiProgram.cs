using Microsoft.Extensions.Logging;
using Respondeo.Services;

namespace Respondeo.Maui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();

        // Registered before AddRespondeoApplication so the native adapters win over shared TryAdd defaults.
        builder.Services.AddSingleton<IHostCapabilities, MauiHostCapabilities>();
        builder.Services.AddSingleton<INativeThemeSync, MauiNativeThemeSync>();
        builder.Services.AddSingleton(BuildInfo.Load(typeof(MauiProgram).Assembly));

        builder.Services.AddMauiBlazorWebView();

        // BlazorWebView serves the bundled static assets from this virtual origin; .NET-side requests never reach the
        // WebView, so the handler reads the same files straight from the app package.
        builder.Services.AddScoped(_ => new HttpClient(new AppPackageHttpHandler()) { BaseAddress = new Uri("https://0.0.0.0/") });

        builder.Configuration.AddRequiredEmbeddedSettings(typeof(MauiProgram).Assembly, "Respondeo.Maui.appsettings.json");
        builder.Services.AddRespondeoApplication(builder.Configuration);

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
