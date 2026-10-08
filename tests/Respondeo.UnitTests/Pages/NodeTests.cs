using System.Net;
using System.Text;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Respondeo.Content.Contracts;
using Respondeo.Content.Infrastructure;
using Respondeo.Content.Shared;
using Respondeo.Content.Inquiry;
using Respondeo.Pages;
using Respondeo.Services;
using Respondeo.UnitTests.TestSupport;

namespace Respondeo.UnitTests.Pages;

public class NodeTests : TestContext
{
    private const string Manifest = "{\"files\":[\"why-god/root.md\",\"why-god/child.md\"]}";

    // root branches to child (branch edges now live in flow, not front-matter).
    private const string RootMd = "---\nid: root\ntitle: Root Question\nsummary: The root\n---\nRoot body";
    private const string ChildMd = "---\nid: child\ntitle: Child Node\n---\nChild body";

    private IBreadcrumbTrail _trail = default!;

    public NodeTests()
    {
        var handler = new StubHandler(new Dictionary<string, string>
        {
            ["_content/Respondeo.Content/inquiry/manifest.json"] = Manifest,
            ["_content/Respondeo.Content/inquiry/why-god/root.md"] = RootMd,
            ["_content/Respondeo.Content/inquiry/why-god/child.md"] = ChildMd,
        });
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };

        _trail = Substitute.For<IBreadcrumbTrail>();
        // Default: the trail contains just the current node (no ancestors).
        _trail.VisitAsync(Arg.Any<string>()).Returns(call => Task.FromResult<IReadOnlyList<string>>(new[] { (string)call[0] }));

        // Branch edges are sourced from IInquiryFlow (flow.json): root -> child.
        var flow = Substitute.For<IInquiryFlow>();
        flow.GetBranchesAsync(Arg.Any<string>()).Returns((IReadOnlyList<BranchLink>)[]);
        flow.GetBranchesAsync("root").Returns((IReadOnlyList<BranchLink>)
        [
            new BranchLink { To = "child", Label = "Go deeper", Prompt = "Explore this path" },
        ]);
        flow.GetTransitionForNodeAsync(Arg.Any<string>(), Arg.Any<string?>()).Returns((StageLink?)null);

        Services.AddSingleton<IContentService>(new InquiryService(http, new InquiryParser(ContentRendering.Renderer)));
        Services.AddSingleton(flow);
        Services.AddSingleton(_trail);
        Services.AddSingleton(Substitute.For<IVisitedNodes>());

        // The Breadcrumb component imports ./js/breadcrumb.js on first render for the scroll/fade behavior.
        var breadcrumbModule = JSInterop.SetupModule("./js/breadcrumb.js");
        breadcrumbModule.SetupVoid("init", _ => true);
        breadcrumbModule.SetupVoid("refresh", _ => true);
        breadcrumbModule.SetupVoid("dispose", _ => true);

        // NodeArticle imports ./js/table-mobile.js during OnAfterRenderAsync to enhance tables.
        var tableModule = JSInterop.SetupModule("./js/table-mobile.js");
        tableModule.SetupVoid("enhance", _ => true);
        tableModule.SetupVoid("dispose", _ => true);
    }

    [Fact]
    public void Renders_node_title_and_body()
    {
        var cut = RenderComponent<Node>(p => p
            .Add(c => c.Id, "root")
            .Add(c => c.Stage, "why-god"));

        Assert.Equal("Root Question", cut.Find("h1.pillar__title").TextContent);
        Assert.Contains("Root body", cut.Find(".content-body").TextContent);
    }

    [Fact]
    public void Renders_branch_cards_with_label_and_prompt()
    {
        var cut = RenderComponent<Node>(p => p
            .Add(c => c.Id, "root")
            .Add(c => c.Stage, "why-god"));

        var card = cut.Find(".branches a.card");
        Assert.Equal("why-god/child", card.GetAttribute("href"));
        Assert.Contains("Go deeper", card.TextContent);
        Assert.Contains("Explore this path", card.TextContent);
    }

    [Fact]
    public void Shows_empty_state_for_unknown_node()
    {
        var cut = RenderComponent<Node>(p => p.Add(c => c.Id, "missing"));

        Assert.Contains("This path hasn't been written yet", cut.Find("h1").TextContent);
        Assert.Empty(cut.FindAll("article.node"));
    }

    [Fact]
    public void Does_not_render_branches_section_when_node_has_no_branches()
    {
        var cut = RenderComponent<Node>(p => p
            .Add(c => c.Id, "child")
            .Add(c => c.Stage, "why-god"));

        Assert.Empty(cut.FindAll("section.branches"));
    }

    [Fact]
    public void Renders_current_node_as_the_final_breadcrumb()
    {
        var cut = RenderComponent<Node>(p => p
            .Add(c => c.Id, "root")
            .Add(c => c.Stage, "why-god"));

        Assert.Equal("Root Question", cut.Find(".breadcrumb__current").TextContent);
    }    [Fact]
    public void Renders_ancestor_crumbs_from_the_trail()
    {
        // Visiting "child" yields a trail of root -> child; root is the ancestor crumb.
        _trail.VisitAsync("child", "why-god").Returns(Task.FromResult<IReadOnlyList<string>>(new[] { "root", "child" }));

        var cut = RenderComponent<Node>(p => p
            .Add(c => c.Id, "child")
            .Add(c => c.Stage, "why-god"));

        var link = cut.Find("a.breadcrumb__link");
        Assert.Equal("why-god/root", link.GetAttribute("href"));
        Assert.Equal("Root Question", link.TextContent);
    }

    [Fact]
    public void Redirects_to_the_canonical_url_when_the_stage_segment_is_wrong()
    {
        // root lives in the why-god stage, so its canonical URL is why-god/root. A wrong stage
        // segment in the URL should trigger a replace-navigation to the canonical route.
        var nav = Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>();

        RenderComponent<Node>(p => p
            .Add(c => c.Id, "root")
            .Add(c => c.Stage, "why-jesus"));

        Assert.Equal(nav.ToAbsoluteUri("why-god/root").ToString(), nav.Uri);
    }

    [Fact]
    public void Does_not_redirect_when_the_stage_segment_matches()
    {
        var nav = Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>();
        nav.NavigateTo("why-god/root");
        var start = nav.Uri;

        // root belongs to why-god; rendering with the matching stage segment is already canonical.
        RenderComponent<Node>(p => p
            .Add(c => c.Id, "root")
            .Add(c => c.Stage, "why-god"));

        Assert.Equal(start, nav.Uri);
    }

    [Fact]
    public void Preserves_the_query_string_when_redirecting_to_the_canonical_url()
    {
        // A deep link such as ?section=... must survive the canonical redirect so shared section
        // links keep working when the URL's stage segment is corrected.
        var nav = Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>();
        nav.NavigateTo("why-jesus/root?section=intro");

        RenderComponent<Node>(p => p
            .Add(c => c.Id, "root")
            .Add(c => c.Stage, "why-jesus"));

        Assert.Equal(nav.ToAbsoluteUri("why-god/root?section=intro").ToString(), nav.Uri);
    }
}
