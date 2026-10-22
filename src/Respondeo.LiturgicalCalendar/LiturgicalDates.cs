namespace Respondeo.LiturgicalCalendar;

/// <summary>
/// The fixed anchors of the Roman liturgical year (Easter and its dependents, Advent, Epiphany, and the Baptism of the Lord),
/// shared by the calendar engine and the Liturgy of the Hours.
/// </summary>
public static class LiturgicalDates
{
    /// <summary>Easter Sunday in the Gregorian calendar.</summary>
    public static DateOnly Easter(int year) => Computus.GregorianEaster(year);

    /// <summary>Ash Wednesday: 46 days before Easter.</summary>
    public static DateOnly AshWednesday(int year) => Easter(year).AddDays(-46);

    /// <summary>The First Sunday of Lent: 42 days before Easter.</summary>
    public static DateOnly FirstSundayOfLent(int year) => Easter(year).AddDays(-42);

    /// <summary>Pentecost Sunday: 49 days after Easter.</summary>
    public static DateOnly Pentecost(int year) => Easter(year).AddDays(49);

    /// <summary>The First Sunday of Advent: the fourth Sunday before Christmas Day (25 December).</summary>
    public static DateOnly FirstSundayOfAdvent(int year)
    {
        var christmas = new DateOnly(year, 12, 25);
        var daysBack = christmas.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)christmas.DayOfWeek;

        return christmas.AddDays(-daysBack - 21);
    }

    /// <summary>
    /// The Epiphany: 6 January, or, where it is transferred to Sunday (as in the United States), the Sunday between 2 and 8 January.
    /// </summary>
    public static DateOnly Epiphany(int year, bool transferredToSunday)
    {
        if (!transferredToSunday)
        {
            return new DateOnly(year, 1, 6);
        }

        var second = new DateOnly(year, 1, 2);
        return second.AddDays((7 - (int)second.DayOfWeek) % 7);
    }

    /// <summary>
    /// The Baptism of the Lord, which closes Christmas Time: the Sunday after the Epiphany,
    /// or the following Monday when a transferred Epiphany falls on 7 or 8 January.
    /// </summary>
    public static DateOnly BaptismOfTheLord(int year, bool epiphanyTransferredToSunday)
    {
        var epiphany = Epiphany(year, epiphanyTransferredToSunday);
        if (epiphanyTransferredToSunday)
        {
            return epiphany.Day >= 7 ? epiphany.AddDays(1) : epiphany.AddDays(7);
        }

        var daysUntilSunday = (7 - (int)epiphany.DayOfWeek) % 7;
        return daysUntilSunday == 0 ? epiphany.AddDays(7) : epiphany.AddDays(daysUntilSunday);
    }
}
