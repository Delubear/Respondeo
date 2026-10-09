using Respondeo.Content.Contracts;
using Respondeo.Services;

namespace Respondeo.UnitTests.Services;

/// <summary>
/// Unit tests for <see cref="SaintBrowse"/>, the pure browse logic for the saints catalog: facet-option
/// derivation, era ranking, combined facet/search/sort filtering, and the card presentation helpers.
/// Mirrors <see cref="MiracleBrowseTests"/>.
/// </summary>
public class SaintBrowseTests
{
    private readonly SaintBrowse _browse = new();

    private static readonly SaintFacetCatalog Facets = new()
    {
        Eras = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["medieval"] = "Medieval",
            ["modern"] = "Modern",
        },
        Regions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["europe"] = "Europe",
            ["middle-east"] = "Middle East",
        },
        StatesOfLife = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["religious"] = "Religious",
            ["priest"] = "Priest",
        },
        Designations = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["doctor-of-the-church"] = "Doctor of the Church",
            ["virgin"] = "Virgin",
        },
        Sexes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["male"] = "Male",
            ["female"] = "Female",
        },
        ReligiousOrders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["franciscan"] = "Franciscan",
            ["carmelite"] = "Carmelite",
        },
        Canonizations = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["canonized"] = "Canonized",
        },
    };

    private static readonly SortOption<SaintIndexEntry> SortTitleAsc =
        new("title-asc", "Title (A–Z)", rows => rows.OrderBy(s => s.SortValue, StringComparer.CurrentCultureIgnoreCase));

    private static readonly SortOption<SaintIndexEntry> SortChronological =
        new("era-asc", "Chronological", rows => rows.OrderBy(s => SaintBrowse.EraRank(s.Era)).ThenBy(s => s.SortValue, StringComparer.CurrentCultureIgnoreCase));

    private static IReadOnlyList<SaintIndexEntry> SampleIndex() =>
    [
        new SaintIndexEntry
        {
            Id = "francis",
            Title = "St. Francis of Assisi",
            Summary = "The Poor Man of Assisi.",
            Era = "medieval",
            Region = "europe",
            Patronages = ["Animals", "Ecology"],
            StatesOfLife = ["religious"],
            Designations = [],
            Sex = "male",
            ReligiousOrders = ["franciscan"],
            Canonizations = ["canonized"],
            Dates = "1181–1226",
            Tags = ["poverty"],
        },
        new SaintIndexEntry
        {
            Id = "therese",
            Title = "St. Thérèse of Lisieux",
            Summary = "The Little Flower.",
            Era = "modern",
            Region = "europe",
            Patronages = [],
            StatesOfLife = ["religious"],
            Designations = ["virgin", "doctor-of-the-church"],
            Sex = "female",
            ReligiousOrders = ["carmelite"],
            Canonizations = ["canonized"],
            Dates = "1873–1897",
            Tags = [],
        },
    ];

    private static HashSet<string> Empty => new(StringComparer.OrdinalIgnoreCase);

    [Fact]
    public void Eras_are_distinct_and_ordered_by_label()
    {
        var eras = _browse.Eras(SampleIndex(), Facets);

        Assert.Equal(["medieval", "modern"], eras);
    }

    [Fact]
    public void StatesOfLife_flattens_and_orders_by_label()
    {
        var states = _browse.StatesOfLife(SampleIndex(), Facets);

        Assert.Equal(["religious"], states);
    }

    [Fact]
    public void Designations_flattens_and_orders_by_label()
    {
        var designations = _browse.Designations(SampleIndex(), Facets);

        Assert.Equal(["doctor-of-the-church", "virgin"], designations);
    }

    [Fact]
    public void Sexes_are_distinct_and_ordered_by_label()
    {
        var sexes = _browse.Sexes(SampleIndex(), Facets);

        Assert.Equal(["female", "male"], sexes);
    }

    [Fact]
    public void ReligiousOrders_flattens_and_orders_by_label()
    {
        var orders = _browse.ReligiousOrders(SampleIndex(), Facets);

        Assert.Equal(["carmelite", "franciscan"], orders);
    }

    [Fact]
    public void Canonizations_flattens_and_orders_by_label()
    {
        var canonizations = _browse.Canonizations(SampleIndex(), Facets);

        Assert.Equal(["canonized"], canonizations);
    }

    [Theory]
    [InlineData("old-testament", 0)]
    [InlineData("early-church", 1)]
    [InlineData("medieval", 2)]
    [InlineData("modern", 4)]
    [InlineData("unknown", 5)]
    [InlineData(null, 5)]
    public void EraRank_ranks_known_eras_and_sends_unknowns_last(string? era, int expected)
    {
        Assert.Equal(expected, SaintBrowse.EraRank(era));
    }

    [Fact]
    public void Filter_without_criteria_returns_all_sorted()
    {
        var results = _browse.Filter(SampleIndex(), Facets, "", Empty, Empty, Empty, Empty, Empty, Empty, Empty, SortTitleAsc);

        Assert.Equal(["francis", "therese"], results.Select(r => r.Id));
    }

    [Fact]
    public void Filter_applies_facet_intersection()
    {
        var eras = new HashSet<string>(["modern"], StringComparer.OrdinalIgnoreCase);

        var results = _browse.Filter(SampleIndex(), Facets, "", eras, Empty, Empty, Empty, Empty, Empty, Empty, SortTitleAsc);

        var only = Assert.Single(results);
        Assert.Equal("therese", only.Id);
    }

    [Fact]
    public void Filter_matches_designation_within_a_multi_value_group()
    {
        var designations = new HashSet<string>(["doctor-of-the-church"], StringComparer.OrdinalIgnoreCase);

        var results = _browse.Filter(SampleIndex(), Facets, "", Empty, Empty, Empty, designations, Empty, Empty, Empty, SortTitleAsc);

        Assert.Equal("therese", Assert.Single(results).Id);
    }

    [Fact]
    public void Filter_matches_sex()
    {
        var sexes = new HashSet<string>(["female"], StringComparer.OrdinalIgnoreCase);

        var results = _browse.Filter(SampleIndex(), Facets, "", Empty, Empty, Empty, Empty, sexes, Empty, Empty, SortTitleAsc);

        Assert.Equal("therese", Assert.Single(results).Id);
    }

    [Fact]
    public void Filter_matches_religious_order()
    {
        var orders = new HashSet<string>(["franciscan"], StringComparer.OrdinalIgnoreCase);

        var results = _browse.Filter(SampleIndex(), Facets, "", Empty, Empty, Empty, Empty, Empty, orders, Empty, SortTitleAsc);

        Assert.Equal("francis", Assert.Single(results).Id);
    }

    [Fact]
    public void Filter_searches_title_summary_tags_patronage_and_facet_labels()
    {
        Assert.Equal("francis", Single("Poor Man").Id);
        Assert.Equal("francis", Single("poverty").Id);
        Assert.Equal("francis", Single("Animals").Id);
        Assert.Equal("therese", Single("Doctor").Id);

        SaintIndexEntry Single(string query) =>
            Assert.Single(_browse.Filter(SampleIndex(), Facets, query, Empty, Empty, Empty, Empty, Empty, Empty, Empty, SortTitleAsc));
    }

    [Fact]
    public void Filter_sorts_chronologically()
    {
        var results = _browse.Filter(SampleIndex(), Facets, "", Empty, Empty, Empty, Empty, Empty, Empty, Empty, SortChronological);

        Assert.Equal(["francis", "therese"], results.Select(r => r.Id));
    }

    [Fact]
    public void CardSummary_builds_facet_trail()
    {
        var francis = SampleIndex()[0];

        var summary = _browse.CardSummary(francis, Facets);

        Assert.Contains("The Poor Man of Assisi.", summary);
        Assert.Contains("Canonized", summary);
        Assert.Contains("Medieval", summary);
        Assert.Contains("1181–1226", summary);
        Assert.Contains("Europe", summary);
    }

    [Fact]
    public void Pills_render_one_per_patronage()
    {
        Assert.Equal(["Animals", "Ecology"], _browse.Pills(SampleIndex()[0]));
        Assert.Empty(_browse.Pills(SampleIndex()[1]));
    }
}
