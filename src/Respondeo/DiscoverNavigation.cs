using Respondeo.Services;

namespace Respondeo;

/// <summary>
/// Central definition of the Discover Catholicism pillar's sub-navigation (its peer tabs).
/// This is the single source of truth for the Discover area, mirroring <see cref="SiteNavigation"/>
/// for the primary journey masthead: add, reorder, or relabel a Discover tab here and it updates everywhere.
/// </summary>
public static class DiscoverNavigation
{
    /// <summary>A single Discover sub-area tab.</summary>
    /// <param name="Href">The relative route (e.g. "discover/saints").</param>
    /// <param name="Label">The visible tab text.</param>
    /// <param name="Exact">When true the tab only highlights on an exact route match (used by the landing link).</param>
    /// <param name="IsEnabled">Granular feature gate that hides this tab when its sub-area is disabled.</param>
    public sealed record Tab(string Href, string Label, bool Exact, Func<FeatureFlags, bool> IsEnabled);

    /// <summary>
    /// The Discover landing plus one tab per sub-area, in display order.
    /// Miracles sits alongside the prayers/devotions/articles trio as a peer.
    /// The landing link is exact so it only highlights on /discover itself.
    /// Every tab is additionally gated by the DiscoverPillar master switch at the call site.
    /// </summary>
    public static readonly IReadOnlyList<Tab> Items =
    [
        new("discover", "Overview", true, _ => true),
        new("discover/miracles", "Miracles", false, f => f.MiraclesFeature),
        new("discover/saints", "Saints", false, f => f.SaintsFeature),
        new("discover/prayers", "Prayers", false, f => f.PrayerFeature),
        new("discover/devotions", "Devotions", false, f => f.DevotionsFeature),
        new("discover/articles", "Articles", false, f => f.ArticlesFeature),
    ];
}
