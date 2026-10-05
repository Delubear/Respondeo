using NSubstitute;
using Respondeo.Content.Contracts;
using Respondeo.Content.Devotions;
using Respondeo.UnitTests.TestSupport;

namespace Respondeo.UnitTests.Services;

public class DevotionServiceTests
{
    private const string ManifestJson = """
        { "files": [ "holy-rosary.json" ] }
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

    private const string ManifestPath = "_content/Respondeo.Content/discover/devotions/devotions-manifest.json";
    private const string RosaryPath = "_content/Respondeo.Content/discover/devotions/holy-rosary.json";

    private static DevotionService CreateService(IPrayerService? prayers = null)
    {
        var handler = new StubHandler(new Dictionary<string, string>
        {
            [ManifestPath] = ManifestJson,
            [RosaryPath] = RosaryJson,
        });

        var http = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        return new DevotionService(http, ContentRendering.Renderer, prayers ?? Substitute.For<IPrayerService>());
    }

    [Fact]
    public async Task GetIndex_returns_devotions()
    {
        var service = CreateService();

        var index = await service.GetIndexAsync();

        var devotion = Assert.Single(index);
        Assert.Equal("holy-rosary", devotion.Id);
        Assert.Equal("rosary", devotion.Kind);
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
    public async Task GetPrayersFor_fetches_each_distinct_referenced_prayer_once()
    {
        var prayers = Substitute.For<IPrayerService>();
        prayers.GetPrayerAsync("hail-mary")
            .Returns(new Prayer { Id = "hail-mary", Title = "Hail Mary", Html = "<p>Hail Mary...</p>" });

        var service = CreateService(prayers);
        var devotion = await service.GetDevotionAsync("holy-rosary");

        var map = await service.GetPrayersForAsync(devotion!);

        // hail-mary appears in both the opening step and the per-mystery step; it must be fetched once.
        var prayer = Assert.Single(map);
        Assert.Equal("hail-mary", prayer.Key);
        await prayers.Received(1).GetPrayerAsync("hail-mary");
    }

    [Fact]
    public async Task GetPrayersFor_omits_ids_that_resolve_to_no_prayer()
    {
        var prayers = Substitute.For<IPrayerService>();
        prayers.GetPrayerAsync(Arg.Any<string>()).Returns((Prayer?)null);

        var service = CreateService(prayers);
        var devotion = await service.GetDevotionAsync("holy-rosary");

        var map = await service.GetPrayersForAsync(devotion!);

        Assert.Empty(map);
    }
}
