using Respondeo.LiturgicalCalendar;

namespace Respondeo.UnitTests.Content;

public class CalendarProfileTests
{
    [Theory]
    [InlineData(CalendarRegion.GeneralRoman, false, false, 39, 60, "Universal")]
    [InlineData(CalendarRegion.GeneralRoman, true, false, 39, 60, "Universal")]
    [InlineData(CalendarRegion.UnitedStates, false, true, 42, 63, "UnitedStates")]
    [InlineData(CalendarRegion.UnitedStates, true, true, 39, 63, "UnitedStates")]
    public void Options_resolve_a_consistent_policy(CalendarRegion region, bool thursday, bool regional,
        int ascensionOffset, int corpusChristiOffset, string obligation)
    {
        var profile = CalendarProfile.From(new(region, AscensionOnThursday: thursday));

        Assert.Equal(region, profile.CatalogRegion);
        Assert.Equal(regional, profile.EpiphanyOnSunday);
        Assert.Equal(regional, profile.IncludesThanksgiving);
        Assert.Equal(regional, profile.TransfersDayOfPrayerFromSunday);
        Assert.Equal(ascensionOffset, profile.AscensionOffset);
        Assert.Equal(corpusChristiOffset, profile.CorpusChristiOffset);
        Assert.Equal(obligation, profile.Obligation.ToString());
    }

    [Theory]
    [InlineData(CalendarRegion.GeneralRoman, false, 14, 4)]
    [InlineData(CalendarRegion.UnitedStates, false, 17, 7)]
    [InlineData(CalendarRegion.UnitedStates, true, 14, 7)]
    public void Profile_controls_dates_and_Ascension_obligation_together(CalendarRegion region, bool thursday, int mayDay, int juneDay)
    {
        var calendar = new RomanCalendar(new(region, AscensionOnThursday: thursday));
        var ascension = calendar.ForDate(new(2026, 5, mayDay));
        var corpusChristi = calendar.ForDate(new(2026, 6, juneDay));

        Assert.Equal("ascension", ascension.Celebration?.CalendarKey);
        Assert.Equal(LiturgicalObligationStatus.Obligatory, ascension.Obligation.Status);
        Assert.Equal("corpus-christi", corpusChristi.Celebration?.CalendarKey);
    }

    [Fact]
    public void Unsupported_profile_preserves_unknown_weekday_obligation_and_universal_dates()
    {
        var calendar = new RomanCalendar(new((CalendarRegion)999));

        Assert.Equal("ascension", calendar.ForDate(new(2026, 5, 14)).Celebration?.CalendarKey);
        Assert.Equal(LiturgicalObligation.Unknown, calendar.ForDate(new(2026, 5, 14)).Obligation);
        Assert.Equal(LiturgicalObligationRule.Sunday, calendar.ForDate(new(2026, 5, 17)).Obligation.Rule);
    }
}
