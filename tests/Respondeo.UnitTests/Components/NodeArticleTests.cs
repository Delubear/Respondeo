using Bunit;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Respondeo.Components;
using Respondeo.Content.Abstractions;

namespace Respondeo.UnitTests.Components;

public class NodeArticleTests : TestContext
{
    public NodeArticleTests()
    {
        // NodeArticle resolves branch targets through IContentService; none are needed for these tests.
        var content = Substitute.For<IContentService>();
        content.GetByIdAsync(Arg.Any<string>()).Returns((ContentNode?)null);
        Services.AddSingleton(content);
    }

    private static ContentNode NodeWithNextStage(string href) => new()
    {
        Id = "what-is-god-like",
        Title = "What is God like?",
        BodyHtml = "<p>Body</p>",
        NextStage = new StageLink { Href = href, Label = "Why Jesus?" },
    };

    [Fact]
    public void Next_stage_href_drops_a_leading_slash_so_it_respects_the_base_href()
    {
        // A root-absolute href like "/why-jesus" would ignore <base href> and escape the app's
        // sub-path when hosted under one (e.g. GitHub Pages). It must render as relative.
        var cut = RenderComponent<NodeArticle>(p => p.Add(c => c.Node, NodeWithNextStage("/why-jesus")));

        Assert.Equal("why-jesus", cut.Find("a.stage-card").GetAttribute("href"));
    }

    [Fact]
    public void Next_stage_href_left_unchanged_when_already_relative()
    {
        var cut = RenderComponent<NodeArticle>(p => p.Add(c => c.Node, NodeWithNextStage("why-jesus")));

        Assert.Equal("why-jesus", cut.Find("a.stage-card").GetAttribute("href"));
    }
}
