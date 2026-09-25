using Respondeo;
using Respondeo.Content.Abstractions;

namespace Respondeo.UnitTests.Routing;

/// <summary>
/// Verifies node URLs are nested under their stage so the section shows in the URL and the
/// masthead tab lights up, while stageless nodes keep the flat route.
/// </summary>
public class ContentRoutesTests
{
    [Fact]
    public void Node_with_a_stage_is_nested_under_the_stage_slug()
    {
        Assert.Equal("why-god/node/aquinas-five-ways", ContentRoutes.NodeHref("aquinas-five-ways", "why-god"));
    }

    [Fact]
    public void Node_without_a_stage_uses_the_flat_route()
    {
        Assert.Equal("node/glossary", ContentRoutes.NodeHref("glossary", null));
    }

    [Fact]
    public void Empty_stage_is_treated_as_stageless()
    {
        Assert.Equal("node/test", ContentRoutes.NodeHref("test", string.Empty));
    }

    [Fact]
    public void Node_overload_uses_the_nodes_stage()
    {
        var node = new ContentNode
        {
            Id = "what-is-god-like",
            Title = "What is God like?",
            BodyHtml = string.Empty,
            Stage = "why-god",
        };

        Assert.Equal("why-god/node/what-is-god-like", ContentRoutes.NodeHref(node));
    }
}
