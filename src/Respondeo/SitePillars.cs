namespace Respondeo;

/// <summary>
/// Central definition of the site's top-level pillars (the primary "pillar switcher" tier of the
/// masthead). This is the single source of truth: add a pillar here and it appears in the switcher.
/// A pillar is a whole area of the site (the Faith Journey, the Summa, the Miracles catalog); it sits
/// one tier above the per-pillar navigation (e.g. the journey stages in <see cref="SiteNavigation"/>).
/// </summary>
public static class SitePillars
{
    /// <summary>A single top-level pillar destination.</summary>
    /// <param name="Id">A stable identifier used to detect the active pillar.</param>
    /// <param name="Href">The relative route the pillar entry links to (empty string is Home/Journey).</param>
    /// <param name="Label">The visible switcher text.</param>
    public sealed record Pillar(string Id, string Href, string Label);

    /// <summary>The Faith Journey pillar id (the default / home pillar).</summary>
    public const string JourneyId = "journey";

    /// <summary>The Summa pillar id.</summary>
    public const string SummaId = "summa";

    /// <summary>The Miracles pillar id.</summary>
    public const string MiraclesId = "miracles";

    /// <summary>The pillars, in display order. The Journey is first because it is the site's spine.</summary>
    public static readonly IReadOnlyList<Pillar> Items =
    [
        new(JourneyId, "", "The Journey"),
        new(SummaId, "summa", "Summa Theologiae"),
        new(MiraclesId, "miracles", "Miracles"),
    ];

    /// <summary>
    /// Resolves which pillar the given relative path belongs to. Summa and Miracles are matched by
    /// route prefix; everything else (Home, the journey stages, the article browser) is the Journey.
    /// </summary>
    /// <param name="relativePath">The current path relative to the app base, without a leading slash.</param>
    public static string ResolveActiveId(string relativePath)
    {
        var path = relativePath.TrimStart('/');

        if (StartsWithSegment(path, "summa"))
        {
            return SummaId;
        }

        if (StartsWithSegment(path, "miracles"))
        {
            return MiraclesId;
        }

        return JourneyId;
    }

    // Matches a route prefix on segment boundaries so "summa" and "summa/prima-q001" match, but a
    // hypothetical unrelated route that merely starts with the same letters would not.
    private static bool StartsWithSegment(string path, string segment) =>
        path.Equals(segment, StringComparison.OrdinalIgnoreCase)
        || path.StartsWith(segment + "/", StringComparison.OrdinalIgnoreCase);
}
