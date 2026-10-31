namespace Respondeo.Navigation;

/// <summary>
/// Which floating guides/orbs a given route shows. The orbs live in <c>MainLayout</c> (not the pages)
/// so each persists as a single element across in-area navigation and animates only on true first appearance;
/// this record is the resolved decision the layout binds to.
/// </summary>
/// <param name="ShowLiturgicalOrb">Whether the liturgical calendar orb is rendered.</param>
/// <param name="LiturgicalOrbRaised">Whether that orb sits in its raised slot (above a sibling guide).</param>
/// <param name="ShowPrayerGuide">Whether the "guide to prayers" orb is rendered.</param>
/// <param name="ShowSaintGlossary">Whether the saint glossary orb is rendered.</param>
/// <param name="ShowSummaGuide">Whether the "how to read the Summa" guide is rendered.</param>
/// <param name="ShowHourSettings">Whether the Liturgy of the Hours settings orb is rendered.</param>
public sealed record FloatingGuideState(
    bool ShowLiturgicalOrb,
    bool LiturgicalOrbRaised,
    bool ShowPrayerGuide,
    bool ShowSaintGlossary,
    bool ShowSummaGuide,
    bool ShowHourSettings = false);

/// <summary>
/// Single source of truth for which floating guides/orbs appear on which routes.
/// Mirrors <see cref="SiteNavigation"/> and <see cref="DiscoverNavigation"/>: declare an orb's routes here once and
/// <c>MainLayout</c> renders them consistently. Keeping the route strings in one place avoids magic strings
/// drifting out of sync with the pages' <c>@page</c> routes.
/// </summary>
public static class FloatingGuideRoutes
{
    /// <summary>Discover-area browse pages that show only the base (unraised) liturgical orb.</summary>
    private static readonly string[] LiturgicalBaseRoutes =
    [
        RoutePath.Discover,
        $"{RoutePath.Discover}/{RoutePath.Miracles}",
        $"{RoutePath.Discover}/{RoutePath.Devotions}",
        $"{RoutePath.Discover}/{RoutePath.Articles}",
    ];

    /// <summary>Prayer browse route. Its detail pages nest beneath it.</summary>
    private const string PrayersBrowse = $"{RoutePath.Discover}/{RoutePath.Prayers}";

    /// <summary>Saint browse route. Its detail pages nest beneath it.</summary>
    private const string SaintsBrowse = $"{RoutePath.Discover}/{RoutePath.Saints}";

    /// <summary>Summa browse route. Its question pages nest beneath it.</summary>
    private const string SummaBrowse = RoutePath.Summa;

    /// <summary>Liturgy of the Hours browse route. Its hour player pages nest beneath it.</summary>
    private const string HoursBrowse = $"{RoutePath.Discover}/hours";

    /// <summary>
    /// Resolves which orbs a route shows. The prayers and saints browse pages raise the liturgical orb above their sibling guide;
    /// their detail pages keep only the sibling guide.
    /// The caller may pass either a raw relative URL or an already-normalized route — <see cref="Normalize"/> is idempotent.
    /// </summary>
    public static FloatingGuideState Resolve(string relativePath)
    {
        var route = Normalize(relativePath);

        var prayers = IsIn(route, PrayersBrowse);
        var saints = IsIn(route, SaintsBrowse);
        var summa = IsIn(route, SummaBrowse);

        // The orb only raises on the browse pages, where it stacks above the prayer/saint guide.
        var liturgicalRaised = route == PrayersBrowse || route == SaintsBrowse;
        var liturgicalBase = Array.IndexOf(LiturgicalBaseRoutes, route) >= 0;

        return new FloatingGuideState(
            ShowLiturgicalOrb: liturgicalBase || liturgicalRaised,
            LiturgicalOrbRaised: liturgicalRaised,
            ShowPrayerGuide: prayers,
            ShowSaintGlossary: saints,
            ShowSummaGuide: summa,
            ShowHourSettings: route == HoursBrowse || route.StartsWith($"{HoursBrowse}/", StringComparison.Ordinal));
    }

    /// <summary>
    /// Normalizes a relative URL to a comparable route key: drops any query/fragment, trims slashes, and lower-cases. Safe to call on an already-normalized value.
    /// </summary>
    public static string Normalize(string relativePath) => RoutePath.StripQueryAndFragment(relativePath).ToLowerInvariant();

    /// <summary>True when <paramref name="route"/> is the browse route itself or one of its nested detail routes.</summary>
    private static bool IsIn(string route, string browseRoute) => route == browseRoute || route.StartsWith($"{browseRoute}/", StringComparison.Ordinal);
}
