namespace Respondeo.Maui;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
    }

    /// <summary>The page's native views, so the theme sync can paint everything behind the web content.</summary>
    internal Microsoft.AspNetCore.Components.WebView.Maui.BlazorWebView WebView => blazorWebView;

    private void OnBlazorWebViewInitialized(object? sender, Microsoft.AspNetCore.Components.WebView.BlazorWebViewInitializedEventArgs e) =>
        MauiNativeThemeSync.ApplyToPage(this, MauiNativeThemeSync.LastColor);
}
