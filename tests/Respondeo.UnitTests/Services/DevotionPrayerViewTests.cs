using Respondeo.Services;

namespace Respondeo.UnitTests.Services;

public class DevotionPrayerViewTests
{
    private static DevotionBead PrayerBead() => new()
    {
        PrayerTitle = "Hail Mary",
        PrayerHtml = "<p>Hail Mary...</p>",
        PrayerLatinTitle = "Ave Maria",
        PrayerLatinHtml = "<p>Ave Maria...</p>",
        ExplanationHtml = "<p>The angelic salutation.</p>",
    };

    [Fact]
    public void ForPrayer_uses_latin_title_and_text_when_requested_and_available()
    {
        var view = DevotionPrayerView.ForPrayer(PrayerBead(), showLatin: true);

        Assert.Equal("Ave Maria", view.Title);
        Assert.Equal("<p>Ave Maria...</p>", view.PrayerHtml);
        Assert.Equal("<p>The angelic salutation.</p>", view.ExplanationHtml);
    }

    [Fact]
    public void ForPrayer_uses_vernacular_when_latin_not_requested()
    {
        var view = DevotionPrayerView.ForPrayer(PrayerBead(), showLatin: false);

        Assert.Equal("Hail Mary", view.Title);
        Assert.Equal("<p>Hail Mary...</p>", view.PrayerHtml);
    }

    [Fact]
    public void ForPrayer_falls_back_to_vernacular_when_latin_text_missing()
    {
        var bead = new DevotionBead
        {
            PrayerTitle = "Our Father",
            PrayerHtml = "<p>Our Father...</p>",
            PrayerLatinTitle = "Pater Noster",
            PrayerLatinHtml = null,
        };

        var view = DevotionPrayerView.ForPrayer(bead, showLatin: true);

        Assert.Equal("Our Father", view.Title);
        Assert.Equal("<p>Our Father...</p>", view.PrayerHtml);
    }

    [Fact]
    public void ForPrayer_falls_back_to_vernacular_title_when_latin_title_missing()
    {
        var bead = new DevotionBead
        {
            PrayerTitle = "Glory Be",
            PrayerHtml = "<p>Glory be...</p>",
            PrayerLatinTitle = null,
            PrayerLatinHtml = "<p>Gloria Patri...</p>",
        };

        var view = DevotionPrayerView.ForPrayer(bead, showLatin: true);

        Assert.Equal("Glory Be", view.Title);
        Assert.Equal("<p>Gloria Patri...</p>", view.PrayerHtml);
    }

    [Fact]
    public void ForInfo_maps_heading_and_explanation_with_no_prayer_text()
    {
        var bead = new DevotionBead
        {
            Heading = "The Luminous Mysteries",
            ExplanationHtml = "<p>Mysteries of light.</p>",
            IsSection = true,
        };

        var view = DevotionPrayerView.ForInfo(bead);

        Assert.Equal("The Luminous Mysteries", view.Title);
        Assert.Null(view.PrayerHtml);
        Assert.Equal("<p>Mysteries of light.</p>", view.ExplanationHtml);
    }
}
