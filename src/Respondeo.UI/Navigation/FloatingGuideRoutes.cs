using Respondeo.Services;

namespace Respondeo.Navigation;

/// <summary>
/// Which floating guides/orbs a given route shows. The orbs live in <c>MainLayout</c> (not the pages)
/// so each persists as a single element across in-area navigation and animates only on true first appearance;
/// this record is the resolved decision the layout binds to.
/// </summary>
/// <param name="ShowLiturgicalOrb">Whether the liturgical calendar orb is rendered.</param>
/// <param name="ShowPrayerGuide">Whether the "guide to prayers" orb is rendered.</param>
/// <param name="ShowSaintGlossary">Whether the saint glossary orb is rendered.</param>
/// <param name="ShowSummaGuide">Whether the "how to read the Summa" guide is rendered.</param>
/// <param name="ShowHourSettings">Whether the Liturgy of the Hours settings dialog (opened by the player's gear) is mounted.</param>
/// <param name="ShowBibleGuide">Whether the "how to read the Bible" guide is rendered.</param>
public sealed record FloatingGuideState(
    bool ShowLiturgicalOrb,
    bool ShowPrayerGuide,
    bool ShowSaintGlossary,
    bool ShowSummaGuide,
    bool ShowHourSettings = false,
    bool ShowBibleGuide = false)
{
    /// <summary>
    /// The zero-based vertical slot for an orb. Visible orbs stack bottom-up in a fixed order
    /// (Bible, Summa, Saints, Prayers, Liturgical) so every visible orb gets its own slot and none overlap,
    /// whatever combination a route shows. Hidden orbs return 0.
    /// </summary>
    public int SlotOf(string orbKey)
    {
        (string Key, bool Visible)[] stack =
        [
            (OrbLauncher.Bible, ShowBibleGuide),
            (OrbLauncher.Summa, ShowSummaGuide),
            (OrbLauncher.Saints, ShowSaintGlossary),
            (OrbLauncher.Prayers, ShowPrayerGuide),
            (OrbLauncher.Liturgical, ShowLiturgicalOrb),
        ];

        var slot = 0;
        foreach (var (key, visible) in stack)
        {
            if (key == orbKey)
            {
                return visible ? slot : 0;
            }

            if (visible)
            {
                slot++;
            }
        }

        return 0;
    }
}

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

    /// <summary>Bible browse route. Its book and chapter pages nest beneath it.</summary>
    private const string BibleBrowse = "bible";

    /// <summary>
    /// Resolves which orbs a route shows. Vertical placement is derived from the result by <see cref="FloatingGuideState.SlotOf"/>.
    /// The caller may pass either a raw relative URL or an already-normalized route — <see cref="Normalize"/> is idempotent.
    /// </summary>
    public static FloatingGuideState Resolve(string relativePath, bool liturgicalCalendarEnabled = true)
    {
        var route = Normalize(relativePath);

        var prayers = IsIn(route, PrayersBrowse);
        var saints = IsIn(route, SaintsBrowse);
        var summa = IsIn(route, SummaBrowse);

        // On the prayers/saints browse pages the liturgical orb shares the screen with their guide; slots keep them apart.
        var liturgical = liturgicalCalendarEnabled && (Array.IndexOf(LiturgicalBaseRoutes, route) >= 0 || route == HoursBrowse
            || route == PrayersBrowse || route == SaintsBrowse);

        return new FloatingGuideState(
            ShowLiturgicalOrb: liturgical,
            ShowPrayerGuide: prayers,
            ShowSaintGlossary: saints,
            ShowSummaGuide: summa,
            ShowHourSettings: route.StartsWith($"{HoursBrowse}/", StringComparison.Ordinal),
            ShowBibleGuide: IsIn(route, BibleBrowse));
    }

    /// <summary>
    /// Normalizes a relative URL to a comparable route key: drops any query/fragment, trims slashes, and lower-cases. Safe to call on an already-normalized value.
    /// </summary>
    public static string Normalize(string relativePath) => RoutePath.StripQueryAndFragment(relativePath).ToLowerInvariant();

    /// <summary>True when <paramref name="route"/> is the browse route itself or one of its nested detail routes.</summary>
    private static bool IsIn(string route, string browseRoute) => route == browseRoute || route.StartsWith($"{browseRoute}/", StringComparison.Ordinal);
}
