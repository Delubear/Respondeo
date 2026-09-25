using System.Net;
using System.Text;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Respondeo.Content.Abstractions;
using Respondeo.Content.Markdown.Services;
using Respondeo.UnitTests.TestSupport;
using Respondeo.Pages;

namespace Respondeo.UnitTests.Pages;

public class StageTests : TestContext
{
    // A landing node whose id matches the "why-god" stage slug in SiteNavigation.
    private const string Manifest = "{\"files\":[\"why-god.md\"]}";
    private const string WhyGodMd = "---\nid: why-god\ntitle: Why God?\nsummary: The first stage\n---\nStage body";

    private void RegisterContent(params (string Path, string Body)[] extra)
    {
        var responses = new Dictionary<string, string>
        {
            ["_content/Respondeo.Content.Markdown/content/manifest.json"] = Manifest,
            ["_content/Respondeo.Content.Markdown/content/why-god.md"] = WhyGodMd,
        };
        foreach (var (path, body) in extra)
        {
            responses[path] = body;
        }

        var http = new HttpClient(new StubHandler(responses)) { BaseAddress = new Uri("https://localhost/") };
        Services.AddSingleton<IContentService>(new ContentService(http, new ContentParser()));
    }

    [Fact]
    public void Renders_the_landing_node_for_a_known_stage()
    {
        RegisterContent();

        var cut = RenderComponent<Stage>(p => p.Add(c => c.Slug, "why-god"));

        Assert.Contains("Stage body", cut.Find("article.node").TextContent);
    }

    [Fact]
    public void Slug_match_is_case_insensitive()
    {
        RegisterContent();

        var cut = RenderComponent<Stage>(p => p.Add(c => c.Slug, "WHY-GOD"));

        Assert.Contains("Stage body", cut.Find("article.node").TextContent);
    }

    [Fact]
    public void Shows_coming_soon_placeholder_when_the_landing_node_is_missing()
    {
        // Known stage in SiteNavigation, but no landing node content is served for it.
        var http = new HttpClient(new StubHandler(new Dictionary<string, string>
        {
            ["_content/Respondeo.Content.Markdown/content/manifest.json"] = "{\"files\":[]}",
        }))
        { BaseAddress = new Uri("https://localhost/") };
        Services.AddSingleton<IContentService>(new ContentService(http, new ContentParser()));

        var cut = RenderComponent<Stage>(p => p.Add(c => c.Slug, "coming-home"));

        Assert.Equal("Coming Home", cut.Find("h1.landing__title").TextContent);
        Assert.Contains("coming soon", cut.Find("p.landing__placeholder").TextContent);
    }

    [Fact]
    public void Renders_not_found_for_an_unknown_stage_slug()
    {
        RegisterContent();

        var cut = RenderComponent<Stage>(p => p.Add(c => c.Slug, "not-a-stage"));

        Assert.Empty(cut.FindAll("article.node"));
        Assert.Empty(cut.FindAll("section.landing"));
        Assert.Contains("Not Found", cut.Find("h3").TextContent);
    }
}
