using System.Collections.Frozen;
using Respondeo.LiturgicalCalendar;

namespace Respondeo.UnitTests.Content;

public class CalendarCatalogTests
{
    [Theory]
    [InlineData(CalendarRegion.GeneralRoman)]
    [InlineData(CalendarRegion.UnitedStates)]
    public void Embedded_catalogs_validate_without_resolving_any_dates(CalendarRegion region) =>
        CalendarCatalog.EnsureValid(region);

    [Theory]
    [InlineData("ash-wednesday")]
    [InlineData("epiphany")]
    [InlineData("sacred-heart")]
    public void Missing_computed_identity_fails_catalog_validation(string key)
    {
        var computed = ComputedCelebrationData.All.Where(entry => entry.CalendarKey != key).ToArray();

        var error = Assert.Throws<InvalidOperationException>(() => CalendarCatalog.Validate(CalendarRegion.GeneralRoman,
            GeneralRomanCalendarData.ForRegion(CalendarRegion.GeneralRoman), computed));

        Assert.Contains($"Computed identity '{key}' must be defined exactly once", error.Message);
        Assert.Contains("GeneralRoman", error.Message);
    }

    [Fact]
    public void Missing_transfer_identity_fails_before_its_nominal_date_is_queried()
    {
        var entries = GeneralRomanCalendarData.ForRegion(CalendarRegion.GeneralRoman).ToDictionary();
        entries.Remove((3, 19));

        var error = Assert.Throws<InvalidOperationException>(() => CalendarCatalog.Validate(CalendarRegion.GeneralRoman,
            entries, ComputedCelebrationData.All.ToArray()));

        Assert.Contains("'saint-joseph'", error.Message);
        Assert.Contains("03-19", error.Message);
    }

    [Fact]
    public void UnitedStates_profile_requires_its_prayer_day()
    {
        var error = Assert.Throws<InvalidOperationException>(() => CalendarCatalog.Validate(CalendarRegion.UnitedStates,
            GeneralRomanCalendarData.ForRegion(CalendarRegion.GeneralRoman), ComputedCelebrationData.All.ToArray()));

        Assert.Contains("'day-of-prayer-for-unborn-children'", error.Message);
        Assert.Contains("01-22", error.Message);
    }

    [Theory]
    [InlineData("epiphany")]
    [InlineData("saint-joseph")]
    public void Conflicting_definitions_across_catalogs_are_rejected(string key)
    {
        var computed = ComputedCelebrationData.All.ToList();
        if (key == "epiphany")
        {
            var index = computed.FindIndex(entry => entry.CalendarKey == key);
            computed[index] = computed[index] with { Name = "Conflicting label" };
        }
        else
        {
            computed.Add(GeneralRomanCalendarData.ForMonthDay(3, 19).Single(entry => entry.CalendarKey == key));
        }

        var error = Assert.Throws<InvalidOperationException>(() => CalendarCatalog.Validate(CalendarRegion.GeneralRoman,
            GeneralRomanCalendarData.ForRegion(CalendarRegion.GeneralRoman), computed));

        Assert.Contains($"Conflicting or duplicate identity '{key}'", error.Message);
    }

    [Fact]
    public void Embedded_fixed_definitions_require_identity_and_precedence_even_though_legacy_parser_rows_are_supported()
    {
        using var reader = new StringReader("03-19 | Solemnity | White | Legacy Joseph");
        var legacy = GeneralRomanCalendarData.Load(reader, "legacy.txt");

        var error = Assert.Throws<InvalidOperationException>(() => CalendarCatalog.Validate(CalendarRegion.GeneralRoman,
            legacy, ComputedCelebrationData.All.ToArray()));

        Assert.Contains("Legacy Joseph", error.Message);
        Assert.Contains("requires an authored calendar key and precedence", error.Message);
    }

    [Fact]
    public void Shared_catalog_collections_cannot_be_modified()
    {
        var fixedEntries = GeneralRomanCalendarData.ForRegion(CalendarRegion.GeneralRoman);
        Assert.IsAssignableFrom<FrozenDictionary<(int, int), IReadOnlyList<LiturgicalCelebration>>>(fixedEntries);
        var joseph = Assert.IsAssignableFrom<IList<LiturgicalCelebration>>(fixedEntries[(3, 19)]);
        Assert.Throws<NotSupportedException>(() => joseph.Clear());

        using var reader = new StringReader("example | Memorial | White | Example | GeneralMemorial");
        Assert.IsAssignableFrom<FrozenDictionary<string, LiturgicalCelebration>>(ComputedCelebrationData.Load(reader, "test.txt"));
        Assert.Equal("saint-joseph", Assert.Single(fixedEntries[(3, 19)]).CalendarKey);
    }

    [Theory]
    [InlineData(CalendarRegion.GeneralRoman)]
    [InlineData(CalendarRegion.UnitedStates)]
    public void Daily_rank_adjustments_and_transfers_leave_authored_definitions_unchanged(CalendarRegion region)
    {
        var calendar = new RomanCalendar(new(region));
        var originalHeart = ComputedCelebrationData.Get("immaculate-heart");
        var originalBoniface = GeneralRomanCalendarData.ForMonthDay(6, 5, region)
            .Single(entry => entry.CalendarKey == "saint-boniface-bishop-and-martyr");
        var originalJoseph = Assert.Single(GeneralRomanCalendarData.ForMonthDay(3, 19, region));

        var collision = calendar.ForDate(new(2027, 6, 5));
        Assert.Equal(2, collision.RankAdjustments.Count);
        Assert.All(collision.RankAdjustments, adjustment =>
        {
            Assert.Equal(CelebrationRank.Memorial, adjustment.Celebration.Rank);
            Assert.Equal(CelebrationRank.Memorial, adjustment.OriginalRank);
            Assert.Equal(CelebrationRank.OptionalMemorial, adjustment.AdjustedRank);
        });
        Assert.All(collision.OptionalMemorials, entry => Assert.Equal(CelebrationRank.OptionalMemorial, entry.Rank));
        Assert.Equal(CelebrationRank.Memorial, originalHeart.Rank);
        Assert.Equal(CelebrationRank.Memorial, originalBoniface.Rank);
        Assert.Same(originalHeart, ComputedCelebrationData.Get("immaculate-heart"));
        Assert.Same(originalBoniface, GeneralRomanCalendarData.ForMonthDay(6, 5, region)
            .Single(entry => entry.CalendarKey == originalBoniface.CalendarKey));

        var transferred = calendar.ForDate(new(2028, 3, 20));
        Assert.Equal(originalJoseph, transferred.Celebration);
        Assert.Equal(new DateOnly(2028, 3, 19), Assert.Single(transferred.Transfers).NominalDate);
        Assert.Same(originalJoseph, Assert.Single(GeneralRomanCalendarData.ForMonthDay(3, 19, region)));
        Assert.Equal(CelebrationRank.Memorial, calendar.ForDate(new(2026, 6, 5)).Celebration?.Rank);
    }
}
