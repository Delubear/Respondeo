using Respondeo.Content.Discover.Articles;
using Respondeo.UnitTests.TestSupport;

namespace Respondeo.UnitTests.Services;

public class ArticleServiceTests
{
    private const string ManifestJson = """
        { "files": [ "confession.md" ] }
        """;

    private const string ConfessionMd = """
        ---
        id: confession
        title: "Confession"
        summary: The sacrament of reconciliation.
        topic: Sacraments
        tags:
          - confession
        sources:
          - label: "Catechism"
            url: "http://example.org"
        ---

        A lead-in paragraph.

        ## Why confess

        Because sin wounds our friendship with God.
        """;

    private const string ManifestPath = "_content/Respondeo.Content.Discover/discover/articles/articles-manifest.json";
    private const string ConfessionPath = "_content/Respondeo.Content.Discover/discover/articles/confession.md";

    private static ArticleService CreateService()
    {
        var handler = new StubHandler(new Dictionary<string, string>
        {
            [ManifestPath] = ManifestJson,
            [ConfessionPath] = ConfessionMd,
        });

        var http = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        return new ArticleService(http, ContentRendering.Renderer);
    }

    [Fact]
    public async Task GetIndex_returns_articles()
    {
        var service = CreateService();

        var index = await service.GetIndexAsync();

        var article = Assert.Single(index);
        Assert.Equal("confession", article.Id);
        Assert.Equal("sacraments", article.Topic);
    }

    [Fact]
    public async Task GetArticle_returns_sections_and_sources()
    {
        var service = CreateService();

        var article = await service.GetArticleAsync("confession");

        Assert.NotNull(article);
        Assert.Equal(2, article!.Sections.Count);
        Assert.Equal(string.Empty, article.Sections[0].Heading);
        Assert.Equal("Why confess", article.Sections[1].Heading);

        var source = Assert.Single(article.Sources);
        Assert.Equal("Catechism", source.Label);
        Assert.Equal("http://example.org", source.Url);
    }
}
