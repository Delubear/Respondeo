using Respondeo.Components;
using Respondeo.Content.Contracts;
using Respondeo.Services;

namespace Respondeo.UnitTests.Services;

public class PrayerBrowseTests
{
    private readonly PrayerBrowse _browse = new();

    private static readonly SortOption<PrayerSummary> SortAsc =
        new("title-asc", "Title (A–Z)", rows => rows.OrderBy(p => p.SortValue, StringComparer.CurrentCultureIgnoreCase));

    private static IReadOnlyList<PrayerSummary> SampleIndex() =>
    [
        new PrayerSummary
        {
            Id = "hail-mary",
            Title = "Hail Mary",
            LatinTitle = "Ave Maria",
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
        new PrayerSummary
        {
            Id = "unsorted",
            Title = "A Nameless Prayer",
            Summary = "No particular category.",
            Category = "other",
            Tags = [],
        },
    ];

    [Fact]
    public void FormatCategory_capitalizes_a_slug()
    {
        Assert.Equal("Marian", _browse.FormatCategory("marian"));
    }

    [Theory]
    [InlineData("other")]
    [InlineData("")]
    [InlineData(" ")]
    public void FormatCategory_returns_null_for_the_neutral_bucket(string category)
    {
        Assert.Null(_browse.FormatCategory(category));
    }

    [Fact]
    public void Categories_excludes_other_and_is_sorted()
    {
        var categories = _browse.Categories(SampleIndex());

        Assert.Equal(["basic", "marian"], categories);
    }

    [Fact]
    public void CategoryLabels_are_display_formatted()
    {
        var labels = _browse.CategoryLabels(["basic", "marian"]);

        Assert.Equal(["Basic", "Marian"], labels);
    }

    [Fact]
    public void Tags_are_distinct_and_sorted()
    {
        var tags = _browse.Tags(SampleIndex());

        Assert.Equal(["basic", "marian", "rosary"], tags);
    }

    [Fact]
    public void Filter_without_criteria_returns_all_sorted_by_title()
    {
        var results = _browse.Filter(SampleIndex(), "", Empty(), Empty(), SortAsc);

        Assert.Equal(["A Nameless Prayer", "Hail Mary", "Our Father"], results.Select(p => p.Title));
    }

    [Fact]
    public void Filter_by_category_matches_the_display_label()
    {
        var results = _browse.Filter(SampleIndex(), "", Selected("Marian"), Empty(), SortAsc);

        Assert.Equal(["hail-mary"], results.Select(p => p.Id));
    }

    [Fact]
    public void Filter_by_tag_matches_any_selected_tag()
    {
        var results = _browse.Filter(SampleIndex(), "", Empty(), Selected("rosary"), SortAsc);

        Assert.Equal(["hail-mary"], results.Select(p => p.Id));
    }

    [Fact]
    public void Filter_by_query_matches_title_summary_tags_and_category()
    {
        Assert.Equal(["hail-mary"], _browse.Filter(SampleIndex(), "angelic", Empty(), Empty(), SortAsc).Select(p => p.Id));
        Assert.Equal(["hail-mary"], _browse.Filter(SampleIndex(), "rosary", Empty(), Empty(), SortAsc).Select(p => p.Id));
        Assert.Equal(["our-father"], _browse.Filter(SampleIndex(), "basic", Empty(), Empty(), SortAsc).Select(p => p.Id));
    }

    [Fact]
    public void Filter_by_query_matches_the_latin_title()
    {
        Assert.Equal(["hail-mary"], _browse.Filter(SampleIndex(), "Ave Maria", Empty(), Empty(), SortAsc).Select(p => p.Id));
    }

    [Fact]
    public void Filter_with_no_match_returns_empty()
    {
        var results = _browse.Filter(SampleIndex(), "nonexistent", Empty(), Empty(), SortAsc);

        Assert.Empty(results);
    }

    private static IReadOnlySet<string> Empty() => new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    private static IReadOnlySet<string> Selected(params string[] values) =>
        new HashSet<string>(values, StringComparer.OrdinalIgnoreCase);
}
