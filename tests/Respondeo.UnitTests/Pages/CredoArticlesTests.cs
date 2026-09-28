using System.Net;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Respondeo.Content.Credo;
using Respondeo.Content.Credo.Services;
using Respondeo.Content.Markdown.Services;
using Respondeo.Content.Shared;
using Respondeo.Pages;
using Respondeo.Services;
using Respondeo.UnitTests.TestSupport;

namespace Respondeo.UnitTests.Pages;

public class CredoArticlesTests : TestContext
{
    private const string CredoManifest = """
        {
          "prayers": [],
          "devotions": [],
          "articles": [ "confession.md" ]
        }
        """;

    private const string ConfessionMd = """
        ---
        id: confession
        title: "Confession"
        summary: The sacrament of reconciliation.
        topic: sacraments
        tags:
          - confession
        ---

        A lead-in paragraph.
        """;

    // beta.md references section.md as a section, so section.md must be excluded from the list.
    private const string ContentManifest = "{\"files\":[\"beta.md\",\"section.md\"]}";
    private const string BetaMd = "---\nid: beta\ntitle: Beta\nsummary: An inquiry article.\ntags:\n  - existence of god\nsections:\n  - section\n---\nBody";
    private const string SectionMd = "---\nid: section\ntitle: Section\n---\nBody";

    public CredoArticlesTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        var handler = new StubHandler(new Dictionary<string, string>
        {
            ["_content/Respondeo.Content.Credo/credo/credo-manifest.json"] = CredoManifest,
            ["_content/Respondeo.Content.Credo/credo/articles/confession.md"] = ConfessionMd,
            ["_content/Respondeo.Content.Markdown/content/manifest.json"] = ContentManifest,
            ["_content/Respondeo.Content.Markdown/content/beta.md"] = BetaMd,
            ["_content/Respondeo.Content.Markdown/content/section.md"] = SectionMd,
        });
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };

        Services.AddSingleton<ICredoService>(new CredoService(http, ContentRendering.Renderer));
        Services.AddSingleton<IContentService>(new ContentService(http, new ContentParser(ContentRendering.Renderer)));
        Services.AddSingleton(Substitute.For<IBreadcrumbTrail>());
        Services.AddSingleton(sp => new CredoBrowseState(sp.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>(), JSInterop.JSRuntime));
    }

    [Fact]
    public void Renders_both_credo_articles_and_inquiry_articles()
    {
        var cut = RenderComponent<CredoArticles>();

        var hrefs = cut.FindAll("a.credo-row__link").Select(c => c.GetAttribute("href")).ToList();
        Assert.Contains("credo/articles/confession", hrefs);
        Assert.Contains("node/beta", hrefs);
    }

    [Fact]
    public void Excludes_inquiry_section_nodes()
    {
        var cut = RenderComponent<CredoArticles>();

        var hrefs = cut.FindAll("a.credo-row__link").Select(c => c.GetAttribute("href")).ToList();
        Assert.DoesNotContain("node/section", hrefs);
    }

    [Fact]
    public void Surfaces_the_inquiry_topic_as_a_filter()
    {
        var cut = RenderComponent<CredoArticles>();

        var topics = cut.FindAll("label.topic-filter__option").Select(o => o.TextContent.Trim()).ToList();
        Assert.Contains("From Inquiry", topics);
    }

    [Fact]
    public void Merged_article_urls_do_not_collide()
    {
        var cut = RenderComponent<CredoArticles>();

        var hrefs = cut.FindAll("a.credo-row__link").Select(c => c.GetAttribute("href")).ToList();
        Assert.Equal(hrefs.Count, hrefs.Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }
}
