using System.Net;
using System.Text;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Respondeo.Content.Abstractions;
using Respondeo.Content.Markdown.Services;
using Respondeo.Pages;
using Respondeo.Services;

namespace Respondeo.UnitTests.Pages;

public class HomeTests : TestContext
{
    private const string Manifest = "{\"files\":[\"home.md\",\"deeper.md\",\"branch.md\"]}";

    // Two entry points and one non-entry-point node.
    private const string HomeMd = "---\nid: home\ntitle: Home\nsummary: Start here\nisEntryPoint: true\n---\nWelcome";
    private const string DeeperMd = "---\nid: deeper\ntitle: Deeper\nisEntryPoint: true\n---\nMore";
    private const string BranchMd = "---\nid: branch\ntitle: Branch\nisEntryPoint: false\n---\nA branch";

    public HomeTests()
    {
        var handler = new StubHandler(new Dictionary<string, string>
        {
            ["_content/Respondeo.Content.Markdown/content/manifest.json"] = Manifest,
            ["_content/Respondeo.Content.Markdown/content/home.md"] = HomeMd,
            ["_content/Respondeo.Content.Markdown/content/deeper.md"] = DeeperMd,
            ["_content/Respondeo.Content.Markdown/content/branch.md"] = BranchMd,
        });
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };

        Services.AddSingleton<IContentService>(new ContentService(http, new ContentParser()));
        Services.AddSingleton(Substitute.For<IBreadcrumbTrail>());
    }

    [Fact]
    public void Renders_a_card_for_each_entry_point()
    {
        var cut = RenderComponent<Home>();

        var cards = cut.FindAll("a.card");
        Assert.Equal(2, cards.Count);
    }

    [Fact]
    public void Entry_point_cards_link_to_their_node()
    {
        var cut = RenderComponent<Home>();

        var hrefs = cut.FindAll("a.card").Select(c => c.GetAttribute("href")).ToList();
        Assert.Contains("node/home", hrefs);
        Assert.Contains("node/deeper", hrefs);
    }

    [Fact]
    public void Does_not_render_non_entry_point_nodes()
    {
        var cut = RenderComponent<Home>();

        var hrefs = cut.FindAll("a.card").Select(c => c.GetAttribute("href")).ToList();
        Assert.DoesNotContain("node/branch", hrefs);
    }

    [Fact]
    public void Clears_the_breadcrumb_trail_on_load()
    {
        var trail = Services.GetRequiredService<IBreadcrumbTrail>();

        RenderComponent<Home>();

        trail.Received().ClearAsync();
    }

    [Fact]
    public void Always_renders_the_hero_heading()
    {
        var cut = RenderComponent<Home>();

        Assert.Equal("Where are you, and where are you headed?", cut.Find("h1.hero__title").TextContent);
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
