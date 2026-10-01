using Bunit;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Respondeo.Content.Contracts;
using Respondeo.Pages;
using Respondeo.Services;

namespace Respondeo.UnitTests.Pages;

public class MiracleDetailTests : TestContext
{
    private IMiracleService _miracles = default!;

    public MiracleDetailTests()
    {
        // Breadcrumb imports ./js/breadcrumb.js on first render; tolerate JS calls.
        JSInterop.Mode = JSRuntimeMode.Loose;
        _miracles = Substitute.For<IMiracleService>();
        Services.AddSingleton(_miracles);
        Services.AddSingleton(Substitute.For<IBreadcrumbTrail>());
    }

    private static MiracleFacetCatalog Facets() => new()
    {
        Categories = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["eucharistic"] = "Eucharistic" },
        Approvals = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["historical"] = "Historical / traditional" },
        Regions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["europe"] = "Europe" },
    };

    private static MiracleRecord SampleMiracle() => new()
    {
        Id = "lanciano",
        Title = "The Eucharistic Miracle of Lanciano",
        Summary = "An 8th-century host that became flesh and blood.",
        Types = ["eucharistic"],
        Approval = "historical",
        Region = "europe",
        Country = "Italy",
        Year = 750,
        BodyHtml = "<p>A monk doubted.</p>",
        Sources =
        [
            new MiracleSource { Label = "Vatican Exhibition", Url = "https://example.org/exhibit" },
        ],
    };

    [Fact]
    public void Renders_the_miracle_title_and_body()
    {
        _miracles.GetFacetsAsync().Returns(Facets());
        _miracles.GetByIdAsync("lanciano").Returns(SampleMiracle());

        var cut = RenderComponent<MiracleDetail>(p => p.Add(c => c.Id, "lanciano"));

        Assert.Equal("The Eucharistic Miracle of Lanciano", cut.Find("h1.pillar__title").TextContent.Trim());
        Assert.Contains("A monk doubted.", cut.Find(".content-body").TextContent);
    }

    [Fact]
    public void Uses_facet_labels_for_category_and_approval()
    {
        _miracles.GetFacetsAsync().Returns(Facets());
        _miracles.GetByIdAsync("lanciano").Returns(SampleMiracle());

        var cut = RenderComponent<MiracleDetail>(p => p.Add(c => c.Id, "lanciano"));

        Assert.Contains("Eucharistic", cut.Find(".pillar__eyebrow").TextContent);
        Assert.Contains("Historical / traditional", cut.Find(".miracle__facts").TextContent);
    }

    [Fact]
    public void Shows_a_not_found_message_for_a_missing_miracle()
    {
        _miracles.GetFacetsAsync().Returns(Facets());
        _miracles.GetByIdAsync("nope").Returns((MiracleRecord?)null);

        var cut = RenderComponent<MiracleDetail>(p => p.Add(c => c.Id, "nope"));

        Assert.Contains("Miracle not found", cut.Markup);
    }
}
