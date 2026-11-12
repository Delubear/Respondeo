namespace Respondeo.LiturgicalCalendar;

/// <summary>The particular calendar layered over the General Roman Calendar.</summary>
public enum CalendarRegion
{
    /// <summary>The General Roman Calendar, as kept universally.</summary>
    GeneralRoman,

    /// <summary>The Proper Calendar for the Dioceses of the United States of America (USCCB).</summary>
    UnitedStates,
}

/// <summary>Selects which calendar the engine computes.</summary>
/// <param name="Region">The calendar region.</param>
/// <param name="AscensionOnThursday">
/// United States only: keep the Ascension on Thursday rather than transferring it to the Seventh Sunday of Easter.
/// The USCCB 2026 calendar lists Boston, Hartford, New York, Omaha, and Philadelphia as Thursday provinces.
/// This explicit preference does not resolve historical policies or diocese membership.
/// </param>
/// <param name="SaintJohnTheBaptistIsPatron">Whether Saint John the Baptist is a patron in the selected local calendar.</param>
public sealed record LiturgicalCalendarOptions(
    CalendarRegion Region = CalendarRegion.GeneralRoman,
    bool AscensionOnThursday = false,
    bool SaintJohnTheBaptistIsPatron = false)
{
    public static LiturgicalCalendarOptions Default { get; } = new();
}

/// <summary>Creates calendar engines for a given set of options.</summary>
public static class LiturgicalCalendars
{
    public static ILiturgicalCalendar Create(LiturgicalCalendarOptions options) => new RomanCalendar(options);
}
