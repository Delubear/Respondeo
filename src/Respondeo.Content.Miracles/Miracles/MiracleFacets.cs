namespace Respondeo.Content.Miracles;

/// <summary>
/// The kind of miracle, used as the primary categorization facet.
/// Stored as a stable slug in front-matter (e.g. "eucharistic") and mapped to this enum.
/// </summary>
public enum MiracleType
{
    /// <summary>A Eucharistic miracle (host/wine becoming visible flesh and blood, bleeding hosts, etc.).</summary>
    Eucharistic,

    /// <summary>An approved or reported Marian apparition.</summary>
    Marian,

    /// <summary>A physical healing attributed to God's intervention (e.g. Lourdes cures).</summary>
    Healing,

    /// <summary>An incorrupt body of a saint or blessed.</summary>
    Incorruptible,

    /// <summary>The stigmata (the wounds of Christ) borne by a saint.</summary>
    Stigmata,

    /// <summary>A weeping, bleeding, or otherwise miraculous image or statue.</summary>
    Image,

    /// <summary>Any well-attested miracle that does not fit the other categories.</summary>
    Other,
}

/// <summary>
/// The Church's stance on a reported miracle, used as a credibility facet.
/// </summary>
public enum ApprovalStatus
{
    /// <summary>Formally approved or recognized by competent Church authority.</summary>
    Approved,

    /// <summary>Currently under formal investigation; no verdict yet.</summary>
    UnderInvestigation,

    /// <summary>Examined and not approved (a negative or "nothing supernatural" judgment).</summary>
    NotApproved,

    /// <summary>A historical case venerated by long tradition that predates modern formal processes.</summary>
    Historical,
}

/// <summary>
/// Broad geographic grouping used as a coarse browse facet, independent of the free-text country.
/// </summary>
public enum MiracleRegion
{
    Europe,
    NorthAmerica,
    LatinAmerica,
    Africa,
    Asia,
    MiddleEast,
    Oceania,
    Unknown,
}

/// <summary>
/// Maps the typed facet enums to their stable front-matter slugs and human-readable display labels,
/// keeping the authored file vocabulary and the UI labels decoupled from the enum member names.
/// Mirrors the intent of the Summa's SummaParts registry.
/// </summary>
public static class MiracleFacets
{
    private static readonly IReadOnlyDictionary<string, MiracleType> TypeBySlug = new Dictionary<string, MiracleType>(StringComparer.OrdinalIgnoreCase)
    {
        ["eucharistic"] = MiracleType.Eucharistic,
        ["marian"] = MiracleType.Marian,
        ["healing"] = MiracleType.Healing,
        ["incorruptible"] = MiracleType.Incorruptible,
        ["stigmata"] = MiracleType.Stigmata,
        ["image"] = MiracleType.Image,
        ["other"] = MiracleType.Other,
    };

    private static readonly IReadOnlyDictionary<string, ApprovalStatus> ApprovalBySlug = new Dictionary<string, ApprovalStatus>(StringComparer.OrdinalIgnoreCase)
    {
        ["approved"] = ApprovalStatus.Approved,
        ["under-investigation"] = ApprovalStatus.UnderInvestigation,
        ["investigating"] = ApprovalStatus.UnderInvestigation,
        ["not-approved"] = ApprovalStatus.NotApproved,
        ["historical"] = ApprovalStatus.Historical,
    };

    private static readonly IReadOnlyDictionary<string, MiracleRegion> RegionBySlug = new Dictionary<string, MiracleRegion>(StringComparer.OrdinalIgnoreCase)
    {
        ["europe"] = MiracleRegion.Europe,
        ["north-america"] = MiracleRegion.NorthAmerica,
        ["latin-america"] = MiracleRegion.LatinAmerica,
        ["africa"] = MiracleRegion.Africa,
        ["asia"] = MiracleRegion.Asia,
        ["middle-east"] = MiracleRegion.MiddleEast,
        ["oceania"] = MiracleRegion.Oceania,
    };

    /// <summary>Parses a type slug, falling back to <see cref="MiracleType.Other"/> when unknown.</summary>
    public static MiracleType ParseType(string? slug) =>
        slug is not null && TypeBySlug.TryGetValue(slug.Trim(), out var value) ? value : MiracleType.Other;

    /// <summary>Parses an approval slug, falling back to <see cref="ApprovalStatus.Historical"/> when unknown.</summary>
    public static ApprovalStatus ParseApproval(string? slug) =>
        slug is not null && ApprovalBySlug.TryGetValue(slug.Trim(), out var value) ? value : ApprovalStatus.Historical;

    /// <summary>Parses a region slug, falling back to <see cref="MiracleRegion.Unknown"/> when unknown.</summary>
    public static MiracleRegion ParseRegion(string? slug) =>
        slug is not null && RegionBySlug.TryGetValue(slug.Trim(), out var value) ? value : MiracleRegion.Unknown;

    /// <summary>The human-readable label for a miracle type.</summary>
    public static string Label(MiracleType type) => type switch
    {
        MiracleType.Eucharistic => "Eucharistic",
        MiracleType.Marian => "Marian apparition",
        MiracleType.Healing => "Healing",
        MiracleType.Incorruptible => "Incorruptible",
        MiracleType.Stigmata => "Stigmata",
        MiracleType.Image => "Miraculous image",
        _ => "Other",
    };

    /// <summary>The human-readable label for an approval status.</summary>
    public static string Label(ApprovalStatus status) => status switch
    {
        ApprovalStatus.Approved => "Church-approved",
        ApprovalStatus.UnderInvestigation => "Under investigation",
        ApprovalStatus.NotApproved => "Not approved",
        _ => "Historical / traditional",
    };

    /// <summary>The human-readable label for a region.</summary>
    public static string Label(MiracleRegion region) => region switch
    {
        MiracleRegion.Europe => "Europe",
        MiracleRegion.NorthAmerica => "North America",
        MiracleRegion.LatinAmerica => "Latin America",
        MiracleRegion.Africa => "Africa",
        MiracleRegion.Asia => "Asia",
        MiracleRegion.MiddleEast => "Middle East",
        MiracleRegion.Oceania => "Oceania",
        _ => "Unknown",
    };

    /// <summary>Derives the century label (e.g. "8th century", "20th century") from a year, or null when unknown.</summary>
    public static string? CenturyLabel(int? year)
    {
        if (year is null || year == 0)
        {
            return null;
        }

        var century = (int)Math.Ceiling(Math.Abs(year.Value) / 100d);
        var suffix = (century % 100) is >= 11 and <= 13
            ? "th"
            : (century % 10) switch { 1 => "st", 2 => "nd", 3 => "rd", _ => "th" };

        return year > 0 ? $"{century}{suffix} century" : $"{century}{suffix} century BC";
    }
}
