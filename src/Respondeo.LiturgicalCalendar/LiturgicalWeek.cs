namespace Respondeo.LiturgicalCalendar;

/// <summary>Background temporal and Psalter weeks, independent of a celebration's proper psalmody.</summary>
/// <param name="TemporalWeek">The numbered Advent, Lent, Easter, or Ordinary Time week; null during Christmas, the Triduum, and the days after Ash Wednesday.</param>
/// <param name="PsalterWeek">The background four-week cycle (1-4), not a command to replace proper psalms. Christmas retains the existing provisional assignment.</param>
public sealed record LiturgicalWeek(int? TemporalWeek, int PsalterWeek);

internal static class LiturgicalWeekResolver
{
    internal static LiturgicalWeek Resolve(LiturgicalDay day, LiturgicalCalendarOptions options)
    {
        var date = day.Date;
        var easter = LiturgicalDates.Easter(date.Year);
        switch (day.Season)
        {
            case LiturgicalSeason.Advent:
                return NumberedWeek(date, LiturgicalDates.FirstSundayOfAdvent(date.Year));
            case LiturgicalSeason.Lent:
                var lent = LiturgicalDates.FirstSundayOfLent(date.Year);
                return date < lent ? new(null, 4) : NumberedWeek(date, lent);
            case LiturgicalSeason.Triduum:
                return date == easter ? new(null, 1) : new(null, 2);
            case LiturgicalSeason.Easter:
                return NumberedWeek(date, easter);
            case LiturgicalSeason.OrdinaryTime:
                var week = OrdinaryTimeWeek(date, options);
                return new(week, Cycle(week));
            case LiturgicalSeason.Christmas:
                // Preserve the existing background lookup convention pending approved-book Christmas reconciliation.
                var christmas = date.Month == 12 ? new DateOnly(date.Year, 12, 25) : new DateOnly(date.Year - 1, 12, 25);
                var sundayAfter = christmas.AddDays(christmas.DayOfWeek == DayOfWeek.Sunday ? 7 : 7 - (int)christmas.DayOfWeek);
                return new(null, date < sundayAfter ? 1 : NumberedWeek(date, sundayAfter).PsalterWeek);
            default:
                throw new ArgumentOutOfRangeException(nameof(day), day.Season, "Unsupported liturgical season.");
        }
    }

    internal static int OrdinaryTimeWeek(DateOnly date, LiturgicalCalendarOptions options)
    {
        if (date > LiturgicalDates.Pentecost(date.Year))
        {
            var advent = LiturgicalDates.FirstSundayOfAdvent(date.Year);
            return 34 - ((advent.DayNumber - date.DayNumber - 1) / 7);
        }

        var profile = CalendarProfile.From(options);
        var baptism = LiturgicalDates.BaptismOfTheLord(date.Year, profile.EpiphanyOnSunday);
        var sunday = baptism.AddDays(-(int)baptism.DayOfWeek);
        return ((date.DayNumber - sunday.DayNumber) / 7) + 1;
    }

    private static LiturgicalWeek NumberedWeek(DateOnly date, DateOnly firstSunday)
    {
        var week = ((date.DayNumber - firstSunday.DayNumber) / 7) + 1;
        return new(week, Cycle(week));
    }

    private static int Cycle(int week) => ((week - 1) % 4) + 1;
}
