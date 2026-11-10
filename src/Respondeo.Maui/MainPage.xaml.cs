namespace Respondeo.Maui;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
    }

    /// <summary>The page's native views, so the theme sync can paint everything behind the web content.</summary>
    internal Microsoft.AspNetCore.Components.WebView.Maui.BlazorWebView WebView => blazorWebView;

    private void OnBlazorWebViewInitialized(object? sender, Microsoft.AspNetCore.Components.WebView.BlazorWebViewInitializedEventArgs e)
    {
#if ANDROID
        // Keep the page's tiles rasterized while the app is backgrounded, so resuming shows the existing
        // content instead of a blank paper frame while Chromium re-rasterizes the text.
        e.WebView.Settings.OffscreenPreRaster = true;
#endif
        MauiNativeThemeSync.ApplyToPage(this, MauiNativeThemeSync.LastColor);
    }
}
