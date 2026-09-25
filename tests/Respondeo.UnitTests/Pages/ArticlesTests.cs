using System.Net;
using System.Text;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Respondeo.Content.Abstractions;
using Respondeo.UnitTests.TestSupport;
using Respondeo.Content.Markdown.Services;
using Respondeo.Pages;
using Respondeo.Services;

namespace Respondeo.UnitTests.Pages;

public class ArticlesTests : TestContext
{
    private const string Manifest = "{\"files\":[\"alpha.md\",\"beta.md\",\"gamma.md\",\"section.md\"]}";

    // alpha references section.md as a section, so section.md must be excluded from the list.
    private const string AlphaMd = "---\nid: alpha\ntitle: Alpha\ntopics:\n  - Existence of God\n  - St. Thomas Aquinas\nsections:\n  - section\n---\nBody";
    private const string BetaMd = "---\nid: beta\ntitle: Beta\ntopics:\n  - Existence of God\n---\nBody";
    private const string GammaMd = "---\nid: gamma\ntitle: Gamma\ntopics:\n  - St. Thomas Aquinas\n---\nBody";
    private const string SectionMd = "---\nid: section\ntitle: Section\n---\nBody";

    public ArticlesTests()
    {
        var handler = new StubHandler(new Dictionary<string, string>
        {
            ["_content/Respondeo.Content.Markdown/content/manifest.json"] = Manifest,
            ["_content/Respondeo.Content.Markdown/content/alpha.md"] = AlphaMd,
            ["_content/Respondeo.Content.Markdown/content/beta.md"] = BetaMd,
            ["_content/Respondeo.Content.Markdown/content/gamma.md"] = GammaMd,
            ["_content/Respondeo.Content.Markdown/content/section.md"] = SectionMd,
        });
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };

        Services.AddSingleton<IContentService>(new ContentService(http, new ContentParser()));
        Services.AddSingleton(Substitute.For<IBreadcrumbTrail>());
    }

    [Fact]
    public void Renders_a_card_for_each_standalone_article()
    {
        var cut = RenderComponent<Articles>();

        var hrefs = cut.FindAll("a.card").Select(c => c.GetAttribute("href")).ToList();
        Assert.Contains("node/alpha", hrefs);
        Assert.Contains("node/beta", hrefs);
        Assert.Contains("node/gamma", hrefs);
    }

    [Fact]
    public void Does_not_render_section_nodes_as_articles()
    {
        var cut = RenderComponent<Articles>();

        var hrefs = cut.FindAll("a.card").Select(c => c.GetAttribute("href")).ToList();
        Assert.DoesNotContain("node/section", hrefs);
    }

    [Fact]
    public void Lists_distinct_topics_in_the_sidebar()
    {
        var cut = RenderComponent<Articles>();

        var topics = cut.FindAll("button.topic-filter__item").Select(b => b.TextContent.Trim()).ToList();
        Assert.Equal(["Existence of God", "St. Thomas Aquinas"], topics);
    }

    [Fact]
    public void Selecting_a_topic_filters_to_matching_articles()
    {
        var cut = RenderComponent<Articles>();

        var topicButton = cut.FindAll("button.topic-filter__item").First(b => b.TextContent.Trim() == "St. Thomas Aquinas");
        topicButton.Click();

        var hrefs = cut.FindAll("a.card").Select(c => c.GetAttribute("href")).ToList();
        Assert.Contains("node/alpha", hrefs);
        Assert.Contains("node/gamma", hrefs);
        Assert.DoesNotContain("node/beta", hrefs);
    }

    [Fact]
    public void Selecting_multiple_topics_requires_all_of_them()
    {
        var cut = RenderComponent<Articles>();

        foreach (var label in new[] { "Existence of God", "St. Thomas Aquinas" })
        {
            cut.FindAll("button.topic-filter__item").First(b => b.TextContent.Trim() == label).Click();
        }

        var hrefs = cut.FindAll("a.card").Select(c => c.GetAttribute("href")).ToList();
        Assert.Contains("node/alpha", hrefs);
        Assert.DoesNotContain("node/beta", hrefs);
        Assert.DoesNotContain("node/gamma", hrefs);
    }

    [Fact]
    public void Clear_filters_restores_all_articles()
    {
        var cut = RenderComponent<Articles>();

        cut.FindAll("button.topic-filter__item").First(b => b.TextContent.Trim() == "St. Thomas Aquinas").Click();
        cut.Find("button.topic-filter__clear").Click();

        var hrefs = cut.FindAll("a.card").Select(c => c.GetAttribute("href")).ToList();
        Assert.Contains("node/alpha", hrefs);
        Assert.Contains("node/beta", hrefs);
        Assert.Contains("node/gamma", hrefs);
    }

    [Fact]
    public void Marks_the_articles_origin_on_load()
    {
        var trail = Services.GetRequiredService<IBreadcrumbTrail>();

        RenderComponent<Articles>();

        trail.Received().SetArticlesOriginAsync(true);
    }
}
