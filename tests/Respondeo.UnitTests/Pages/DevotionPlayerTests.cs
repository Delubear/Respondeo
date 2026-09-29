using Bunit;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Respondeo.Content.Discover.Devotions;
using Respondeo.Content.Discover.Prayers;
using Respondeo.Pages;
using Respondeo.Services;

namespace Respondeo.UnitTests.Pages;

public class DevotionPlayerTests : TestContext
{
    private IDevotionService _devotions = default!;
    private IPrayerService _prayers = default!;

    public DevotionPlayerTests()
    {
        _devotions = Substitute.For<IDevotionService>();
        _prayers = Substitute.For<IPrayerService>();
        Services.AddSingleton(_devotions);
        Services.AddSingleton(_prayers);
        Services.AddSingleton(Substitute.For<IBreadcrumbTrail>());

        _prayers.GetPrayerAsync(Arg.Any<string>()).Returns((Prayer?)null);
    }

    private void SetPrayer(string id, string title, string html)
    {
        _prayers.GetPrayerAsync(id).Returns(new Prayer { Id = id, Title = title, Html = html });
    }

    private static Devotion SimpleDevotion() => new()
    {
        Id = "divine-mercy-chaplet",
        Title = "The Divine Mercy Chaplet",
        Summary = "A chaplet of mercy.",
        IntroHtml = "<p>Begin in the name of the Father.</p>",
        Sequence =
        [
            new DevotionStep { Kind = "prayer", Title = "Opening", PrayerId = "our-father", Repeat = 1 },
            new DevotionStep { Kind = "prayer", Title = "Hail Marys", PrayerId = "hail-mary", Repeat = 3 },
        ],
    };

    [Fact]
    public void Shows_the_intro_and_begin_button_before_starting()
    {
        _devotions.GetDevotionAsync("divine-mercy-chaplet").Returns(SimpleDevotion());
        SetPrayer("our-father", "Our Father", "<p>Our Father...</p>");
        SetPrayer("hail-mary", "Hail Mary", "<p>Hail Mary...</p>");

        var cut = RenderComponent<DevotionPlayer>(p => p.Add(c => c.Id, "divine-mercy-chaplet"));

        Assert.Contains("Begin in the name of the Father.", cut.Markup);
        Assert.NotEmpty(cut.FindAll("button.devotion__begin"));
        Assert.Empty(cut.FindAll(".devotion--praying"));
    }

    [Fact]
    public void Begin_renders_the_whole_devotion_as_a_scroll()
    {
        _devotions.GetDevotionAsync("divine-mercy-chaplet").Returns(SimpleDevotion());
        SetPrayer("our-father", "Our Father", "<p>Our Father...</p>");
        SetPrayer("hail-mary", "Hail Mary", "<p>Hail Mary...</p>");
        var cut = RenderComponent<DevotionPlayer>(p => p.Add(c => c.Id, "divine-mercy-chaplet"));

        cut.Find("button.devotion__begin").Click();

        Assert.NotEmpty(cut.FindAll(".devotion--praying"));
        // Every step is on the page at once: 1 Our Father + 3 Hail Marys = 4 prayer titles.
        Assert.Equal(4, cut.FindAll(".devotion__prayer-title").Count);
        Assert.Empty(cut.FindAll(".devotion__nav"));
    }

    [Fact]
    public void Repeated_prayers_are_listed_once_per_repetition()
    {
        _devotions.GetDevotionAsync("divine-mercy-chaplet").Returns(SimpleDevotion());
        SetPrayer("our-father", "Our Father", "<p>Our Father...</p>");
        SetPrayer("hail-mary", "Hail Mary", "<p>Hail Mary...</p>");
        var cut = RenderComponent<DevotionPlayer>(p => p.Add(c => c.Id, "divine-mercy-chaplet"));

        cut.Find("button.devotion__begin").Click();

        var counts = cut.FindAll(".devotion__count").ToList();
        Assert.Equal(3, counts.Count);
        Assert.Contains("1 of 3", counts[0].TextContent);
        Assert.Contains("3 of 3", counts[2].TextContent);
    }

    [Fact]
    public void Shows_a_not_found_message_for_a_missing_devotion()
    {
        _devotions.GetDevotionAsync("nope").Returns((Devotion?)null);

        var cut = RenderComponent<DevotionPlayer>(p => p.Add(c => c.Id, "nope"));

        Assert.Contains("Devotion not found", cut.Markup);
    }
}
