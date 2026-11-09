using Respondeo.Content.Contracts;

namespace Respondeo.Services;

/// <summary>
/// A thin composition facade for the Discover landing page. It holds no state of its own; it simply fans out to the
/// four peer content services (prayers, devotions, articles, miracles, saints) to warm each index so the section
/// pages load instantly once a reader chooses one.
/// </summary>
public sealed class DiscoverOverview(IPrayerService prayers, IDevotionService devotions, IArticleService articles, IMiracleService miracles, ISaintService saints)
{
    /// <summary>Warms every Discover content index in parallel so the section pages render without a cold fetch.</summary>
    public Task WarmAsync() => Task.WhenAll(
        prayers.GetIndexAsync(),
        devotions.GetIndexAsync(),
        articles.GetIndexAsync(),
        miracles.GetIndexAsync(),
        saints.GetIndexAsync());
}
