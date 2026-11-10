using Respondeo.Services;

namespace Respondeo.Maui;

/// <summary>
/// Paints the window, page and Android WebView with the CSS <c>--paper</c> color of the current theme,
/// so switching back to the app never flashes a mismatched native background.
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
            Platform.CurrentActivity?.Window?.DecorView.SetBackgroundColor(ToAndroid(color));
#endif
        });
    }

    internal static void ApplyToPage(MainPage page, Color color)
    {
        page.BackgroundColor = color;

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
#endif
    }

#if ANDROID
    private static Android.Graphics.Color ToAndroid(Color color) =>
        Android.Graphics.Color.Argb(
            (int)(color.Alpha * 255), (int)(color.Red * 255), (int)(color.Green * 255), (int)(color.Blue * 255));
#endif
}
