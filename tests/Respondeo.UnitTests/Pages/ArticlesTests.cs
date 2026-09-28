using Bunit;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Respondeo.Content.Discover.Articles;
using Respondeo.Content.Discover.Services;
using Respondeo.Content.Markdown.Services;
using Respondeo.Content.Shared;
using Respondeo.Pages;
using Respondeo.Services;
using Respondeo.UnitTests.TestSupport;

namespace Respondeo.UnitTests.Pages;

public class ArticlesTests : TestContext
{
    private const string CredoManifest = """
        { "files": [ "confession.md" ] }
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

    public ArticlesTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        var handler = new StubHandler(new Dictionary<string, string>
        {
            ["_content/Respondeo.Content.Discover/discover/articles/articles-manifest.json"] = CredoManifest,
            ["_content/Respondeo.Content.Discover/discover/articles/confession.md"] = ConfessionMd,
            ["_content/Respondeo.Content.Markdown/content/manifest.json"] = ContentManifest,
            ["_content/Respondeo.Content.Markdown/content/beta.md"] = BetaMd,
            ["_content/Respondeo.Content.Markdown/content/section.md"] = SectionMd,
        });
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };

        Services.AddSingleton<IArticleService>(new ArticleService(http, ContentRendering.Renderer));
        Services.AddSingleton<IContentService>(new ContentService(http, new ContentParser(ContentRendering.Renderer)));
        Services.AddSingleton(Substitute.For<IBreadcrumbTrail>());
        Services.AddSingleton(sp => new DiscoverBrowseState(sp.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>(), JSInterop.JSRuntime));
    }

    [Fact]
    public void Renders_both_credo_articles_and_inquiry_articles()
    {
        var cut = RenderComponent<Articles>();

        var hrefs = cut.FindAll("a.card").Select(c => c.GetAttribute("href")).ToList();
        Assert.Contains("discover/articles/confession", hrefs);
        Assert.Contains("node/beta", hrefs);
    }

    [Fact]
    public void Excludes_inquiry_section_nodes()
    {
        var cut = RenderComponent<Articles>();

        var hrefs = cut.FindAll("a.card").Select(c => c.GetAttribute("href")).ToList();
        Assert.DoesNotContain("node/section", hrefs);
    }

    [Fact]
    public void Surfaces_the_inquiry_source_as_a_filter()
    {
        var cut = RenderComponent<Articles>();

        var sources = cut.FindAll("button.source-filter__option").Select(o => o.TextContent.Trim()).ToList();
        Assert.Contains("From Inquiry", sources);
    }

    [Fact]
    public void Merged_article_urls_do_not_collide()
    {
        var cut = RenderComponent<Articles>();

        var hrefs = cut.FindAll("a.card").Select(c => c.GetAttribute("href")).ToList();
        Assert.Equal(hrefs.Count, hrefs.Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    [Fact]
    public void Surfaces_tags_as_title_cased_filter_options()
    {
        var cut = RenderComponent<Articles>();

        var tagsHeading = cut.FindAll("h2.credo__sidebar-title").Any(h => h.TextContent.Trim() == "Tags");
        Assert.True(tagsHeading);

        var options = cut.FindAll("label.topic-filter__option").Select(o => o.TextContent.Trim()).ToList();
        Assert.Contains("Existence of God", options);
        Assert.Contains("Confession", options);
    }

    [Fact]
    public void Selecting_a_tag_narrows_the_list_to_matching_articles()
    {
        var cut = RenderComponent<Articles>();

        // The "Existence of God" tag belongs only to the inquiry article (beta), not to confession.
        var checkbox = cut.FindAll("label.topic-filter__option")
            .First(o => o.TextContent.Trim() == "Existence of God")
            .QuerySelector("input[type=checkbox]")!;
        checkbox.Change(true);

        var hrefs = cut.FindAll("a.card").Select(c => c.GetAttribute("href")).ToList();
        Assert.Contains("node/beta", hrefs);
        Assert.DoesNotContain("discover/articles/confession", hrefs);
    }
}
