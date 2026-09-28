using System.Net;
using Respondeo.Content.Credo.Services;
using Respondeo.UnitTests.TestSupport;

namespace Respondeo.UnitTests.Services;

public class CredoServiceTests
{
    private const string ManifestJson = """
        {
          "prayers": [ "hail-mary.md", "ave-maria.md" ],
          "devotions": [ "holy-rosary.json" ],
          "articles": [ "confession.md" ]
        }
        """;

    private const string HailMaryMd = """
        ---
        id: hail-mary
        title: "Hail Mary"
        summary: The angelic salutation.
        category: Marian
        language: en
        translationKey: hail-mary
        tags:
          - marian
        attribution: Traditional (public domain).
        ---

        Hail Mary, full of grace, the Lord is with thee.
        """;

    private const string HailMaryLatinMd = """
        ---
        id: ave-maria
        title: "Ave Maria"
        summary: The Latin of the Hail Mary.
        category: Marian
        language: la
        translationKey: hail-mary
        tags:
          - latin
        ---

        Ave Maria, gratia plena, Dominus tecum.
        """;

    private const string RosaryJson = """
        {
          "id": "holy-rosary",
          "title": "The Holy Rosary",
          "summary": "A meditative prayer on the mysteries.",
          "kind": "Rosary",
          "intro": "Take up the beads and begin.",
          "mysterySets": [
            {
              "id": "joyful",
              "name": "The Joyful Mysteries",
              "when": "Mondays and Saturdays",
              "mysteries": [
                { "title": "The Annunciation", "reflection": "The angel greets Mary." }
              ]
            }
          ],
          "sequence": [
            { "kind": "prayer", "title": "Begin", "prayerId": "hail-mary", "repeat": 3 },
            {
              "kind": "mysteries",
              "perMystery": [
                { "kind": "prayer", "prayerId": "hail-mary", "repeat": 10 }
              ]
            }
          ]
        }
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

    private const string ManifestPath = "_content/Respondeo.Content.Credo/credo/credo-manifest.json";
    private const string HailMaryPath = "_content/Respondeo.Content.Credo/credo/prayers/hail-mary.md";
    private const string HailMaryLatinPath = "_content/Respondeo.Content.Credo/credo/prayers/ave-maria.md";
    private const string RosaryPath = "_content/Respondeo.Content.Credo/credo/devotions/holy-rosary.json";
    private const string ConfessionPath = "_content/Respondeo.Content.Credo/credo/articles/confession.md";

    private static CredoService CreateService()
    {
        var handler = new StubHandler(new Dictionary<string, string>
        {
            [ManifestPath] = ManifestJson,
            [HailMaryPath] = HailMaryMd,
            [HailMaryLatinPath] = HailMaryLatinMd,
            [RosaryPath] = RosaryJson,
            [ConfessionPath] = ConfessionMd,
        });

        var http = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        return new CredoService(http, ContentRendering.Renderer);
    }

    [Fact]
    public async Task GetIndex_returns_prayers_devotions_and_articles()
    {
        var service = CreateService();

        var index = await service.GetIndexAsync();

        // Only the primary-language (English) prayer is listed; the Latin file is not a separate row.
        var prayer = Assert.Single(index.Prayers);
        Assert.Equal("hail-mary", prayer.Id);
        Assert.Equal("marian", prayer.Category);

        var devotion = Assert.Single(index.Devotions);
        Assert.Equal("holy-rosary", devotion.Id);
        Assert.Equal("rosary", devotion.Kind);

        var article = Assert.Single(index.Articles);
        Assert.Equal("confession", article.Id);
        Assert.Equal("sacraments", article.Topic);
    }

    [Fact]
    public async Task GetPrayer_returns_vernacular_and_latin_and_attribution()
    {
        var service = CreateService();

        var prayer = await service.GetPrayerAsync("hail-mary");

        Assert.NotNull(prayer);
        Assert.Contains("full of grace", prayer!.Html);
        Assert.Equal("en", prayer.Language);
        Assert.Equal("hail-mary", prayer.TranslationKey);
        Assert.NotNull(prayer.LatinHtml);
        Assert.Contains("Ave Maria", prayer.LatinHtml!);
        Assert.Equal("Traditional (public domain).", prayer.Attribution);
    }

    [Fact]
    public async Task GetPrayer_returns_the_latin_translation_by_its_own_id()
    {
        var service = CreateService();

        var latin = await service.GetPrayerAsync("ave-maria");

        Assert.NotNull(latin);
        Assert.Equal("la", latin!.Language);
        Assert.Equal("hail-mary", latin.TranslationKey);
        Assert.Contains("Ave Maria", latin.Html);
    }

    [Fact]
    public async Task GetDevotion_returns_sets_and_data_driven_sequence()
    {
        var service = CreateService();

        var devotion = await service.GetDevotionAsync("holy-rosary");

        Assert.NotNull(devotion);
        var set = Assert.Single(devotion!.MysterySets);
        Assert.Equal("joyful", set.Id);
        var mystery = Assert.Single(set.Mysteries);
        Assert.Equal("The Annunciation", mystery.Title);

        Assert.Equal(2, devotion.Sequence.Count);
        Assert.Equal("prayer", devotion.Sequence[0].Kind);
        Assert.Equal(3, devotion.Sequence[0].Repeat);
        Assert.Equal("mysteries", devotion.Sequence[1].Kind);
        var perMystery = Assert.Single(devotion.Sequence[1].PerMystery);
        Assert.Equal(10, perMystery.Repeat);
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

    [Fact]
    public async Task GetPrayer_returns_null_for_unknown_id()
    {
        var service = CreateService();

        var prayer = await service.GetPrayerAsync("does-not-exist");

        Assert.Null(prayer);
    }
}
