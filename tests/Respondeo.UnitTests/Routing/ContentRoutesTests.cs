using Respondeo;
using Respondeo.Content.Contracts;

namespace Respondeo.UnitTests.Routing;

/// <summary>
/// Verifies node URLs are nested under their stage so the section shows in the URL and the
/// masthead tab lights up. Every node belongs to a stage, so a missing stage is a content error.
/// </summary>
public class ContentRoutesTests
{
    [Fact]
    public void Node_with_a_stage_is_nested_under_the_stage_slug()
    {
        Assert.Equal("why-god/aquinas-five-ways", ContentRoutes.NodeHref("aquinas-five-ways", "why-god"));
    }

    [Fact]
    public void Node_without_a_stage_throws()
    {
        Assert.Throws<ArgumentException>(() => ContentRoutes.NodeHref("glossary", null));
    }

    [Fact]
    public void Empty_stage_throws()
    {
        Assert.Throws<ArgumentException>(() => ContentRoutes.NodeHref("test", string.Empty));
    }

    [Fact]
    public void Node_overload_uses_the_nodes_stage()
    {
        var node = new InquiryNode
        {
            Id = "what-is-god-like",
            Title = "What is God like?",
            BodyHtml = string.Empty,
            Stage = "why-god",
        };

        Assert.Equal("why-god/what-is-god-like", ContentRoutes.NodeHref(node));
    }

    [Fact]
    public void Discover_detail_routes_sit_under_their_catalog()
    {
        Assert.Equal("discover/prayers/hail-mary", ContentRoutes.PrayerHref("hail-mary"));
        Assert.Equal("discover/saints/augustine", ContentRoutes.SaintHref("augustine"));
        Assert.Equal("discover/miracles/lanciano", ContentRoutes.MiracleHref("lanciano"));
        Assert.Equal("discover/devotions/rosary", ContentRoutes.DevotionHref("rosary"));
        Assert.Equal("discover/articles/what-is-grace", ContentRoutes.ArticleHref("what-is-grace"));
    }
}
