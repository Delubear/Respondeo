using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Respondeo.Content.Contracts;
using Respondeo.Content.Devotions;
using Respondeo.Content.Miracles;
using Respondeo.Pages;
using Respondeo.Services;
using Respondeo.UnitTests.TestSupport;

namespace Respondeo.UnitTests.Pages;

public class DevotionsTests : TestContext
{
    private const string ManifestJson = """
        { "files": [ "holy-rosary.json", "divine-mercy-chaplet.json" ] }
        """;

    private const string RosaryJson = """
        {
          "id": "holy-rosary",
          "title": "The Holy Rosary",
          "summary": "A meditative prayer on the mysteries.",
          "kind": "rosary",
          "intro": "Take up the beads and begin.",
          "sequence": [ { "kind": "prayer", "title": "Begin", "prayerId": "hail-mary", "repeat": 1 } ]
        }
        """;

    private const string ChapletJson = """
        {
          "id": "divine-mercy-chaplet",
          "title": "Divine Mercy Chaplet",
          "summary": "A prayer of mercy on rosary beads.",
          "kind": "chaplet",
          "intro": "Begin with the Sign of the Cross.",
          "sequence": [ { "kind": "prayer", "title": "Begin", "prayerId": "our-father", "repeat": 1 } ]
        }
        """;

    private const string Root = "_content/Respondeo.Content/discover/devotions";

    public DevotionsTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        var handler = new StubHandler(new Dictionary<string, string>
        {
            [$"{Root}/devotions-manifest.json"] = ManifestJson,
            [$"{Root}/holy-rosary.json"] = RosaryJson,
            [$"{Root}/divine-mercy-chaplet.json"] = ChapletJson,
        });
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };

        Services.AddSingleton<IDevotionService>(new DevotionService(http, ContentRendering.Renderer));
        Services.AddSingleton(Substitute.For<IBreadcrumbTrail>());
        Services.AddSingleton(sp => new BrowseState(sp.GetRequiredService<NavigationManager>()));
        Services.AddScoped<BrowseStateInterop>();
    }

    [Fact]
    public void Renders_each_devotion_as_a_card()
    {
        var cut = RenderComponent<Devotions>();

        var hrefs = cut.FindAll("a.card").Select(c => c.GetAttribute("href")).ToList();
        Assert.Contains("discover/devotions/holy-rosary", hrefs);
        Assert.Contains("discover/devotions/divine-mercy-chaplet", hrefs);
    }

    [Fact]
    public void Search_by_kind_label_filters_to_matching_devotion()
    {
        var cut = RenderComponent<Devotions>();

        cut.Find("#devotion-search").Input("Chaplet");

        cut.WaitForAssertion(() =>
        {
            var hrefs = cut.FindAll("a.card").Select(c => c.GetAttribute("href")).ToList();
            Assert.Contains("discover/devotions/divine-mercy-chaplet", hrefs);
            Assert.DoesNotContain("discover/devotions/holy-rosary", hrefs);
        });
    }
}
