using Respondeo.LiturgicalCalendar;

namespace Respondeo.LiturgyOfTheHours;

/// <summary>
/// Computes the week of the four-week psalter for a date.
/// The psalter restarts at Week I on the First Sunday of Advent, the First Sunday of Lent, and the First Week in Ordinary Time,
/// and otherwise runs in a continuous 1-2-3-4 cycle (General Instruction of the Liturgy of the Hours, 133).
/// </summary>
internal static class PsalterWeek
{
    public static int For(DateOnly date, LiturgicalSeason season, bool epiphanyTransferredToSunday = false)
    {
        var easter = LiturgicalDates.Easter(date.Year);
        var ashWednesday = LiturgicalDates.AshWednesday(date.Year);
        var firstSundayOfLent = LiturgicalDates.FirstSundayOfLent(date.Year);

        switch (season)
        {
            case LiturgicalSeason.Advent:
                return Cycle(WeeksSince(LiturgicalDates.FirstSundayOfAdvent(date.Year), date));

            case LiturgicalSeason.Christmas:
            {
                // The psalter continues from Advent; Christmas Time begins its own count from the Sunday after Christmas.
                var christmas = date.Month == 12 ? new DateOnly(date.Year, 12, 25) : new DateOnly(date.Year - 1, 12, 25);
                var sundayAfter = christmas.AddDays(christmas.DayOfWeek == DayOfWeek.Sunday ? 7 : 7 - (int)christmas.DayOfWeek);

                return date < sundayAfter ? 1 : Cycle(WeeksSince(sundayAfter, date));
            }

            case LiturgicalSeason.Lent:
                return date < firstSundayOfLent && date >= ashWednesday ? 4 : Cycle(WeeksSince(firstSundayOfLent, date));

            case LiturgicalSeason.Triduum:
                return 2;

            case LiturgicalSeason.Easter:
                return Cycle(WeeksSince(easter, date));

            default:
                return Cycle(OrdinaryTimeWeek(date, easter, epiphanyTransferredToSunday) - 1);
        }
    }

    /// <summary>The week of Ordinary Time (1 to 34) containing the date.</summary>
    internal static int OrdinaryTimeWeek(DateOnly date, DateOnly easter, bool epiphanyTransferredToSunday = false)
    {
        var pentecost = easter.AddDays(49);

        if (date > pentecost)
        {
            var nextAdvent = LiturgicalDates.FirstSundayOfAdvent(date.Year);
            var weeksBeforeAdvent = ((nextAdvent.DayNumber - date.DayNumber) - 1) / 7;

            return 34 - weeksBeforeAdvent;
        }

        // The First Week in Ordinary Time begins the day after the Baptism of the Lord, which falls on the Sunday after January 6
        // (or the Monday when Epiphany, transferred to Sunday, falls on January 7 or 8).
        var baptism = LiturgicalDates.BaptismOfTheLord(date.Year, epiphanyTransferredToSunday);
        var anchor = baptism.AddDays(-(int)baptism.DayOfWeek);

        return WeeksSince(anchor, date) + 1;
    }

    private static int WeeksSince(DateOnly sunday, DateOnly date) => Math.Max(0, (date.DayNumber - sunday.DayNumber) / 7);

    private static int Cycle(int weeksElapsed) => (weeksElapsed % 4) + 1;
}
