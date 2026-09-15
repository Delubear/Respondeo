using System.Net;
using System.Text;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Respondeo.Pages;
using Respondeo.Services;
using Xunit;

namespace Respondeo.UnitTests.Pages;

public class NodeTests : TestContext
{
    private const string Manifest = "{\"files\":[\"root.md\",\"child.md\"]}";

    // root branches to child.
    private const string RootMd = "---\nid: root\ntitle: Root Question\nsummary: The root\nisEntryPoint: true\nbranches:\n  - to: child\n    label: Go deeper\n    prompt: Explore this path\n---\nRoot body";
    private const string ChildMd = "---\nid: child\ntitle: Child Node\nisEntryPoint: false\n---\nChild body";

    private IBreadcrumbTrail _trail = default!;

    public NodeTests()
    {
        var handler = new StubHandler(new Dictionary<string, string>
        {
            ["_content/Respondeo.Content/content/manifest.json"] = Manifest,
            ["_content/Respondeo.Content/content/root.md"] = RootMd,
            ["_content/Respondeo.Content/content/child.md"] = ChildMd,
        });
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };

        _trail = Substitute.For<IBreadcrumbTrail>();
        // Default: the trail contains just the current node (no ancestors).
        _trail.VisitAsync(Arg.Any<string>()).Returns(call => Task.FromResult<IReadOnlyList<string>>(new[] { (string)call[0] }));

        Services.AddSingleton(new ContentService(http, new ContentParser()));
        Services.AddSingleton(_trail);
    }

    [Fact]
    public void Renders_node_title_and_body()
    {
        var cut = RenderComponent<Node>(p => p.Add(c => c.Id, "root"));

        Assert.Equal("Root Question", cut.Find("h1.node__title").TextContent);
        Assert.Contains("Root body", cut.Find(".node__body").TextContent);
    }

    [Fact]
    public void Renders_branch_cards_with_label_and_prompt()
    {
        var cut = RenderComponent<Node>(p => p.Add(c => c.Id, "root"));

        var card = cut.Find(".branches a.card");
        Assert.Equal("node/child", card.GetAttribute("href"));
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
        var cut = RenderComponent<Node>(p => p.Add(c => c.Id, "child"));

        Assert.Empty(cut.FindAll("section.branches"));
    }

    [Fact]
    public void Renders_current_node_as_the_final_breadcrumb()
    {
        var cut = RenderComponent<Node>(p => p.Add(c => c.Id, "root"));

        Assert.Equal("Root Question", cut.Find(".breadcrumb__current").TextContent);
    }

    [Fact]
    public void Renders_ancestor_crumbs_from_the_trail()
    {
        // Visiting "child" yields a trail of root -> child; root is the ancestor crumb.
        _trail.VisitAsync("child").Returns(Task.FromResult<IReadOnlyList<string>>(new[] { "root", "child" }));

        var cut = RenderComponent<Node>(p => p.Add(c => c.Id, "child"));

        var link = cut.Find("a.breadcrumb__link");
        Assert.Equal("node/root", link.GetAttribute("href"));
        Assert.Equal("Root Question", link.TextContent);
    }

    private sealed class StubHandler(IReadOnlyDictionary<string, string> responses) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri!.AbsolutePath.TrimStart('/');
            if (responses.TryGetValue(path, out var body))
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(body, Encoding.UTF8, "text/plain"),
                });
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }
    }
}
