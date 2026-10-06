using Respondeo.Content.Contracts;
using Respondeo.Services;

namespace Respondeo.UnitTests.Services;

public class ArticleBrowseTests
{
    private readonly ArticleBrowse _browse = new();

    private static readonly SortOption<ArticleRow> SortAsc =
        new("title-asc", "Title (A–Z)", rows => rows.OrderBy(a => a.SortValue, StringComparer.CurrentCultureIgnoreCase));

    private static IReadOnlyList<ArticleSummary> SampleIndex() =>
    [
        new ArticleSummary
        {
            Id = "confession",
            Title = "Why We Confess",
            Summary = "The sacrament of reconciliation.",
            Topic = "sacraments",
            Tags = ["confession", "mercy"],
        },
        new ArticleSummary
        {
            Id = "eucharist",
            Title = "The Real Presence",
            Summary = "Christ truly present.",
            Topic = "sacraments",
            Tags = ["eucharist"],
        },
        new ArticleSummary
        {
            Id = "plain",
            Title = "A General Reflection",
            Summary = "No particular topic.",
            Topic = "general",
            Tags = [],
        },
    ];

    [Fact]
    public void FormatPill_capitalizes_a_slug()
    {
        Assert.Equal("Sacraments", _browse.FormatPill("sacraments"));
    }

    [Theory]
    [InlineData("general")]
    [InlineData("")]
    [InlineData(" ")]
    public void FormatPill_returns_null_for_the_neutral_bucket(string topic)
    {
        Assert.Null(_browse.FormatPill(topic));
    }

    [Fact]
    public void BuildRows_projects_pill_and_href_sorted_by_title()
    {
        var rows = _browse.BuildRows(SampleIndex());

        Assert.Equal(["A General Reflection", "The Real Presence", "Why We Confess"], rows.Select(r => r.Title));
        var confession = rows.Single(r => r.Id == "confession");
        Assert.Equal("Sacraments", confession.Pill);
        Assert.Equal("discover/articles/confession", confession.Href);
        Assert.Null(rows.Single(r => r.Id == "plain").Pill);
    }

    [Fact]
    public void Tags_are_distinct_and_sorted()
    {
        var tags = _browse.Tags(_browse.BuildRows(SampleIndex()));

        Assert.Equal(["confession", "eucharist", "mercy"], tags);
    }

    [Fact]
    public void Filter_without_criteria_returns_all_sorted_by_title()
    {
        var rows = _browse.BuildRows(SampleIndex());

        var results = _browse.Filter(rows, "", Empty(), SortAsc);

        Assert.Equal(["A General Reflection", "The Real Presence", "Why We Confess"], results.Select(r => r.Title));
    }

    [Fact]
    public void Filter_by_tag_matches_any_selected_tag()
    {
        var rows = _browse.BuildRows(SampleIndex());

        var results = _browse.Filter(rows, "", Selected("eucharist"), SortAsc);

        Assert.Equal(["eucharist"], results.Select(r => r.Id));
    }

    [Fact]
    public void Filter_by_query_matches_title_summary_tags_and_pill()
    {
        var rows = _browse.BuildRows(SampleIndex());

        Assert.Equal(["confession"], _browse.Filter(rows, "reconciliation", Empty(), SortAsc).Select(r => r.Id));
        Assert.Equal(["confession"], _browse.Filter(rows, "mercy", Empty(), SortAsc).Select(r => r.Id));
        Assert.Equal(["eucharist", "confession"], _browse.Filter(rows, "sacraments", Empty(), SortAsc).Select(r => r.Id));
    }

    [Fact]
    public void Filter_with_no_match_returns_empty()
    {
        var rows = _browse.BuildRows(SampleIndex());

        var results = _browse.Filter(rows, "nonexistent", Empty(), SortAsc);

        Assert.Empty(results);
    }

    private static IReadOnlySet<string> Empty() => new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    private static IReadOnlySet<string> Selected(params string[] values) =>
        new HashSet<string>(values, StringComparer.OrdinalIgnoreCase);
}
