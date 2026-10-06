using Respondeo.Content.Contracts;
using Respondeo.Services;

namespace Respondeo.UnitTests.Services;

public class DevotionBrowseTests
{
    private readonly DevotionBrowse _browse = new();

    private static readonly SortOption<DevotionSummary> SortAsc =
        new("title-asc", "Title (A–Z)", rows => rows.OrderBy(d => d.SortValue, StringComparer.CurrentCultureIgnoreCase));

    private static IReadOnlyList<DevotionSummary> SampleIndex() =>
    [
        new DevotionSummary
        {
            Id = "holy-rosary",
            Title = "The Holy Rosary",
            Summary = "Pray the mysteries bead by bead.",
            Kind = "rosary",
        },
        new DevotionSummary
        {
            Id = "divine-mercy",
            Title = "Divine Mercy Chaplet",
            Summary = "The chaplet of mercy.",
            Kind = "chaplet",
        },
        new DevotionSummary
        {
            Id = "plain",
            Title = "A Simple Devotion",
            Summary = "No particular kind.",
            Kind = "devotion",
        },
    ];

    [Fact]
    public void FormatKind_capitalizes_a_slug()
    {
        Assert.Equal("Rosary", _browse.FormatKind("rosary"));
    }

    [Theory]
    [InlineData("devotion")]
    [InlineData("")]
    [InlineData(" ")]
    public void FormatKind_returns_null_for_the_neutral_bucket(string kind)
    {
        Assert.Null(_browse.FormatKind(kind));
    }

    [Fact]
    public void Kinds_excludes_devotion_and_is_sorted()
    {
        var kinds = _browse.Kinds(SampleIndex());

        Assert.Equal(["chaplet", "rosary"], kinds);
    }

    [Fact]
    public void KindLabels_are_display_formatted()
    {
        var labels = _browse.KindLabels(["chaplet", "rosary"]);

        Assert.Equal(["Chaplet", "Rosary"], labels);
    }

    [Fact]
    public void Filter_without_criteria_returns_all_sorted_by_title()
    {
        var results = _browse.Filter(SampleIndex(), "", Empty(), SortAsc);

        Assert.Equal(["A Simple Devotion", "Divine Mercy Chaplet", "The Holy Rosary"], results.Select(d => d.Title));
    }

    [Fact]
    public void Filter_by_kind_matches_the_display_label()
    {
        var results = _browse.Filter(SampleIndex(), "", Selected("Rosary"), SortAsc);

        Assert.Equal(["holy-rosary"], results.Select(d => d.Id));
    }

    [Fact]
    public void Filter_by_query_matches_title_summary_and_kind()
    {
        Assert.Equal(["divine-mercy"], _browse.Filter(SampleIndex(), "mercy", Empty(), SortAsc).Select(d => d.Id));
        Assert.Equal(["holy-rosary"], _browse.Filter(SampleIndex(), "bead", Empty(), SortAsc).Select(d => d.Id));
        Assert.Equal(["divine-mercy"], _browse.Filter(SampleIndex(), "chaplet", Empty(), SortAsc).Select(d => d.Id));
    }

    [Fact]
    public void Filter_with_no_match_returns_empty()
    {
        var results = _browse.Filter(SampleIndex(), "nonexistent", Empty(), SortAsc);

        Assert.Empty(results);
    }

    private static IReadOnlySet<string> Empty() => new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    private static IReadOnlySet<string> Selected(params string[] values) =>
        new HashSet<string>(values, StringComparer.OrdinalIgnoreCase);
}
