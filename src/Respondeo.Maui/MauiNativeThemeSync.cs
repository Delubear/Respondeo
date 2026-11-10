using Respondeo.Services;

namespace Respondeo.Maui;

/// <summary>
/// Matches the content and Android WebView to the theme's CSS <c>--paper</c> color,
/// while keeping the page's system-bar gutters black.
/// </summary>
public sealed class MauiNativeThemeSync : INativeThemeSync
{
    private static readonly Color LightPaper = Color.FromArgb("#FAF9F6");
    private static readonly Color DarkPaper = Color.FromArgb("#15140F");

    /// <summary>The most recently applied color, reused when the web view finishes initializing.</summary>
    internal static Color LastColor { get; private set; } = LightPaper;

    public void Apply(Theme theme)
    {
        var color = theme == Theme.Dark ? DarkPaper : LightPaper;
        LastColor = color;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (Application.Current?.Windows.FirstOrDefault()?.Page is MainPage page)
            {
                ApplyToPage(page, color);
            }

#if ANDROID
            ApplyToWindow();
#endif
        });
    }

    internal static void ApplyToPage(MainPage page, Color color)
    {
        page.BackgroundColor = Colors.Black;

        if (page.Content is VisualElement content)
        {
            content.BackgroundColor = color;
        }

        page.WebView.BackgroundColor = color;

#if ANDROID
        if (page.WebView.Handler?.PlatformView is Android.Webkit.WebView webView)
        {
            webView.SetBackgroundColor(ToAndroid(color));
        }

        ApplyToWindow();
#endif
    }

#if ANDROID
    internal static void ApplyToWindow()
    {
        var window = Platform.CurrentActivity?.Window;
        if (window is null)
        {
            return;
        }

        // The system bar gutters stay black to match the masthead; only the content area follows the theme.
        window.DecorView.SetBackgroundColor(Android.Graphics.Color.Black);

        var controller = AndroidX.Core.View.WindowCompat.GetInsetsController(window, window.DecorView);
        controller.AppearanceLightStatusBars = false;
        controller.AppearanceLightNavigationBars = false;
    }

    private static Android.Graphics.Color ToAndroid(Color color) =>
        Android.Graphics.Color.Argb(
            (int)(color.Alpha * 255), (int)(color.Red * 255), (int)(color.Green * 255), (int)(color.Blue * 255));
#endif
}
