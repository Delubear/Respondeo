using System.Net;
using System.Text;
using Respondeo.Content.Miracles;
using Respondeo.Content.Miracles.Services;

namespace Respondeo.UnitTests.Services;

public class MiracleServiceTests
{
    private const string ManifestJson = """
        { "files": [ "eucharistic/lanciano.md", "marian/guadalupe.md" ] }
        """;

    private const string LancianoMd = """
        ---
        id: lanciano
        title: "The Eucharistic Miracle of Lanciano"
        summary: An 8th-century host that became flesh and blood.
        type: eucharistic
        approval: historical
        region: europe
        country: Italy
        year: 750
        tags:
          - bleeding host
        sources:
          - label: "Vatican Exhibition"
            url: "http://example.org"
        ---

        <b>NOTE: unvetted.</b>

        ## What happened

        A monk doubted, and the host changed.

        ## The findings

        Human cardiac tissue and type AB blood.
        """;

    private const string GuadalupeMd = """
        ---
        id: guadalupe
        title: "Our Lady of Guadalupe"
        summary: The 1531 apparitions and the tilma.
        type: marian
        approval: approved
        region: latin-america
        country: Mexico
        year: 1531
        ---

        ## What happened

        The Virgin appeared to St. Juan Diego.
        """;

    private const string ManifestPath = "_content/Respondeo.Content.Miracles/miracles/miracles-manifest.json";
    private const string LancianoPath = "_content/Respondeo.Content.Miracles/miracles/eucharistic/lanciano.md";
    private const string GuadalupePath = "_content/Respondeo.Content.Miracles/miracles/marian/guadalupe.md";

    private static MiracleService CreateService()
    {
        var handler = new StubHandler(new Dictionary<string, string>
        {
            [ManifestPath] = ManifestJson,
            [LancianoPath] = LancianoMd,
            [GuadalupePath] = GuadalupeMd,
        });

        var http = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        return new MiracleService(http);
    }

    [Fact]
    public async Task GetIndex_returns_every_miracle_with_typed_facets()
    {
        var service = CreateService();

        var index = await service.GetIndexAsync();

        Assert.Equal(2, index.Entries.Count);
        var lanciano = Assert.Single(index.Entries, e => e.Id == "lanciano");
        Assert.Equal(MiracleType.Eucharistic, lanciano.Type);
        Assert.Equal(ApprovalStatus.Historical, lanciano.Approval);
        Assert.Equal(MiracleRegion.Europe, lanciano.Region);
        Assert.Equal("Italy", lanciano.Country);
        Assert.Equal(750, lanciano.Year);
        Assert.Contains("bleeding host", lanciano.Tags);
    }

    [Fact]
    public async Task GetById_returns_full_record_with_sections_and_sources()
    {
        var service = CreateService();

        var record = await service.GetByIdAsync("lanciano");

        Assert.NotNull(record);
        Assert.Equal("The Eucharistic Miracle of Lanciano", record!.Title);

        // Lead-in (disclaimer) becomes an untitled section, then the two ## sections.
        Assert.Equal(3, record.Sections.Count);
        Assert.Equal(string.Empty, record.Sections[0].Heading);
        Assert.Equal("What happened", record.Sections[1].Heading);
        Assert.Contains("the host changed", record.Sections[1].Html);
        Assert.Equal("The findings", record.Sections[2].Heading);

        var source = Assert.Single(record.Sources);
        Assert.Equal("Vatican Exhibition", source.Label);
        Assert.Equal("http://example.org", source.Url);
    }

    [Fact]
    public async Task GetById_returns_null_for_unknown_id()
    {
        var service = CreateService();

        var record = await service.GetByIdAsync("does-not-exist");

        Assert.Null(record);
    }

    private sealed class StubHandler(IReadOnlyDictionary<string, string> responses) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri!.AbsolutePath.TrimStart('/');

            if (responses.TryGetValue(path, out var body))
            {
                var contentType = path.EndsWith(".json", StringComparison.OrdinalIgnoreCase) ? "application/json" : "text/markdown";
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(body, Encoding.UTF8, contentType),
                });
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }
    }
}
