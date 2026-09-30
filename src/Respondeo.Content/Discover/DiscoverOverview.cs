using Respondeo.Content.Contracts;

namespace Respondeo.Content.Discover;

/// <summary>
/// The <see cref="IDiscoverOverview"/> implementation. A thin facade over the four peer content services;
/// it holds no state of its own and only fans out to warm each index so the landing page can pre-load the whole pillar in one call.
/// </summary>
internal sealed class DiscoverOverview(IPrayerService prayers, IDevotionService devotions, IArticleService articles, IMiracleService miracles) : IDiscoverOverview
{
    /// <summary>Warms every Discover content index in parallel so the section pages render without a cold fetch.</summary>
    public Task WarmAsync() => Task.WhenAll(
        prayers.GetIndexAsync(),
        devotions.GetIndexAsync(),
        articles.GetIndexAsync(),
        miracles.GetIndexAsync());
}
