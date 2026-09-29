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
        JSInterop.Mode = JSRuntimeMode.Loose;
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
    public void Begin_lists_every_repetition_as_its_own_row()
    {
        _devotions.GetDevotionAsync("divine-mercy-chaplet").Returns(SimpleDevotion());
        SetPrayer("our-father", "Our Father", "<p>Our Father...</p>");
        SetPrayer("hail-mary", "Hail Mary", "<p>Hail Mary...</p>");
        var cut = RenderComponent<DevotionPlayer>(p => p.Add(c => c.Id, "divine-mercy-chaplet"));

        cut.Find("button.devotion__begin").Click();

        // Each repetition gets its own row: one Our Father + three Hail Marys = four rows.
        var rows = cut.FindAll(".devotion__prayer-row").ToList();
        Assert.Equal(4, rows.Count);
        Assert.Contains("Our Father", rows[0].TextContent);
        Assert.Contains("Hail Mary", rows[1].TextContent);
        Assert.Contains("Hail Mary", rows[3].TextContent);
        // Prayer text is not rendered inline; it only appears once opened.
        Assert.DoesNotContain("Our Father...", cut.Markup);
    }

    [Fact]
    public void Steps_can_only_be_marked_complete_in_order()
    {
        _devotions.GetDevotionAsync("divine-mercy-chaplet").Returns(SimpleDevotion());
        SetPrayer("our-father", "Our Father", "<p>Our Father...</p>");
        SetPrayer("hail-mary", "Hail Mary", "<p>Hail Mary...</p>");
        var cut = RenderComponent<DevotionPlayer>(p => p.Add(c => c.Id, "divine-mercy-chaplet"));
        cut.Find("button.devotion__begin").Click();

        var mains = cut.FindAll(".devotion__prayer-main").ToList();
        // Only the first step is enabled at the start.
        Assert.False(mains[0].HasAttribute("disabled"));
        Assert.True(mains[1].HasAttribute("disabled"));

        mains[0].Click();

        // After completing the first, the second becomes enabled and the first is marked done.
        mains = cut.FindAll(".devotion__prayer-main").ToList();
        Assert.False(mains[1].HasAttribute("disabled"));
        Assert.NotEmpty(cut.FindAll(".devotion__prayer-row.is-done"));
    }

    [Fact]
    public void Tapping_the_book_opens_the_prayer_text_in_a_dialog()
    {
        _devotions.GetDevotionAsync("divine-mercy-chaplet").Returns(SimpleDevotion());
        SetPrayer("our-father", "Our Father", "<p>Our Father...</p>");
        SetPrayer("hail-mary", "Hail Mary", "<p>Hail Mary...</p>");
        var cut = RenderComponent<DevotionPlayer>(p => p.Add(c => c.Id, "divine-mercy-chaplet"));
        cut.Find("button.devotion__begin").Click();

        cut.FindAll(".devotion__prayer-book").ToList()[0].Click();

        Assert.Contains("Our Father", cut.Find(".devotion-dialog__title").TextContent);
        Assert.Contains("Our Father...", cut.Find(".devotion-dialog__body").TextContent);
    }

    [Fact]
    public void Shows_a_not_found_message_for_a_missing_devotion()
    {
        _devotions.GetDevotionAsync("nope").Returns((Devotion?)null);

        var cut = RenderComponent<DevotionPlayer>(p => p.Add(c => c.Id, "nope"));

        Assert.Contains("Devotion not found", cut.Markup);
    }
}
