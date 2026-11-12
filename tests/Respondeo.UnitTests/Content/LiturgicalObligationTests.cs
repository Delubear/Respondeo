using Respondeo.LiturgicalCalendar;

namespace Respondeo.UnitTests.Content;

public class LiturgicalObligationTests
{
    private static readonly RomanCalendar General = new();
    private static readonly RomanCalendar UnitedStates = new(new LiturgicalCalendarOptions(CalendarRegion.UnitedStates));
    private static readonly RomanCalendar UnitedStatesThursday = new(new LiturgicalCalendarOptions(
        CalendarRegion.UnitedStates, AscensionOnThursday: true));

    [Theory]
    [InlineData(2025, 1, 1)]
    [InlineData(2025, 1, 6)]
    [InlineData(2025, 3, 19)]
    [InlineData(2025, 6, 29)]
    [InlineData(2025, 8, 15)]
    [InlineData(2025, 11, 1)]
    [InlineData(2025, 12, 8)]
    [InlineData(2025, 12, 25)]
    public void GeneralRoman_IdentifiesUniversalFixedHolyDays(int year, int month, int day) =>
        Assert.Equal(LiturgicalObligationStatus.Obligatory,
            General.ForDate(new DateOnly(year, month, day)).Obligation.Status);

    [Theory]
    [InlineData(2026, 5, 14)]
    [InlineData(2026, 6, 4)]
    public void GeneralRoman_IdentifiesUniversalMoveableHolyDays(int year, int month, int day)
    {
        var obligation = General.ForDate(new DateOnly(year, month, day)).Obligation;

        Assert.Equal(LiturgicalObligationStatus.Obligatory, obligation.Status);
        Assert.Equal(LiturgicalObligationRule.UniversalCanon1246, obligation.Rule);
    }

    [Fact]
    public void UnitedStates_IdentifiesNationalHolyDaysAndSundayAscension()
    {
        Assert.Equal(LiturgicalObligationStatus.Obligatory, UnitedStates.ForDate(new(2026, 1, 1)).Obligation.Status);
        Assert.Equal(LiturgicalObligationStatus.Obligatory, UnitedStates.ForDate(new(2025, 12, 8)).Obligation.Status);
        Assert.Equal(LiturgicalObligationStatus.Obligatory, UnitedStates.ForDate(new(2025, 12, 25)).Obligation.Status);

        var ascensionSunday = UnitedStates.ForDate(new(2026, 5, 17)).Obligation;
        Assert.Equal(LiturgicalObligationStatus.Obligatory, ascensionSunday.Status);
        Assert.Equal(LiturgicalObligationRule.Sunday, ascensionSunday.Rule);
        Assert.Equal(LiturgicalObligationStatus.NotObligatory, UnitedStates.ForDate(new(2026, 5, 14)).Obligation.Status);
        Assert.Equal(LiturgicalObligationStatus.Obligatory, UnitedStatesThursday.ForDate(new(2026, 5, 14)).Obligation.Status);
    }

    [Theory]
    [InlineData(2022, 1, 1)]
    [InlineData(2022, 8, 15)]
    [InlineData(2021, 11, 1)]
    public void UnitedStates_AppliesSaturdayMondayExceptions(int year, int month, int day)
    {
        var obligation = UnitedStates.ForDate(new DateOnly(year, month, day)).Obligation;

        Assert.Equal(LiturgicalObligationStatus.NotObligatory, obligation.Status);
        Assert.Equal(LiturgicalObligationRule.UnitedStatesSaturdayMondayException, obligation.Rule);
    }

    [Fact]
    public void UnitedStates_DoesNotInferObligationFromSolemnityRank()
    {
        var day = UnitedStates.ForDate(new(2026, 6, 24));

        Assert.Equal(CelebrationRank.Solemnity, day.Celebration?.Rank);
        Assert.Equal(LiturgicalObligationStatus.NotObligatory, day.Obligation.Status);
    }

    [Fact]
    public void SundayRemainsObligatoryWhenAUnitedStatesExceptionDateFallsOnSunday()
    {
        var obligation = UnitedStates.ForDate(new(2021, 8, 15)).Obligation;

        Assert.Equal(LiturgicalObligationStatus.Obligatory, obligation.Status);
        Assert.Equal(LiturgicalObligationRule.Sunday, obligation.Rule);
    }

    [Fact]
    public void TransferredUniversalHolyDayHasUnknownObligationEffectOnTheObservedDate()
    {
        var sunday = General.ForDate(new(2024, 12, 8));
        var monday = General.ForDate(new(2024, 12, 9));

        Assert.Equal(LiturgicalObligationStatus.Obligatory, sunday.Obligation.Status);
        Assert.Equal(LiturgicalObligationRule.Sunday, sunday.Obligation.Rule);
        Assert.Equal(LiturgicalObligationStatus.Unknown, monday.Obligation.Status);
        Assert.Equal(LiturgicalObligationRule.TransferEffectNotModeled, monday.Obligation.Rule);
        Assert.Contains(monday.Transfers, transfer => transfer.CelebrationKey == "immaculate-conception");
    }

    [Fact]
    public void DayEqualityIncludesObligationStatus()
    {
        var day = General.ForDate(new(2026, 6, 4));
        var changed = day with
        {
            Obligation = new(LiturgicalObligationStatus.NotObligatory, LiturgicalObligationRule.NoApplicableObligation),
        };

        Assert.NotEqual(day, changed);
        Assert.NotEqual(day.GetHashCode(), changed.GetHashCode());
    }
}
