using Respondeo.Content.Contracts;
using Respondeo.Services;

namespace Respondeo.UnitTests.Services;

public class MiracleBrowseTests
{
    private readonly MiracleBrowse _browse = new();

    private static readonly MiracleFacetCatalog Facets = new()
    {
        Categories = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["eucharistic"] = "Eucharistic",
            ["marian"] = "Marian apparition",
            ["image"] = "Miraculous image / icon",
        },
        Approvals = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["approved"] = "Church-approved",
            ["historical"] = "Historical / traditional",
        },
        Regions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["europe"] = "Europe",
            ["latin-america"] = "Latin America",
        },
    };

    private static readonly SortOption<MiracleIndexEntry> SortTitleAsc =
        new("title-asc", "Title (A–Z)", rows => rows.OrderBy(m => m.SortValue, StringComparer.CurrentCultureIgnoreCase));

    private static readonly SortOption<MiracleIndexEntry> SortYearAsc =
        new("year-asc", "Year (oldest first)", rows => rows.OrderBy(m => m.Year ?? int.MaxValue).ThenBy(m => m.SortValue, StringComparer.CurrentCultureIgnoreCase));

    private static IReadOnlyList<MiracleIndexEntry> SampleIndex() =>
    [
        new MiracleIndexEntry
        {
            Id = "lanciano",
            Title = "The Eucharistic Miracle of Lanciano",
            Summary = "An 8th-century host that became flesh and blood.",
            Types = ["eucharistic"],
            Approval = "historical",
            Region = "europe",
            Country = "Italy",
            Year = 750,
            Tags = ["host"],
        },
        new MiracleIndexEntry
        {
            Id = "guadalupe",
            Title = "Our Lady of Guadalupe",
            Summary = "The 1531 apparitions and the tilma.",
            Types = ["marian", "image"],
            Approval = "approved",
            Region = "latin-america",
            Country = "Mexico",
            Year = 1531,
            Tags = ["apparition"],
        },
    ];

    [Fact]
    public void Types_are_distinct_and_ordered_by_label()
    {
        var types = _browse.Types(SampleIndex(), Facets);

        Assert.Equal(["eucharistic", "marian", "image"], types);
    }

    [Fact]
    public void Approvals_are_distinct_and_ordered_by_label()
    {
        var approvals = _browse.Approvals(SampleIndex(), Facets);

        Assert.Equal(["approved", "historical"], approvals);
    }

    [Fact]
    public void Regions_are_distinct_and_ordered_by_label()
    {
        var regions = _browse.Regions(SampleIndex(), Facets);

        Assert.Equal(["europe", "latin-america"], regions);
    }

    [Fact]
    public void Filter_without_criteria_returns_all_sorted_by_title()
    {
        var results = _browse.Filter(SampleIndex(), Facets, "", Empty(), Empty(), Empty(), SortTitleAsc);

        Assert.Equal(["Our Lady of Guadalupe", "The Eucharistic Miracle of Lanciano"], results.Select(m => m.Title));
    }

    [Fact]
    public void Filter_sorts_by_year_when_requested()
    {
        var results = _browse.Filter(SampleIndex(), Facets, "", Empty(), Empty(), Empty(), SortYearAsc);

        Assert.Equal(["lanciano", "guadalupe"], results.Select(m => m.Id));
    }

    [Fact]
    public void Filter_by_type_matches_any_selected_type()
    {
        var results = _browse.Filter(SampleIndex(), Facets, "", Selected("image"), Empty(), Empty(), SortTitleAsc);

        Assert.Equal(["guadalupe"], results.Select(m => m.Id));
    }

    [Fact]
    public void Filter_intersects_across_facet_groups()
    {
        var results = _browse.Filter(SampleIndex(), Facets, "", Selected("marian"), Selected("historical"), Empty(), SortTitleAsc);

        Assert.Empty(results);
    }

    [Fact]
    public void Filter_by_approval_and_region()
    {
        var results = _browse.Filter(SampleIndex(), Facets, "", Empty(), Selected("approved"), Selected("latin-america"), SortTitleAsc);

        Assert.Equal(["guadalupe"], results.Select(m => m.Id));
    }

    [Fact]
    public void Filter_by_query_matches_title_summary_country_tags_and_facet_labels()
    {
        Assert.Equal(["lanciano"], _browse.Filter(SampleIndex(), Facets, "Lanciano", Empty(), Empty(), Empty(), SortTitleAsc).Select(m => m.Id));
        Assert.Equal(["lanciano"], _browse.Filter(SampleIndex(), Facets, "Italy", Empty(), Empty(), Empty(), SortTitleAsc).Select(m => m.Id));
        Assert.Equal(["guadalupe"], _browse.Filter(SampleIndex(), Facets, "apparition", Empty(), Empty(), Empty(), SortTitleAsc).Select(m => m.Id));
        Assert.Equal(["lanciano"], _browse.Filter(SampleIndex(), Facets, "Eucharistic", Empty(), Empty(), Empty(), SortTitleAsc).Select(m => m.Id));
        Assert.Equal(["guadalupe"], _browse.Filter(SampleIndex(), Facets, "Latin America", Empty(), Empty(), Empty(), SortTitleAsc).Select(m => m.Id));
        Assert.Equal(["guadalupe"], _browse.Filter(SampleIndex(), Facets, "Church-approved", Empty(), Empty(), Empty(), SortTitleAsc).Select(m => m.Id));
        Assert.Equal(["lanciano"], _browse.Filter(SampleIndex(), Facets, "Historical", Empty(), Empty(), Empty(), SortTitleAsc).Select(m => m.Id));
    }

    [Fact]
    public void Pills_render_one_label_per_kind()
    {
        var pills = _browse.Pills(SampleIndex()[1], Facets);

        Assert.Equal(["Marian apparition", "Miraculous image / icon"], pills);
    }

    [Fact]
    public void CardSummary_appends_approval_century_and_country()
    {
        var summary = _browse.CardSummary(SampleIndex()[0], Facets);

        Assert.Contains("An 8th-century host that became flesh and blood.", summary);
        Assert.Contains("Historical / traditional", summary);
        Assert.Contains("8th century", summary);
        Assert.Contains("Italy", summary);
    }

    private static IReadOnlySet<string> Empty() => new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    private static IReadOnlySet<string> Selected(params string[] values) =>
        new HashSet<string>(values, StringComparer.OrdinalIgnoreCase);
}
