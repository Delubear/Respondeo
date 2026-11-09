namespace Respondeo.Services;

/// <summary>
/// Describes what the hosting shell supports. The browser (PWA) host supports the service worker and install prompt;
/// native hosts such as MAUI register their own implementation that turns these features off.
/// </summary>
public interface IHostCapabilities
{
    /// <summary>Whether PWA-only features (install prompt, service-worker offline packs) are available.</summary>
    bool SupportsPwa { get; }
}

/// <summary>Default capabilities for the browser host.</summary>
public sealed class BrowserHostCapabilities : IHostCapabilities
{
    public bool SupportsPwa => true;
}
