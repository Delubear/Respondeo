namespace Respondeo.LiturgicalCalendar;

internal enum ObligationPolicy
{
    Unknown,
    Universal,
    UnitedStates,
}

/// <summary>Resolved regional policy; date algorithms and collision rules remain in their domain resolvers.</summary>
internal sealed record CalendarProfile(
    CalendarRegion CatalogRegion,
    bool EpiphanyOnSunday,
    int AscensionOffset,
    int CorpusChristiOffset,
    bool IncludesThanksgiving,
    bool TransfersDayOfPrayerFromSunday,
    ObligationPolicy Obligation)
{
    internal static CalendarProfile From(LiturgicalCalendarOptions options) => options.Region switch
    {
        CalendarRegion.UnitedStates => new(CalendarRegion.UnitedStates, true, options.AscensionOnThursday ? 39 : 42,
            63, true, true, ObligationPolicy.UnitedStates),
        CalendarRegion.GeneralRoman => new(CalendarRegion.GeneralRoman, false, 39, 60, false, false, ObligationPolicy.Universal),
        _ => new(CalendarRegion.GeneralRoman, false, 39, 60, false, false, ObligationPolicy.Unknown),
    };
}
