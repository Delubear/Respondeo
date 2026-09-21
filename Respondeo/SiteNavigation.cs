namespace Respondeo;

/// <summary>
/// Central definition of the primary site navigation (the persistent masthead nav).
/// This is the single source of truth: change a label or route here and it updates everywhere.
/// Reordering, renaming (e.g. switching to the "path" wording), or adding a stage is a one-line edit in <see cref="Items"/>.
/// </summary>
public static class SiteNavigation
{
    /// <summary>A single top-level navigation destination.</summary>
    /// <param name="Href">The relative route (empty string is Home).</param>
    /// <param name="Label">The visible tab text.</param>
    public sealed record NavItem(string Href, string Label);

    /// <summary>The primary navigation stages, in journey order.</summary>
    public static readonly IReadOnlyList<NavItem> Items =
    [
        new("", "Home"),
        new("why-god", "Why God?"),
        new("why-jesus", "Why Jesus?"),
        new("why-the-church", "Why the Church?"),
        new("coming-home", "Coming Home"),
    ];
}
