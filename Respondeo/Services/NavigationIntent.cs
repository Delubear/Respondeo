namespace Respondeo.Services;

/// <summary>
/// Carries the intent of the most recent navigation from the element that triggered it to the
/// scroll handler that runs on <c>LocationChanged</c>.
/// </summary>
/// <remarks>
/// The masthead nav tabs and the entry-point cards can point at the same route (e.g. both the
/// "Why God?" tab and the "Why God?" card navigate to <c>why-god</c>), so the destination URL
/// alone cannot distinguish them. The masthead nav sets <see cref="ResetToTop"/> before it
/// navigates; the scroll handler reads and clears it to decide whether to jump to the very top
/// (top-level navigation) or scroll down to the content region (card/article navigation).
/// </remarks>
public sealed class NavigationIntent
{
    /// <summary>
    /// When true, the next navigation should reset the viewport to the very top of the page.
    /// Defaults to false, meaning navigation scrolls down to the content region.
    /// </summary>
    public bool ResetToTop { get; set; }

    /// <summary>Reads the pending intent and resets it to the default for the next navigation.</summary>
    public bool ConsumeResetToTop()
    {
        var value = ResetToTop;
        ResetToTop = false;
        return value;
    }
}
