using Respondeo.Content.Saints;
using Respondeo.UnitTests.TestSupport;

namespace Respondeo.UnitTests.Services;

/// <summary>
/// Unit tests for <see cref="SaintService"/>, exercising static-content loading of the saints catalog
/// and facets via <see cref="StubHandler"/>: index building, by-id lookup, facet mapping with graceful
/// fallback when facets.json is missing, and one-time caching.
/// </summary>
public class SaintServiceTests
{
    private const string ManifestJson = """
        { "files": [ "francis-of-assisi.md", "therese-of-lisieux.md" ] }
        """;

    private const string FrancisMd = """
        ---
        id: francis-of-assisi
        title: "St. Francis of Assisi"
        summary: The Poor Man of Assisi.
        era: medieval
        region: europe
        patronages:
          - Animals
          - Ecology
        statesOfLife: [religious, founder]
        canonizations: [canonized]
        dates: "1181–1226"
        feastDay: "October 4"
        tags:
          - poverty
        sources:
          - label: "Catholic Encyclopedia"
            url: "http://example.org/francis"
        ---

        ## Life

        He rebuilt the Church.
        """;

    private const string ThereseMd = """
        ---
        id: therese-of-lisieux
        title: "St. Thérèse of Lisieux"
        summary: The Little Flower.
        era: modern
        region: europe
        statesOfLife: [religious]
        canonizations: [canonized, doctor-of-the-church]
        dates: "1873–1897"
        ---

        ## Life

        The little way.
        """;

    private const string FacetsJson = """
        {
          "eras": {
            "medieval": "Medieval",
            "modern": "Modern"
          },
          "regions": { "europe": "Europe" },
          "statesOfLife": { "religious": "Religious", "founder": "Founder" },
          "canonizations": { "canonized": "Canonized", "doctor-of-the-church": "Doctor of the Church" }
        }
        """;

    private const string Root = "_content/Respondeo.Content/discover/saints";
    private const string ManifestPath = Root + "/saints-manifest.json";
    private const string FacetsPath = Root + "/facets.json";
    private const string FrancisPath = Root + "/francis-of-assisi.md";
    private const string TheresePath = Root + "/therese-of-lisieux.md";

    private static SaintService CreateService(bool includeFacets = true)
    {
        var responses = new Dictionary<string, string>
        {
            [ManifestPath] = ManifestJson,
            [FrancisPath] = FrancisMd,
            [TheresePath] = ThereseMd,
        };

        if (includeFacets)
        {
            responses[FacetsPath] = FacetsJson;
        }

        var handler = new StubHandler(responses);
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        return new SaintService(http, ContentRendering.Renderer);
    }

    [Fact]
    public async Task GetIndex_returns_every_saint_with_typed_facets()
    {
        var service = CreateService();

        var index = await service.GetIndexAsync();

        Assert.Equal(2, index.Entries.Count);
        var francis = Assert.Single(index.Entries, e => e.Id == "francis-of-assisi");
        Assert.Equal("St. Francis of Assisi", francis.Title);
        Assert.Equal("medieval", francis.Era);
        Assert.Equal("europe", francis.Region);
        Assert.Equal(["religious", "founder"], francis.StatesOfLife);
        Assert.Equal(["canonized"], francis.Canonizations);
        Assert.Equal("1181–1226", francis.Dates);
        Assert.Contains("Animals", francis.Patronages);
        Assert.Contains("poverty", francis.Tags);
    }

    [Fact]
    public async Task GetById_returns_full_record_with_rendered_body_and_sources()
    {
        var service = CreateService();

        var record = await service.GetByIdAsync("francis-of-assisi");

        Assert.NotNull(record);
        Assert.Equal("St. Francis of Assisi", record!.Title);
        Assert.Equal("October 4", record.FeastDay);
        Assert.Contains("rebuilt the Church", record.BodyHtml);
        var source = Assert.Single(record.Sources);
        Assert.Equal("Catholic Encyclopedia", source.Label);
        Assert.Equal("http://example.org/francis", source.Url);
    }

    [Fact]
    public async Task GetById_returns_null_for_unknown_id()
    {
        var service = CreateService();

        Assert.Null(await service.GetByIdAsync("does-not-exist"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetById_returns_null_for_blank_id(string? id)
    {
        var service = CreateService();

        Assert.Null(await service.GetByIdAsync(id!));
    }

    [Fact]
    public async Task GetFacets_maps_slugs_to_labels()
    {
        var service = CreateService();

        var facets = await service.GetFacetsAsync();

        Assert.Equal("Medieval", facets.Era("medieval"));
        Assert.Equal("Europe", facets.Region("europe"));
        Assert.Equal("Religious", facets.StateOfLife("religious"));
        Assert.Equal("Doctor of the Church", facets.Canonization("doctor-of-the-church"));
    }

    [Fact]
    public async Task GetFacets_falls_back_to_empty_when_file_is_missing()
    {
        var service = CreateService(includeFacets: false);

        var facets = await service.GetFacetsAsync();

        // Missing facets must not break browsing; unknown slugs humanize instead.
        Assert.Equal("Medieval", facets.Era("medieval"));
    }
}
