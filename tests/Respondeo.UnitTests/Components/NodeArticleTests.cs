using Bunit;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Respondeo.Components;
using Respondeo.Content.Contracts;
using Respondeo.Services;

namespace Respondeo.UnitTests.Components;

public class NodeArticleTests : TestContext
{
    private readonly IInquiryFlow _flow = Substitute.For<IInquiryFlow>();

    public NodeArticleTests()
    {
        // NodeArticle resolves branch targets through IContentService; none are needed for these tests.
        var content = Substitute.For<IContentService>();
        content.GetByIdAsync(Arg.Any<string>()).Returns((InquiryNode?)null);
        Services.AddSingleton(content);

        // NodeArticle sources branches and the stage transition from IInquiryFlow (flow.json).
        _flow.GetBranchesAsync(Arg.Any<string>()).Returns((IReadOnlyList<BranchLink>)[]);
        _flow.GetTransitionForNodeAsync(Arg.Any<string>(), Arg.Any<string?>()).Returns((StageLink?)null);
        Services.AddSingleton(_flow);

        // NodeArticle reads the visited set to mark already-seen branch cards.
        var visited = Substitute.For<IVisitedNodes>();
        visited.GetVisitedAsync().Returns((IReadOnlySet<string>)new HashSet<string>());
        Services.AddSingleton(visited);
    }

    private static InquiryNode Node() => new()
    {
        Id = "what-is-god-like",
        Title = "What is God like?",
        BodyHtml = "<p>Body</p>",
    };

    private void TransitionReturns(string href) =>
        _flow.GetTransitionForNodeAsync(Arg.Any<string>(), Arg.Any<string?>())
            .Returns(new StageLink { Href = href, Label = "Why Jesus?" });

    [Fact]
    public void Next_stage_href_drops_a_leading_slash_so_it_respects_the_base_href()
    {
        // A root-absolute href like "/why-jesus" would ignore <base href> and escape the app's
        // sub-path when hosted under one (e.g. GitHub Pages). It must render as relative.
        TransitionReturns("/why-jesus");

        var cut = RenderComponent<NodeArticle>(p => p.Add(c => c.Node, Node()));

        Assert.Equal("why-jesus", cut.Find("a.stage-card").GetAttribute("href"));
    }

    [Fact]
    public void Next_stage_href_left_unchanged_when_already_relative()
    {
        TransitionReturns("why-jesus");

        var cut = RenderComponent<NodeArticle>(p => p.Add(c => c.Node, Node()));

        Assert.Equal("why-jesus", cut.Find("a.stage-card").GetAttribute("href"));
    }
}
