using Microsoft.AspNetCore.Components;

namespace Respondeo.UnitTests.TestSupport;

/// <summary>
/// Minimal <see cref="NavigationManager"/> that lets tests drive <c>LocationChanged</c> via
/// <see cref="NavigationManager.NavigateTo(string, bool)"/>. Constructed with an absolute URI; the
/// base URI is fixed to <c>https://localhost/</c> to match the app's test conventions.
/// </summary>
internal sealed class TestNavigationManager : NavigationManager
{
    private const string BaseUri = "https://localhost/";

    public TestNavigationManager(string uri) => Initialize(BaseUri, uri);

    protected override void NavigateToCore(string uri, bool forceLoad)
    {
        var absolute = ToAbsoluteUri(uri).ToString();
        Uri = absolute;
        NotifyLocationChanged(isInterceptedLink: false);
    }
}
