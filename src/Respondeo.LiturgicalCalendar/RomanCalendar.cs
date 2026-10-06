namespace Respondeo.LiturgicalCalendar;

/// <summary>
/// A pure liturgical-calendar engine for the Ordinary Form.
/// Everything is derived from two anchors:
/// the fixed civil dates (Christmas is always 25 December) and Gregorian Easter (from which every moveable feast is an offset).
/// No per-day data is stored; the whole year is computed on demand.
/// </summary>
internal sealed class RomanCalendar : ILiturgicalCalendar
{
    public LiturgicalDay ForDate(DateOnly date)
    {
        var season = SeasonFor(date);
        var resolution = ResolveCelebrations(date, season);
        var color = resolution.Principal?.Color ?? SeasonColor(season, date);
        var ferialName = resolution.Principal is null ? FerialName(date, season) : null;

        return new LiturgicalDay(date, season, color, resolution.Principal)
        {
            FerialName = ferialName,
            OptionalMemorials = resolution.OptionalMemorials,
        };
    }

    // -----------------------------------------------------------------------
    // Precedence resolution: choose the single principal celebration for the day and the optional memorials offered alongside it,
    // following the Church's order of precedence.
    // -----------------------------------------------------------------------

    /// <summary>A named celebration claiming a day, flagged when it is a celebration of the Lord.</summary>
    private readonly record struct Candidate(LiturgicalCelebration Celebration, bool OfTheLord);

    private readonly record struct DayResolution(LiturgicalCelebration? Principal, IReadOnlyList<LiturgicalCelebration> OptionalMemorials);

    /// <summary>Fixed feasts/commemorations of the Lord (or the equivalent) that replace a Sunday.</summary>
    private static readonly HashSet<(int, int)> SundayReplacingFixedDates = [(2, 2), (8, 6), (9, 14), (11, 9), (11, 2)];

    private static DayResolution ResolveCelebrations(DateOnly date, LiturgicalSeason season)
    {
        var named = NamedCelebrations(date, season);

        var solemnity = HighestOfRank(named, CelebrationRank.Solemnity);
        var isSunday = date.DayOfWeek == DayOfWeek.Sunday;
        var sundayIsPrivileged = isSunday && season is LiturgicalSeason.Advent or LiturgicalSeason.Lent or LiturgicalSeason.Easter;

        // A solemnity outranks everything except a privileged Sunday (Advent/Lent/Easter),
        // onto which a non-dominical solemnity would be transferred rather than celebrated (transfer handled elsewhere).
        if (solemnity is { } sol && !(sundayIsPrivileged && !sol.OfTheLord))
        {
            return new DayResolution(sol.Celebration, []);
        }

        // On any Sunday only a feast OF THE LORD may replace the Sunday; saints' feasts and memorials are omitted.
        if (isSunday)
        {
            var lordFeast = named.FirstOrDefault(c => c.OfTheLord && c.Celebration.Rank == CelebrationRank.Feast);
            return new DayResolution(lordFeast.Celebration, []);
        }

        var feast = HighestOfRank(named, CelebrationRank.Feast);
        if (feast is { } f)
        {
            return new DayResolution(f.Celebration, []);
        }

        // Weekdays: an obligatory memorial is the principal unless the weekday is privileged
        // (Lent, the Octaves, and the final Advent days), where every memorial is reduced to optional.
        var privilegedWeekday = IsPrivilegedWeekday(date, season);
        LiturgicalCelebration? principal = null;
        var optional = new List<LiturgicalCelebration>();
        foreach (var candidate in named.Where(c => c.Celebration.Rank is CelebrationRank.Memorial or CelebrationRank.OptionalMemorial))
        {
            var isObligatory = candidate.Celebration.Rank == CelebrationRank.Memorial;
            if (isObligatory && !privilegedWeekday && principal is null)
            {
                principal = candidate.Celebration;
            }
            else
            {
                optional.Add(candidate.Celebration);
            }
        }

        return new DayResolution(principal, optional);
    }

    private static Candidate? HighestOfRank(IReadOnlyList<Candidate> candidates, CelebrationRank rank)
    {
        Candidate? match = null;
        foreach (var candidate in candidates)
        {
            if (candidate.Celebration.Rank != rank)
            {
                continue;
            }

            // Prefer a celebration of the Lord when several share the same rank on one day.
            if (match is null || (candidate.OfTheLord && !match.Value.OfTheLord))
            {
                match = candidate;
            }
        }

        return match;
    }

    /// <summary>All named celebrations claiming the day: moveable, Sunday-based feasts of the Lord, and fixed dataset entries.</summary>
    private static IReadOnlyList<Candidate> NamedCelebrations(DateOnly date, LiturgicalSeason season)
    {
        var candidates = new List<Candidate>();

        if (MoveableCelebration(date) is { } moveable)
        {
            candidates.Add(new Candidate(moveable, OfTheLord: true));
        }

        foreach (var lordFeast in SundayLordCelebrations(date))
        {
            candidates.Add(new Candidate(lordFeast, OfTheLord: true));
        }

        foreach (var fixedCelebration in GeneralRomanCalendarData.ForMonthDay(date.Month, date.Day))
        {
            // A solemnity impeded by a Lenten Sunday, Holy Week, or the Octave of Easter is suppressed here and re-emitted on its transfer date below.
            if (IsTransferredAway(date))
            {
                continue;
            }

            candidates.Add(new Candidate(fixedCelebration, SundayReplacingFixedDates.Contains((date.Month, date.Day))));
        }

        foreach (var transferred in TransferredSolemnities(date))
        {
            candidates.Add(new Candidate(transferred, OfTheLord: false));
        }

        return candidates;
    }

    // -----------------------------------------------------------------------
    // Solemnity transfers:
    // Saint Joseph (19 March) and the Annunciation (25 March) are moved when they collide with a Sunday of Lent, Holy Week, or the Octaveof Easter, which always outrank them.
    // -----------------------------------------------------------------------

    private static readonly (int Month, int Day, string Name)[] TransferableSolemnities =
    [
        (3, 19, "Saint Joseph, Spouse of the Blessed Virgin Mary"),
        (3, 25, "The Annunciation of the Lord"),
    ];

    /// <summary>True when the given date is the ordinary date of a transferable solemnity that is impeded this year.</summary>
    private static bool IsTransferredAway(DateOnly date) =>
        TransferableSolemnities.Any(s => s.Month == date.Month && s.Day == date.Day && TransferTarget(date.Year, s.Month, s.Day) is not null);

    /// <summary>Transferable solemnities whose transfer date is the given date this year.</summary>
    private static IEnumerable<LiturgicalCelebration> TransferredSolemnities(DateOnly date)
    {
        foreach (var solemnity in TransferableSolemnities)
        {
            if (TransferTarget(date.Year, solemnity.Month, solemnity.Day) == date)
            {
                yield return new LiturgicalCelebration(solemnity.Name, CelebrationRank.Solemnity, LiturgicalColor.White);
            }
        }
    }

    /// <summary>
    /// The date a solemnity is transferred to when its ordinary date is impeded, or <c>null</c> when it is free.
    /// Holy Week and the Octave of Easter push the solemnity to the Monday after the Second Sunday of Easter;
    /// a Sunday of Lent pushes it to the following Monday.
    /// </summary>
    private static DateOnly? TransferTarget(int year, int month, int day)
    {
        var original = new DateOnly(year, month, day);
        var easter = Computus.GregorianEaster(year);

        // Holy Week, the Triduum, and the Octave of Easter (Palm Sunday through Divine Mercy Sunday).
        if (original >= easter.AddDays(-7) && original <= easter.AddDays(7))
        {
            return easter.AddDays(8);
        }

        if (original.DayOfWeek == DayOfWeek.Sunday && SeasonFor(original) == LiturgicalSeason.Lent)
        {
            return original.AddDays(1);
        }

        return null;
    }

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

    /// <summary>Feasts of the Lord anchored to a Sunday (or the Christmas octave): Holy Family, the Baptism of the Lord, and Christ the King.</summary>
    private static IEnumerable<LiturgicalCelebration> SundayLordCelebrations(DateOnly date)
    {
        var year = date.Year;

        if (date == HolyFamily(year))
        {
            yield return new LiturgicalCelebration("The Holy Family of Jesus, Mary and Joseph", CelebrationRank.Feast, LiturgicalColor.White);
        }

        if (date == BaptismOfTheLord(year))
        {
            yield return new LiturgicalCelebration("The Baptism of the Lord", CelebrationRank.Feast, LiturgicalColor.White);
        }

        if (date == AdventStart(year).AddDays(-7))
        {
            yield return new LiturgicalCelebration("Our Lord Jesus Christ, King of the Universe", CelebrationRank.Solemnity, LiturgicalColor.White);
        }
    }

    /// <summary>The Holy Family: the Sunday within the Octave of Christmas, or 30 December when Christmas itself is a Sunday.</summary>
    private static DateOnly HolyFamily(int year)
    {
        var christmas = new DateOnly(year, 12, 25);
        if (christmas.DayOfWeek == DayOfWeek.Sunday)
        {
            return new DateOnly(year, 12, 30);
        }

        var daysUntilSunday = (7 - (int)christmas.DayOfWeek) % 7;
        return christmas.AddDays(daysUntilSunday == 0 ? 7 : daysUntilSunday);
    }

    /// <summary>True on weekdays whose rank reduces every memorial to optional: Lenten/Triduum weekdays, the Octaves, and 17-24 December.</summary>
    private static bool IsPrivilegedWeekday(DateOnly date, LiturgicalSeason season)
    {
        return season switch
        {
            LiturgicalSeason.Lent or LiturgicalSeason.Triduum => true,
            LiturgicalSeason.Advent => date is { Month: 12, Day: >= 17 },
            LiturgicalSeason.Christmas => date is { Month: 12, Day: >= 25 },
            LiturgicalSeason.Easter => date <= Computus.GregorianEaster(date.Year).AddDays(7),
            _ => false,
        };
    }

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

    // -----------------------------------------------------------------------
    // Ferial naming: the descriptive name of a day that has no principal celebration (e.g. "Monday of the Third Week of Lent").
    // -----------------------------------------------------------------------

    private static readonly string[] Ordinals =
    [
        "First", "Second", "Third", "Fourth", "Fifth", "Sixth", "Seventh", "Eighth", "Ninth", "Tenth",
        "Eleventh", "Twelfth", "Thirteenth", "Fourteenth", "Fifteenth", "Sixteenth", "Seventeenth", "Eighteenth", "Nineteenth", "Twentieth",
        "Twenty-First", "Twenty-Second", "Twenty-Third", "Twenty-Fourth", "Twenty-Fifth", "Twenty-Sixth", "Twenty-Seventh", "Twenty-Eighth", "Twenty-Ninth", "Thirtieth",
        "Thirty-First", "Thirty-Second", "Thirty-Third", "Thirty-Fourth",
    ];

    private static string Ordinal(int n) => n >= 1 && n <= Ordinals.Length ? Ordinals[n - 1] : n.ToString();

    private static string FerialName(DateOnly date, LiturgicalSeason season) => season switch
    {
        LiturgicalSeason.Advent => AdventFerialName(date),
        LiturgicalSeason.Christmas => ChristmasFerialName(date),
        LiturgicalSeason.Lent => LentFerialName(date),
        LiturgicalSeason.Triduum => $"{date.DayOfWeek} of the Paschal Triduum",
        LiturgicalSeason.Easter => EasterFerialName(date),
        _ => SeasonDayName(date, OrdinaryTimeWeek(date), "Ordinary Time", "in"),
    };

    /// <summary>"Third Sunday of Advent" or "Monday of the Third Week of Advent" etc.</summary>
    private static string AdventFerialName(DateOnly date)
    {
        var week = ((date.DayNumber - AdventStart(date.Year).DayNumber) / 7) + 1;
        return SeasonDayName(date, week, "Advent", "of");
    }

    private static string ChristmasFerialName(DateOnly date)
    {
        // Within the Octave of the Nativity (25 December to 1 January).
        if (date is { Month: 12, Day: >= 25 })
        {
            return $"{Ordinal(date.Day - 24)} Day within the Octave of the Nativity";
        }

        if (date is { Month: 1, Day: 1 })
        {
            return "The Octave Day of the Nativity";
        }

        return $"{date.DayOfWeek} of Christmas Time";
    }

    private static string LentFerialName(DateOnly date)
    {
        var easter = Computus.GregorianEaster(date.Year);
        var firstSundayOfLent = easter.AddDays(-42);

        // The four days from Ash Wednesday to the Saturday before the First Sunday of Lent.
        if (date < firstSundayOfLent)
        {
            return $"{date.DayOfWeek} after Ash Wednesday";
        }

        // Holy Week: the weekdays from the Monday after Palm Sunday up to the Triduum.
        if (date >= easter.AddDays(-7))
        {
            return $"{date.DayOfWeek} of Holy Week";
        }

        var week = ((date.DayNumber - firstSundayOfLent.DayNumber) / 7) + 1;
        return SeasonDayName(date, week, "Lent", "of");
    }

    private static string EasterFerialName(DateOnly date)
    {
        var easter = Computus.GregorianEaster(date.Year);

        // The weekdays within the Octave of Easter (the Sunday a week later begins the Second Week).
        if (date > easter && date < easter.AddDays(7))
        {
            return $"{date.DayOfWeek} within the Octave of Easter";
        }

        var week = ((date.DayNumber - easter.DayNumber) / 7) + 1;
        return SeasonDayName(date, week, "Easter", "of");
    }

    /// <summary>The week of Ordinary Time, numbered 1-34 across the two spans before Lent and after Pentecost.</summary>
    private static int OrdinaryTimeWeek(DateOnly date)
    {
        var year = date.Year;
        var precedingSunday = date.AddDays(-(int)date.DayOfWeek);
        var ashWednesday = Computus.GregorianEaster(year).AddDays(-46);

        if (date < ashWednesday)
        {
            // First span: counted from the Baptism of the Lord, whose week is the First Week in Ordinary Time.
            return ((precedingSunday.DayNumber - BaptismOfTheLord(year).DayNumber) / 7) + 1;
        }

        // Second span: counted back from the Thirty-Fourth Sunday (Christ the King), the last before Advent.
        var christKingSunday = AdventStart(year).AddDays(-7);
        return 34 - ((christKingSunday.DayNumber - precedingSunday.DayNumber) / 7);
    }

    private static string SeasonDayName(DateOnly date, int week, string season, string preposition)
    {
        var ordinal = Ordinal(week);
        return date.DayOfWeek == DayOfWeek.Sunday
            ? $"{ordinal} Sunday {preposition} {season}"
            : $"{date.DayOfWeek} of the {ordinal} Week {preposition} {season}";
    }
}
