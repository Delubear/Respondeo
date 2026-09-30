using Bunit;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Respondeo.Content.Articles;
using Respondeo.Content.Services;
using Respondeo.Content.Inquiry.Services;
using Respondeo.Content.Shared;
using Respondeo.Pages;
using Respondeo.Services;
using Respondeo.UnitTests.TestSupport;

namespace Respondeo.UnitTests.Pages;

public class ArticlesTests : TestContext
{
    private const string DiscoverManifest = """
        { "files": [ "confession.md", "eucharist.md" ] }
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

    private const string EucharistMd = """
        ---
        id: eucharist
        title: "Eucharist"
        summary: The source and summit of the Christian life.
        topic: sacraments
        tags:
          - eucharist
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
            ["_content/Respondeo.Content/discover/articles/articles-manifest.json"] = DiscoverManifest,
            ["_content/Respondeo.Content/discover/articles/confession.md"] = ConfessionMd,
            ["_content/Respondeo.Content/discover/articles/eucharist.md"] = EucharistMd,
            ["_content/Respondeo.Content/inquiry/manifest.json"] = ContentManifest,
            ["_content/Respondeo.Content/inquiry/beta.md"] = BetaMd,
            ["_content/Respondeo.Content/inquiry/section.md"] = SectionMd,
        });
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };

        Services.AddSingleton<IArticleService>(new ArticleService(http, ContentRendering.Renderer));
        Services.AddSingleton<IContentService>(new ContentService(http, new ContentParser(ContentRendering.Renderer)));
        Services.AddSingleton(Substitute.For<IBreadcrumbTrail>());
        Services.AddSingleton(sp => new DiscoverBrowseState(sp.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>(), JSInterop.JSRuntime));
    }

    [Fact]
    public void Renders_discover_articles()
    {
        var cut = RenderComponent<Articles>();

        var hrefs = cut.FindAll("a.card").Select(c => c.GetAttribute("href")).ToList();
        Assert.Contains("discover/articles/confession", hrefs);
    }

    [Fact]
    public void Does_not_render_inquiry_nodes()
    {
        var cut = RenderComponent<Articles>();

        var hrefs = cut.FindAll("a.card").Select(c => c.GetAttribute("href")).ToList();
        Assert.DoesNotContain("node/beta", hrefs);
        Assert.DoesNotContain("node/section", hrefs);
    }

    [Fact]
    public void Does_not_render_a_source_filter()
    {
        var cut = RenderComponent<Articles>();

        Assert.Empty(cut.FindAll("button.source-filter__option"));
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

        var tagsHeading = cut.FindAll("h2.discover__sidebar-title").Any(h => h.TextContent.Trim() == "Tags");
        Assert.True(tagsHeading);

        var options = cut.FindAll("label.topic-filter__option").Select(o => o.TextContent.Trim()).ToList();
        Assert.Contains("Eucharist", options);
        Assert.Contains("Confession", options);
    }

    [Fact]
    public void Selecting_a_tag_narrows_the_list_to_matching_articles()
    {
        var cut = RenderComponent<Articles>();

        // The "Eucharist" tag belongs only to the eucharist article, not to confession.
        var checkbox = cut.FindAll("label.topic-filter__option")
            .First(o => o.TextContent.Trim() == "Eucharist")
            .QuerySelector("input[type=checkbox]")!;
        checkbox.Change(true);

        var hrefs = cut.FindAll("a.card").Select(c => c.GetAttribute("href")).ToList();
        Assert.Contains("discover/articles/eucharist", hrefs);
        Assert.DoesNotContain("discover/articles/confession", hrefs);
    }

    [Fact]
    public void Searching_a_topic_pill_label_matches_the_article()
    {
        var cut = RenderComponent<Articles>();

        // "sacraments" renders as the "Sacraments" pill on the confession card.
        cut.Find("#article-search").Input("Sacraments");

        cut.WaitForAssertion(() =>
        {
            var hrefs = cut.FindAll("a.card").Select(c => c.GetAttribute("href")).ToList();
            Assert.Contains("discover/articles/confession", hrefs);
            Assert.Contains("discover/articles/eucharist", hrefs);
        });
    }
}
