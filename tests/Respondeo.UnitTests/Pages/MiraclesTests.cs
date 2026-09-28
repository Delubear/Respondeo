using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Respondeo.Content.Discover;
using Respondeo.Content.Discover.Services;
using Respondeo.Pages;
using Respondeo.Services;
using Respondeo.UnitTests.TestSupport;

namespace Respondeo.UnitTests.Pages;

public class MiraclesTests : TestContext
{
    private const string ManifestJson = """
        { "files": [ "lanciano-eucharistic-miracle.md", "our-lady-of-guadalupe.md" ] }
        """;

    private const string FacetsJson = """
        {
          "categories": { "eucharistic": "Eucharistic", "marian": "Marian apparition", "image": "Miraculous image / icon" },
          "approvals": { "approved": "Church-approved", "historical": "Historical / traditional" },
          "regions": { "europe": "Europe", "latin-america": "Latin America" }
        }
        """;

    private const string LancianoMd = """
        ---
        id: lanciano-eucharistic-miracle
        title: "The Eucharistic Miracle of Lanciano"
        summary: An 8th-century host that became flesh and blood.
        types: [eucharistic]
        approval: historical
        region: europe
        country: Italy
        year: 750
        ---

        ## What happened

        A monk doubted, and the host changed.
        """;

    private const string GuadalupeMd = """
        ---
        id: our-lady-of-guadalupe
        title: "Our Lady of Guadalupe"
        summary: The 1531 apparitions and the tilma.
        types: [marian, image]
        approval: approved
        region: latin-america
        country: Mexico
        year: 1531
        ---

        ## What happened

        The Virgin appeared to St. Juan Diego.
        """;

    private const string Root = "_content/Respondeo.Content.Discover/discover/miracles";

    public MiraclesTests()
    {
        var handler = new StubHandler(new Dictionary<string, string>
        {
            [$"{Root}/miracles-manifest.json"] = ManifestJson,
            [$"{Root}/facets.json"] = FacetsJson,
            [$"{Root}/lanciano-eucharistic-miracle.md"] = LancianoMd,
            [$"{Root}/our-lady-of-guadalupe.md"] = GuadalupeMd,
        });
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };

        Services.AddSingleton<IMiracleService>(new MiracleService(http, ContentRendering.Renderer));
        Services.AddSingleton(Substitute.For<IBreadcrumbTrail>());
        Services.AddSingleton(sp => new MiracleBrowseState(sp.GetRequiredService<NavigationManager>()));
    }

    [Fact]
    public void Renders_each_miracle_as_a_card()
    {
        var cut = RenderComponent<Miracles>();

        var hrefs = cut.FindAll("a.card").Select(c => c.GetAttribute("href")).ToList();
        Assert.Contains("discover/miracles/lanciano-eucharistic-miracle", hrefs);
        Assert.Contains("discover/miracles/our-lady-of-guadalupe", hrefs);
    }

    [Fact]
    public void Shows_each_kind_as_its_own_card_pill()
    {
        var cut = RenderComponent<Miracles>();

        var pills = cut.FindAll("span.card__pill").Select(p => p.TextContent.Trim()).ToList();
        Assert.Contains("Eucharistic", pills);
        Assert.Contains("Marian apparition", pills);
        Assert.Contains("Miraculous image / icon", pills);
    }

    [Fact]
    public void Summary_carries_judgment_century_and_country()
    {
        var cut = RenderComponent<Miracles>();

        var summaries = cut.FindAll("p.card__summary").Select(s => s.TextContent).ToList();

        var lanciano = summaries.Single(s => s.Contains("8th-century host"));
        Assert.Contains("Historical / traditional", lanciano);
        Assert.Contains("Italy", lanciano);
        // The kind is shown in the pill, not repeated in the summary.
        Assert.DoesNotContain("Eucharistic", lanciano);
    }
}
