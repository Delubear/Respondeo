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
    public void Begin_walks_into_the_first_bead()
    {
        _devotions.GetDevotionAsync("divine-mercy-chaplet").Returns(SimpleDevotion());
        SetPrayer("our-father", "Our Father", "<p>Our Father...</p>");
        SetPrayer("hail-mary", "Hail Mary", "<p>Hail Mary...</p>");
        var cut = RenderComponent<DevotionPlayer>(p => p.Add(c => c.Id, "divine-mercy-chaplet"));

        cut.Find("button.devotion__begin").Click();

        Assert.NotEmpty(cut.FindAll(".devotion--praying"));
        Assert.Contains("Our Father", cut.Find(".devotion__prayer-title").TextContent);
        Assert.Contains("Step 1 of 4", cut.Find(".devotion__step-count").TextContent);
    }

    [Fact]
    public void Next_advances_through_the_repeated_beads()
    {
        _devotions.GetDevotionAsync("divine-mercy-chaplet").Returns(SimpleDevotion());
        SetPrayer("our-father", "Our Father", "<p>Our Father...</p>");
        SetPrayer("hail-mary", "Hail Mary", "<p>Hail Mary...</p>");
        var cut = RenderComponent<DevotionPlayer>(p => p.Add(c => c.Id, "divine-mercy-chaplet"));
        cut.Find("button.devotion__begin").Click();

        cut.Find("button.devotion__nav--primary").Click();

        Assert.Contains("Step 2 of 4", cut.Find(".devotion__step-count").TextContent);
        Assert.Contains("Hail Mary", cut.Find(".devotion__prayer-title").TextContent);
        Assert.Contains("1 of 3", cut.Find(".devotion__count").TextContent);
    }

    [Fact]
    public void Shows_a_not_found_message_for_a_missing_devotion()
    {
        _devotions.GetDevotionAsync("nope").Returns((Devotion?)null);

        var cut = RenderComponent<DevotionPlayer>(p => p.Add(c => c.Id, "nope"));

        Assert.Contains("Devotion not found", cut.Markup);
    }
}
