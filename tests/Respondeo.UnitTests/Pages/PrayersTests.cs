using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Respondeo.Content.Contracts;
using Respondeo.Pages;
using Respondeo.Services;

namespace Respondeo.UnitTests.Pages;

public class PrayersTests : TestContext
{
    private readonly IPrayerService _prayers;

    public PrayersTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        _prayers = Substitute.For<IPrayerService>();
        _prayers.GetIndexAsync().Returns(SampleIndex());

        Services.AddSingleton(_prayers);
        Services.AddSingleton(Substitute.For<IBreadcrumbTrail>());
        Services.AddSingleton(sp => new BrowseState(sp.GetRequiredService<NavigationManager>()));
        Services.AddScoped<BrowseStateInterop>();
        Services.AddScoped<PrayerBrowse>();
        Services.AddScoped<DialogInterop>();
    }

    private static IReadOnlyList<PrayerSummary> SampleIndex() =>
    [
        new PrayerSummary
        {
            Id = "hail-mary",
            Title = "Hail Mary",
            Summary = "The angelic salutation.",
            Category = "marian",
            Tags = ["rosary", "marian"],
        },
        new PrayerSummary
        {
            Id = "our-father",
            Title = "Our Father",
            Summary = "The prayer the Lord taught.",
            Category = "basic",
            Tags = ["basic"],
        },
    ];

    [Fact]
    public void Renders_a_card_linking_to_each_prayer()
    {
        var cut = RenderComponent<Prayers>();

        var hrefs = cut.FindAll("a.card").Select(c => c.GetAttribute("href")).ToList();
        Assert.Contains("discover/prayers/hail-mary", hrefs);
        Assert.Contains("discover/prayers/our-father", hrefs);
    }

    [Fact]
    public void Surfaces_categories_and_tags_as_filter_options()
    {
        var cut = RenderComponent<Prayers>();

        var sidebarTitles = cut.FindAll("h2.filter-rail__title").Select(h => h.TextContent.Trim()).ToList();
        Assert.Contains("Category", sidebarTitles);
        Assert.Contains("Tags", sidebarTitles);
    }

    [Fact]
    public void Search_narrows_the_list_to_matching_prayers()
    {
        var cut = RenderComponent<Prayers>();

        cut.Find("input#prayer-search").Input("father");

        cut.WaitForAssertion(() =>
        {
            var hrefs = cut.FindAll("a.card").Select(c => c.GetAttribute("href")).ToList();
            Assert.Contains("discover/prayers/our-father", hrefs);
            Assert.DoesNotContain("discover/prayers/hail-mary", hrefs);
        });
    }

    [Fact]
    public void Search_by_category_label_filters_to_matching_prayers()
    {
        var cut = RenderComponent<Prayers>();

        cut.Find("input#prayer-search").Input("Marian");

        cut.WaitForAssertion(() =>
        {
            var hrefs = cut.FindAll("a.card").Select(c => c.GetAttribute("href")).ToList();
            Assert.Contains("discover/prayers/hail-mary", hrefs);
            Assert.DoesNotContain("discover/prayers/our-father", hrefs);
        });
    }

    [Fact]
    public void Shows_an_empty_message_when_nothing_matches()
    {
        var cut = RenderComponent<Prayers>();

        cut.Find("input#prayer-search").Input("zzz-no-match");

        cut.WaitForAssertion(() =>
        {
            Assert.Empty(cut.FindAll("a.card"));
            Assert.Contains("No prayers match your search.", cut.Find(".empty-state").TextContent);
        });
    }

    [Fact]
    public void Sorting_by_title_descending_reverses_the_order()
    {
        var cut = RenderComponent<Prayers>();

        // Default is Title (A–Z): Hail Mary, then Our Father.
        var ascending = cut.FindAll("a.card").Select(c => c.GetAttribute("href")).ToList();
        Assert.Equal(["discover/prayers/hail-mary", "discover/prayers/our-father"], ascending);

        cut.Find("select").Change("title-desc");

        var descending = cut.FindAll("a.card").Select(c => c.GetAttribute("href")).ToList();
        Assert.Equal(["discover/prayers/our-father", "discover/prayers/hail-mary"], descending);
    }
}
