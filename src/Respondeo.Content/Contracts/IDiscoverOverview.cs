namespace Respondeo.Content.Discover;

/// <summary>
/// A thin composition facade for the Discover landing page.
/// It does not own any content; it simply warms the four independent content services (prayers, devotions, articles, miracles)
/// so the section pages load instantly once a reader chooses one.
/// </summary>
public interface IDiscoverOverview
{
    /// <summary>Warms every Discover content index in parallel so the section pages render without a cold fetch.</summary>
    Task WarmAsync();
}
