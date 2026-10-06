using Respondeo.Content.Contracts;

namespace Respondeo.Content.Liturgy;

/// <summary>
/// A pure liturgical-calendar engine for the Ordinary Form.
/// Everything is derived from two anchors:
/// the fixed civil dates (Christmas is always 25 December) and Gregorian Easter (from which every moveable feast is an offset).
/// No per-day data is stored; the whole year is computed on demand.
/// </summary>
internal sealed class LiturgicalCalendar : ILiturgicalCalendar
{
    public LiturgicalDay ForDate(DateOnly date)
    {
        var celebration = PrincipalCelebration(date);
        var season = SeasonFor(date);
        var color = celebration?.Color ?? SeasonColor(season, date);

        return new LiturgicalDay(date, season, color, celebration);
    }

    // -----------------------------------------------------------------------
    // Named celebrations: the moveable feasts (Easter-relative) take precedence over the fixed-date solemnities when, rarely, they coincide.
    // -----------------------------------------------------------------------

    private static LiturgicalCelebration? PrincipalCelebration(DateOnly date) => MoveableCelebration(date) ?? FixedCelebration(date);

    /// <summary>Moveable feasts, each a fixed offset in days from Easter Sunday of the same year.</summary>
    private static LiturgicalCelebration? MoveableCelebration(DateOnly date)
    {
        var easter = Computus.GregorianEaster(date.Year);
        var offset = date.DayNumber - easter.DayNumber;

        return offset switch
        {
            -46 => new("Ash Wednesday", CelebrationRank.Feast, LiturgicalColor.Violet),
            -7 => new("Palm Sunday of the Passion of the Lord", CelebrationRank.Solemnity, LiturgicalColor.Red),
            -3 => new("Holy Thursday", CelebrationRank.Solemnity, LiturgicalColor.White),
            -2 => new("Good Friday of the Passion of the Lord", CelebrationRank.Solemnity, LiturgicalColor.Red),
            -1 => new("Holy Saturday", CelebrationRank.Solemnity, LiturgicalColor.White),
            0 => new("Easter Sunday of the Resurrection of the Lord", CelebrationRank.Solemnity, LiturgicalColor.White),
            39 => new("The Ascension of the Lord", CelebrationRank.Solemnity, LiturgicalColor.White),
            49 => new("Pentecost Sunday", CelebrationRank.Solemnity, LiturgicalColor.Red),
            56 => new("The Most Holy Trinity", CelebrationRank.Solemnity, LiturgicalColor.White),
            60 => new("The Most Holy Body and Blood of Christ", CelebrationRank.Solemnity, LiturgicalColor.White),
            68 => new("The Most Sacred Heart of Jesus", CelebrationRank.Solemnity, LiturgicalColor.White),
            _ => null,
        };
    }

    /// <summary>Principal fixed-date solemnities and feasts of the Lord in the general calendar.</summary>
    private static LiturgicalCelebration? FixedCelebration(DateOnly date) => (date.Month, date.Day) switch
        {
            (1, 1) => new("Mary, the Holy Mother of God", CelebrationRank.Solemnity, LiturgicalColor.White),
            (1, 6) => new("The Epiphany of the Lord", CelebrationRank.Solemnity, LiturgicalColor.White),
            (3, 19) => new("Saint Joseph, Spouse of the Blessed Virgin Mary", CelebrationRank.Solemnity, LiturgicalColor.White),
            (3, 25) => new("The Annunciation of the Lord", CelebrationRank.Solemnity, LiturgicalColor.White),
            (6, 24) => new("The Nativity of Saint John the Baptist", CelebrationRank.Solemnity, LiturgicalColor.White),
            (6, 29) => new("Saints Peter and Paul, Apostles", CelebrationRank.Solemnity, LiturgicalColor.Red),
            (8, 6) => new("The Transfiguration of the Lord", CelebrationRank.Feast, LiturgicalColor.White),
            (8, 15) => new("The Assumption of the Blessed Virgin Mary", CelebrationRank.Solemnity, LiturgicalColor.White),
            (11, 1) => new("All Saints", CelebrationRank.Solemnity, LiturgicalColor.White),
            (11, 2) => new("The Commemoration of All the Faithful Departed", CelebrationRank.Feast, LiturgicalColor.Violet),
            (12, 8) => new("The Immaculate Conception of the Blessed Virgin Mary", CelebrationRank.Solemnity, LiturgicalColor.White),
            (12, 25) => new("The Nativity of the Lord", CelebrationRank.Solemnity, LiturgicalColor.White),
            _ => null,
        };

    // -----------------------------------------------------------------------
    // Seasons, derived from Easter and the fixed Christmas / Epiphany anchors.
    // -----------------------------------------------------------------------

    private static LiturgicalSeason SeasonFor(DateOnly date)
    {
        var year = date.Year;
        var easter = Computus.GregorianEaster(year);
        var ashWednesday = easter.AddDays(-46);
        var holyThursday = easter.AddDays(-3);
        var pentecost = easter.AddDays(49);

        // Christmas Time spans the civil-year boundary, so test both ends first.
        var christmas = new DateOnly(year, 12, 25);
        var adventStart = AdventStart(year);
        var baptismOfTheLord = BaptismOfTheLord(year);

        // 1 January through the Baptism of the Lord still belongs to the previous year's Christmas.
        if (date <= baptismOfTheLord)
        {
            return LiturgicalSeason.Christmas;
        }

        // Lent runs from Ash Wednesday up to (but not including) the evening of Holy Thursday.
        if (date >= ashWednesday && date < holyThursday)
        {
            return LiturgicalSeason.Lent;
        }

        // The Triduum: Holy Thursday through Easter Sunday inclusive.
        if (date >= holyThursday && date <= easter)
        {
            return LiturgicalSeason.Triduum;
        }

        // Easter Time: the day after Easter through Pentecost.
        if (date > easter && date <= pentecost)
        {
            return LiturgicalSeason.Easter;
        }

        // Advent: the Advent-start Sunday through Christmas Eve.
        if (date >= adventStart && date < christmas)
        {
            return LiturgicalSeason.Advent;
        }

        // Christmas Time at the end of the year: Christmas Day through 31 December.
        if (date >= christmas)
        {
            return LiturgicalSeason.Christmas;
        }

        return LiturgicalSeason.OrdinaryTime;
    }

    /// <summary>
    /// The First Sunday of Advent: the fourth Sunday before Christmas Day (25 December).
    /// </summary>
    private static DateOnly AdventStart(int year)
    {
        var christmas = new DateOnly(year, 12, 25);

        // Step back to the Sunday on or before Christmas, then back three more Sundays.
        var daysAfterSunday = (int)christmas.DayOfWeek; // Sunday == 0
        var sundayBeforeChristmas = christmas.AddDays(-daysAfterSunday);
        return sundayBeforeChristmas.AddDays(-21);
    }

    /// <summary>
    /// The Baptism of the Lord, which closes Christmas Time: the Sunday after Epiphany (6 January).
    /// </summary>
    private static DateOnly BaptismOfTheLord(int year)
    {
        var epiphany = new DateOnly(year, 1, 6);
        var daysUntilSunday = (7 - (int)epiphany.DayOfWeek) % 7;

        // When Epiphany itself is a Sunday, the Baptism is the following Sunday (7 days later).
        return daysUntilSunday == 0 ? epiphany.AddDays(7) : epiphany.AddDays(daysUntilSunday);
    }

    private static LiturgicalColor SeasonColor(LiturgicalSeason season, DateOnly date) => season switch
    {
        LiturgicalSeason.Advent => IsGaudete(date) ? LiturgicalColor.Rose : LiturgicalColor.Violet,
        LiturgicalSeason.Lent => IsLaetare(date) ? LiturgicalColor.Rose : LiturgicalColor.Violet,
        LiturgicalSeason.Christmas => LiturgicalColor.White,
        LiturgicalSeason.Triduum => LiturgicalColor.White,
        LiturgicalSeason.Easter => LiturgicalColor.White,
        _ => LiturgicalColor.Green,
    };

    /// <summary>Gaudete Sunday: the Third Sunday of Advent (rose vestments).</summary>
    private static bool IsGaudete(DateOnly date) => date.DayOfWeek == DayOfWeek.Sunday && date == AdventStart(date.Year).AddDays(14);

    /// <summary>Laetare Sunday: the Fourth Sunday of Lent (rose vestments).</summary>
    private static bool IsLaetare(DateOnly date)
    {
        var easter = Computus.GregorianEaster(date.Year);

        // The Fourth Sunday of Lent falls 21 days before Easter Sunday.
        return date.DayOfWeek == DayOfWeek.Sunday && date == easter.AddDays(-21);
    }
}
