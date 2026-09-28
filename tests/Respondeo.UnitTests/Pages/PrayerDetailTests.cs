using Bunit;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Respondeo.Content.Discover.Prayers;
using Respondeo.Pages;
using Respondeo.Services;

namespace Respondeo.UnitTests.Pages;

public class PrayerDetailTests : TestContext
{
    private IPrayerService _prayers = default!;

    public PrayerDetailTests()
    {
        _prayers = Substitute.For<IPrayerService>();
        Services.AddSingleton(_prayers);
        Services.AddSingleton(Substitute.For<IBreadcrumbTrail>());
    }

    private static Prayer SamplePrayer(string? latinHtml = null, string? attribution = null) => new()
    {
        Id = "hail-mary",
        Title = "Hail Mary",
        Summary = "The angelic salutation.",
        Html = "<p>Hail Mary, full of grace.</p>",
        LatinHtml = latinHtml,
        Attribution = attribution,
    };

    [Fact]
    public void Renders_the_prayer_title_and_text()
    {
        _prayers.GetPrayerAsync("hail-mary").Returns(SamplePrayer());

        var cut = RenderComponent<PrayerDetail>(p => p.Add(c => c.Id, "hail-mary"));

        Assert.Equal("Hail Mary", cut.Find("h1.pillar__title").TextContent.Trim());
        Assert.Contains("Hail Mary, full of grace.", cut.Find(".prayer__text").TextContent);
    }

    [Fact]
    public void Shows_latin_section_when_latin_html_is_present()
    {
        _prayers.GetPrayerAsync("hail-mary").Returns(SamplePrayer(latinHtml: "<p>Ave Maria.</p>"));

        var cut = RenderComponent<PrayerDetail>(p => p.Add(c => c.Id, "hail-mary"));

        Assert.Contains("In Latin", cut.Find(".prayer__latin-title").TextContent);
        Assert.Contains("Ave Maria.", cut.Markup);
    }

    [Fact]
    public void Omits_latin_section_when_no_latin_html()
    {
        _prayers.GetPrayerAsync("hail-mary").Returns(SamplePrayer());

        var cut = RenderComponent<PrayerDetail>(p => p.Add(c => c.Id, "hail-mary"));

        Assert.Empty(cut.FindAll(".prayer__latin"));
    }

    [Fact]
    public void Shows_attribution_when_present()
    {
        _prayers.GetPrayerAsync("hail-mary").Returns(SamplePrayer(attribution: "Traditional"));

        var cut = RenderComponent<PrayerDetail>(p => p.Add(c => c.Id, "hail-mary"));

        Assert.Equal("Traditional", cut.Find(".prayer__attribution").TextContent.Trim());
    }

    [Fact]
    public void Shows_a_not_found_message_for_a_missing_prayer()
    {
        _prayers.GetPrayerAsync("nope").Returns((Prayer?)null);

        var cut = RenderComponent<PrayerDetail>(p => p.Add(c => c.Id, "nope"));

        Assert.Contains("Prayer not found", cut.Markup);
    }
}
