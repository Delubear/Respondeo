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
        Eras = new Dictionary<string, SaintEra>(StringComparer.OrdinalIgnoreCase)
        {
            ["medieval"] = new SaintEra("Medieval", "The Middle Ages."),
            ["modern"] = new SaintEra("Modern", "The modern era."),
        },
        Regions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["europe"] = "Europe",
            ["middle-east"] = "Middle East",
        },
        StatesOfLife = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["religious"] = "Religious",
            ["founder"] = "Founder",
        },
        Canonizations = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["canonized"] = "Canonized",
            ["doctor-of-the-church"] = "Doctor of the Church",
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
            StatesOfLife = ["religious", "founder"],
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
            Canonizations = ["canonized", "doctor-of-the-church"],
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

        Assert.Equal(["founder", "religious"], states);
    }

    [Fact]
    public void Canonizations_flattens_and_orders_by_label()
    {
        var canonizations = _browse.Canonizations(SampleIndex(), Facets);

        Assert.Equal(["canonized", "doctor-of-the-church"], canonizations);
    }

    [Theory]
    [InlineData("early-church", 0)]
    [InlineData("medieval", 1)]
    [InlineData("modern", 3)]
    [InlineData("unknown", 4)]
    [InlineData(null, 4)]
    public void EraRank_ranks_known_eras_and_sends_unknowns_last(string? era, int expected)
    {
        Assert.Equal(expected, SaintBrowse.EraRank(era));
    }

    [Fact]
    public void Filter_without_criteria_returns_all_sorted()
    {
        var results = _browse.Filter(SampleIndex(), Facets, "", Empty, Empty, Empty, Empty, SortTitleAsc);

        Assert.Equal(["francis", "therese"], results.Select(r => r.Id));
    }

    [Fact]
    public void Filter_applies_facet_intersection()
    {
        var eras = new HashSet<string>(["modern"], StringComparer.OrdinalIgnoreCase);

        var results = _browse.Filter(SampleIndex(), Facets, "", eras, Empty, Empty, Empty, SortTitleAsc);

        var only = Assert.Single(results);
        Assert.Equal("therese", only.Id);
    }

    [Fact]
    public void Filter_matches_canonization_within_a_multi_value_group()
    {
        var canonizations = new HashSet<string>(["doctor-of-the-church"], StringComparer.OrdinalIgnoreCase);

        var results = _browse.Filter(SampleIndex(), Facets, "", Empty, Empty, Empty, canonizations, SortTitleAsc);

        Assert.Equal("therese", Assert.Single(results).Id);
    }

    [Fact]
    public void Filter_searches_title_summary_tags_patronage_and_facet_labels()
    {
        Assert.Equal("francis", Single("Poor Man").Id);
        Assert.Equal("francis", Single("poverty").Id);
        Assert.Equal("francis", Single("Animals").Id);
        Assert.Equal("therese", Single("Doctor").Id);

        SaintIndexEntry Single(string query) =>
            Assert.Single(_browse.Filter(SampleIndex(), Facets, query, Empty, Empty, Empty, Empty, SortTitleAsc));
    }

    [Fact]
    public void Filter_sorts_chronologically()
    {
        var results = _browse.Filter(SampleIndex(), Facets, "", Empty, Empty, Empty, Empty, SortChronological);

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
