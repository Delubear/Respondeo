using System.Net;
using Respondeo.Content.Prayers;
using Respondeo.UnitTests.TestSupport;

namespace Respondeo.UnitTests.Services;

public class PrayerServiceTests
{
    private const string ManifestJson = """
        { "files": [ "hail-mary.md", "ave-maria.md", "dominican-versicle.md" ] }
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

    private const string UnlistedMd = """
        ---
        id: dominican-versicle
        title: "O God, Come to My Assistance"
        summary: A contextual versicle.
        category: Marian
        language: en
        translationKey: dominican-versicle
        unlisted: true
        tags:
          - marian
        ---

        V. O God, come to my assistance.
        """;

    private const string ManifestPath = "_content/Respondeo.Content/discover/prayers/prayers-manifest.json";
    private const string HailMaryPath = "_content/Respondeo.Content/discover/prayers/hail-mary.md";
    private const string HailMaryLatinPath = "_content/Respondeo.Content/discover/prayers/ave-maria.md";
    private const string UnlistedPath = "_content/Respondeo.Content/discover/prayers/dominican-versicle.md";

    private static PrayerService CreateService()
    {
        var handler = new StubHandler(new Dictionary<string, string>
        {
            [ManifestPath] = ManifestJson,
            [HailMaryPath] = HailMaryMd,
            [HailMaryLatinPath] = HailMaryLatinMd,
            [UnlistedPath] = UnlistedMd,
        });

        var http = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        return new PrayerService(http, ContentRendering.Renderer);
    }

    [Fact]
    public async Task GetIndex_lists_only_primary_language_prayers()
    {
        var service = CreateService();

        var index = await service.GetIndexAsync();

        // Only the primary-language (English) prayer is listed; the Latin file is not a separate row
        // and the unlisted versicle is excluded from the browse index.
        var prayer = Assert.Single(index);
        Assert.Equal("hail-mary", prayer.Id);
        Assert.Equal("marian", prayer.Category);
    }

    [Fact]
    public async Task GetPrayer_returns_unlisted_prayer_by_id()
    {
        var service = CreateService();

        var prayer = await service.GetPrayerAsync("dominican-versicle");

        Assert.NotNull(prayer);
        Assert.Contains("come to my assistance", prayer!.Html);
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
    public async Task GetPrayer_returns_null_for_unknown_id()
    {
        var service = CreateService();

        var prayer = await service.GetPrayerAsync("does-not-exist");

        Assert.Null(prayer);
    }
}
