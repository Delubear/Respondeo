using Respondeo.Content.Contracts;
using Respondeo.Services;

namespace Respondeo.UnitTests.Services;

public class PrayerBrowseTests
{
    private readonly PrayerBrowse _browse = new()
    {
        Facets = new PrayerFacetCatalog
        {
            PrayedAt = new Dictionary<string, string> { ["daily"] = "Daily", ["mass"] = "Holy Mass", ["rosary"] = "Rosary" },
            Tags = new Dictionary<string, string> { ["marian"] = "Marian", ["dominical"] = "Dominical" },
        },
    };

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
			PrayedAt = ["daily", "rosary"],
			Tags = ["marian"],
		},
		new PrayerSummary
		{
			Id = "our-father",
			Title = "Our Father",
			Summary = "The prayer the Lord taught.",
			PrayedAt = ["mass"],
			Tags = ["dominical"],
		},
		new PrayerSummary
		{
			Id = "unsorted",
			Title = "A Nameless Prayer",
			Summary = "No particular occasion.",
			PrayedAt = [],
			Tags = [],
		},
	];

	[Fact]
	public void Labels_come_from_the_catalog_and_humanize_unmapped_slugs()
	{
		var prayedAt = _browse.Label(PrayerBrowse.PrayedAtFacet.Key);

		Assert.Equal("Holy Mass", prayedAt("mass"));
		Assert.Equal("Night prayer", prayedAt("night-prayer"));
		Assert.Equal("Marian", _browse.Label(PrayerBrowse.TagFacet.Key)("marian"));
	}

	[Fact]
	public void PrimaryPrayedAt_is_the_first_occasion_or_null()
	{
		var index = SampleIndex();

		Assert.Equal("Daily", _browse.PrimaryPrayedAt(index[0]));
		Assert.Null(_browse.PrimaryPrayedAt(index[2]));
	}

	[Fact]
	public void PrayedAt_options_are_slugs_sorted_by_label()
	{
		var options = _browse.Options(SampleIndex(), PrayerBrowse.PrayedAtFacet);

		Assert.Equal(["daily", "mass", "rosary"], options);
	}

	[Fact]
	public void Tags_are_distinct_and_sorted()
	{
		var tags = _browse.Tags(SampleIndex());

		Assert.Equal(["dominical", "marian"], tags);
	}

	[Fact]
	public void Filter_without_criteria_returns_all_sorted_by_title()
	{
		var results = _browse.Filter(SampleIndex(), "", Empty(), Empty(), SortAsc);

		Assert.Equal(["A Nameless Prayer", "Hail Mary", "Our Father"], results.Select(p => p.Title));
	}

	[Fact]
	public void Filter_by_prayed_at_matches_any_occasion_slug()
	{
		var results = _browse.Filter(SampleIndex(), "", Selected("rosary"), Empty(), SortAsc);

		Assert.Equal(["hail-mary"], results.Select(p => p.Id));
	}

	[Fact]
	public void Filter_by_tag_matches_any_selected_tag()
	{
		var results = _browse.Filter(SampleIndex(), "", Empty(), Selected("marian"), SortAsc);

		Assert.Equal(["hail-mary"], results.Select(p => p.Id));
	}

	[Fact]
	public void Filter_by_query_matches_title_summary_tags_and_prayed_at()
	{
		Assert.Equal(["hail-mary"], _browse.Filter(SampleIndex(), "angelic", Empty(), Empty(), SortAsc).Select(p => p.Id));
		Assert.Equal(["hail-mary"], _browse.Filter(SampleIndex(), "marian", Empty(), Empty(), SortAsc).Select(p => p.Id));
		Assert.Equal(["our-father"], _browse.Filter(SampleIndex(), "holy mass", Empty(), Empty(), SortAsc).Select(p => p.Id));
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
