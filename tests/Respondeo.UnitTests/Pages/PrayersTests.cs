using System.Net;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Respondeo.Content.Credo;
using Respondeo.Content.Credo.Services;
using Respondeo.Pages;
using Respondeo.Services;
using Respondeo.UnitTests.TestSupport;

namespace Respondeo.UnitTests.Pages;

public class PrayersTests : TestContext
{
    private const string CredoManifest = """
        {
          "prayers": [ "hail-mary.md", "our-father.md" ],
          "devotions": [],
          "articles": []
        }
        """;

    private const string HailMaryMd = """
        ---
        id: hail-mary
        title: "The Hail Mary"
        summary: The angelic salutation.
        category: marian
        tags:
          - marian
          - rosary
        ---

        Hail Mary, full of grace.
        """;

    private const string OurFatherMd = """
        ---
        id: our-father
        title: "The Our Father"
        summary: The Lord's own prayer.
        category: basic
        tags:
          - basic
        ---

        Our Father, who art in heaven.
        """;

    public PrayersTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        var handler = new StubHandler(new Dictionary<string, string>
        {
            ["_content/Respondeo.Content.Credo/credo/credo-manifest.json"] = CredoManifest,
            ["_content/Respondeo.Content.Credo/credo/prayers/hail-mary.md"] = HailMaryMd,
            ["_content/Respondeo.Content.Credo/credo/prayers/our-father.md"] = OurFatherMd,
        });
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };

        Services.AddSingleton<ICredoService>(new CredoService(http, ContentRendering.Renderer));
        Services.AddSingleton(Substitute.For<IBreadcrumbTrail>());
        Services.AddSingleton(sp => new DiscoverBrowseState(sp.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>(), JSInterop.JSRuntime));
    }

    [Fact]
    public void Surfaces_tags_as_title_cased_filter_options()
    {
        var cut = RenderComponent<Prayers>();

        var tagsHeading = cut.FindAll("h2.credo__sidebar-title").Any(h => h.TextContent.Trim() == "Tags");
        Assert.True(tagsHeading);

        var options = cut.FindAll("label.topic-filter__option").Select(o => o.TextContent.Trim()).ToList();
        Assert.Contains("Marian", options);
        Assert.Contains("Rosary", options);
        Assert.Contains("Basic", options);
    }

    [Fact]
    public void Selecting_a_tag_narrows_the_list_to_matching_prayers()
    {
        var cut = RenderComponent<Prayers>();

        // The "Rosary" tag belongs only to the Hail Mary, not to the Our Father.
        var checkbox = cut.FindAll("label.topic-filter__option")
            .First(o => o.TextContent.Trim() == "Rosary")
            .QuerySelector("input[type=checkbox]")!;
        checkbox.Change(true);

        var hrefs = cut.FindAll("a.card").Select(c => c.GetAttribute("href")).ToList();
        Assert.Contains("discover/prayers/hail-mary", hrefs);
        Assert.DoesNotContain("discover/prayers/our-father", hrefs);
    }
}
