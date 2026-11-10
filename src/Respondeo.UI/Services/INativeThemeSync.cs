namespace Respondeo.Services;

/// <summary>
/// Lets a native host paint its own views (window, page, web view) to match the in-app theme,
/// so nothing light flashes behind the web content when the app is resumed or redrawn.
/// </summary>
public interface INativeThemeSync
{
    /// <summary>Applies the given theme's background to the host's native views.</summary>
    void Apply(Theme theme);
}

/// <summary>The browser default: the page itself is the only surface, so there is nothing native to paint.</summary>
public sealed class NoOpNativeThemeSync : INativeThemeSync
{
    public void Apply(Theme theme)
    {
    }
}
