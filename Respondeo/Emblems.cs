namespace Respondeo;

/// <summary>
/// Central source of truth for the small section emblems (SVG path data) used across the
/// site. Both the Home reel cards and the "Next Stage" cards resolve icons from here so a
/// section always shows the same symbol wherever it appears.
/// </summary>
public static class Emblems
{
    /// <summary>
    /// Maps an emblem name (e.g. from a next-stage <c>icon:</c> front-matter value) to its
    /// SVG path data. The question mark is the default so an unspecified transition reads as
    /// an open question rather than a fixed symbol.
    /// </summary>
    public static string Path(string? icon) => (icon?.Trim().ToLowerInvariant()) switch
    {
        // Cross — Why Jesus.
        "cross" => "M10 2h4v6h6v4h-6v10h-4V12H4V8h6z",
        // Compass — Which God.
        "compass" => "M12 2a10 10 0 1 0 0 20 10 10 0 0 0 0-20zm0 2a8 8 0 1 1 0 16 8 8 0 0 1 0-16zm4 4-5.5 2.5L8 16l5.5-2.5L16 8zm-4 3.2a.8.8 0 1 1 0 1.6.8.8 0 0 1 0-1.6z",
        // Scroll / open book.
        "scroll" => "M6 2h11a3 3 0 0 1 3 3v12a3 3 0 0 1-3 3H7a3 3 0 0 1-3-3V4a2 2 0 0 1 2-2zm0 2v13a1 1 0 0 0 1 1h8.2A3 3 0 0 1 15 17V5a1 1 0 0 0-1-1H6zm3 3h6v2H9V7zm0 4h6v2H9v-2z",
        // Church — Why the Church.
        "church" => "M11 2h2v2h2v2h-2v2.2l6 3.6V22h-5v-5a2 2 0 0 0-4 0v5H5V11.8l6-3.6V6H9V4h2V2zm1 8.3-5 3V20h1v-3a4 4 0 0 1 8 0v3h1v-6.7l-5-3z",
        // Home / hearth — Coming Home.
        "home" => "M12 3 2 11h3v9h6v-6h2v6h6v-9h3L12 3z",
        // Question mark — default (e.g. Why God, and any unspecified transition).
        _ => "M11 18h2v-2h-2v2zm1-16C6.5 2 2 6.5 2 12s4.5 10 10 10 10-4.5 10-10S17.5 2 12 2zm0 18c-4.4 0-8-3.6-8-8s3.6-8 8-8 8 3.6 8 8-3.6 8-8 8zm0-14a4 4 0 0 0-4 4h2a2 2 0 1 1 3 1.7c-.8.6-2 1.3-2 3.3h2c0-1.3 1.2-1.6 2.2-2.6A4 4 0 0 0 12 6z",
    };

    /// <summary>
    /// Maps a stage slug (as defined in <see cref="SiteNavigation"/>) to its emblem name.
    /// Used by the Home reel so each stage card shows the icon that represents that section.
    /// </summary>
    public static string ForStage(string slug) => (slug?.Trim().ToLowerInvariant()) switch
    {
        "why-god" => "question",
        "which-god" => "compass",
        "why-jesus" => "cross",
        "why-the-church" => "church",
        "coming-home" => "home",
        _ => "question",
    };
}
