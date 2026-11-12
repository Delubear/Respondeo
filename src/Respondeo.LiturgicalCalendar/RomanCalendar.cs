namespace Respondeo.LiturgicalCalendar;

/// <summary>
/// A pure liturgical-calendar engine for the Ordinary Form.
/// Everything is derived from two anchors:
/// the fixed civil dates (Christmas is always 25 December) and Gregorian Easter (from which every moveable feast is an offset).
/// No per-day data is stored; the whole year is computed on demand.
/// </summary>
internal sealed class RomanCalendar : ILiturgicalCalendar
{
    public RomanCalendar() : this(LiturgicalCalendarOptions.Default) { }

    public RomanCalendar(LiturgicalCalendarOptions options)
    {
        Options = options;
        Profile = CalendarProfile.From(options);
        CalendarCatalog.EnsureValid(Profile.CatalogRegion);
    }

    public LiturgicalCalendarOptions Options { get; }

    private CalendarProfile Profile { get; }

    public LiturgicalDay ForDate(DateOnly date)
    {
        var season = SeasonFor(date);
        var candidateGeneration = NamedCelebrations(date);
        var resolution = LiturgicalOccurrenceResolver.Resolve(date, season, candidateGeneration.Candidates);
        var color = resolution.Principal?.Color ?? SeasonColor(season, date);
        var ferialName = resolution.Principal is null ? FerialName(date, season) : null;
        var transfers = TransfersFor(date);

        return new LiturgicalDay(date, season, color, resolution.Principal)
        {
            FerialName = ferialName,
            OptionalMemorials = resolution.OptionalMemorials,
            MemorialCommemorations = resolution.MemorialCommemorations,
            Occurrence = resolution.Decision,
            RankAdjustments = candidateGeneration.RankAdjustments,
            Transfers = transfers,
            Obligation = LiturgicalObligationResolver.Resolve(date, Profile, transfers),
        };
    }

    private sealed record CandidateGeneration(
        IReadOnlyList<CelebrationCandidate> Candidates,
        IReadOnlyList<LiturgicalCelebrationRankAdjustment> RankAdjustments);

    /// <summary>All named celebrations claiming the day, plus any candidate-generation adjustments.</summary>
    private CandidateGeneration NamedCelebrations(DateOnly date, string? excludedTransferKey = null)
    {
        var candidates = new List<CelebrationCandidate>();
        var rankAdjustments = new List<LiturgicalCelebrationRankAdjustment>();

        if (MoveableCelebration(date) is { } moveable && !IsTransferredAway(date, moveable, excludedTransferKey))
        {
            candidates.Add(new CelebrationCandidate(moveable));
        }

        foreach (var lordFeast in SundayLordCelebrations(date))
        {
            candidates.Add(new CelebrationCandidate(lordFeast));
        }

        foreach (var fixedCelebration in GeneralRomanCalendarData.ForMonthDay(date.Month, date.Day, Profile.CatalogRegion))
        {
            // Sunday Epiphany is emitted separately below.
            if (Profile.EpiphanyOnSunday && date is { Month: 1, Day: 6 } && fixedCelebration.Rank == CelebrationRank.Solemnity)
            {
                continue;
            }

            // Suppress only the impeded solemnity, retaining any other candidates on its nominal date.
            if (IsTransferredAway(date, fixedCelebration, excludedTransferKey))
            {
                continue;
            }

            // In the United States, when 22 January falls on a Sunday the Day of Prayer is observed on Monday, 23 January.
            if (IsDayOfPrayerForUnborn(fixedCelebration) && date.DayOfWeek == DayOfWeek.Sunday)
            {
                continue;
            }

            candidates.Add(new CelebrationCandidate(fixedCelebration));
        }

        if (Profile.TransfersDayOfPrayerFromSunday && date is { Month: 1, Day: 23 }
            && new DateOnly(date.Year, 1, 22).DayOfWeek == DayOfWeek.Sunday)
        {
            foreach (var dayOfPrayer in GeneralRomanCalendarData.ForMonthDay(1, 22, Profile.CatalogRegion).Where(IsDayOfPrayerForUnborn))
            {
                candidates.Add(new CelebrationCandidate(dayOfPrayer));
            }
        }

        foreach (var transferred in TransferredSolemnities(date, excludedTransferKey))
        {
            candidates.Add(new CelebrationCandidate(transferred));
        }

        // When the Immaculate Heart coincides with another obligatory memorial, both become optional memorials.
        var memorialCount = candidates.Count(c => c.Celebration.Rank == CelebrationRank.Memorial);
        if (memorialCount > 1 && candidates.Any(c => c.Celebration.CalendarKey == "immaculate-heart"))
        {
            for (var i = 0; i < candidates.Count; i++)
            {
                if (candidates[i].Celebration.Rank == CelebrationRank.Memorial)
                {
                    var original = candidates[i].Celebration;
                    var adjusted = original with { Rank = CelebrationRank.OptionalMemorial, Precedence = null };
                    rankAdjustments.Add(new(original, original.Rank, adjusted.Rank,
                        LiturgicalRankAdjustmentRule.ImmaculateHeartMemorialCollision));
                    candidates[i] = candidates[i] with { Celebration = adjusted };
                }
            }
        }

        return new(candidates, rankAdjustments);
    }

    private bool IsDayOfPrayerForUnborn(LiturgicalCelebration celebration) =>
        Profile.TransfersDayOfPrayerFromSunday && celebration.CalendarKey == "day-of-prayer-for-unborn-children";

    // -----------------------------------------------------------------------
    // Solemnity transfers:
    // Pair-specific collision policies are explicit; unrelated equal-class collisions remain unresolved.
    // -----------------------------------------------------------------------

    /// <summary>True only for the identified solemnity on its impeded nominal date.</summary>
    private bool IsTransferredAway(DateOnly date, LiturgicalCelebration celebration, string? excludedTransferKey) =>
        CalendarCatalog.TransferableSolemnities.Any(item => item.Key == celebration.CalendarKey && item.Key != excludedTransferKey
            && item.Month == date.Month && item.Day == date.Day
            && TransferResolution(date.Year, item.Month, item.Day, item.Key) is not null);

    /// <summary>Transfers retain the region's complete authored celebration instead of reconstructing it from a name and color.</summary>
    private IEnumerable<LiturgicalCelebration> TransferredSolemnities(DateOnly date, string? excludedTransferKey)
    {
        foreach (var item in CalendarCatalog.TransferableSolemnities)
        {
            if (item.Key != excludedTransferKey
                && TransferResolution(date.Year, item.Month, item.Day, item.Key) is { } transfer && transfer.Target == date)
            {
                if (item.Key == "sacred-heart")
                {
                    yield return ComputedCelebrationData.Get(item.Key);
                    continue;
                }

                yield return GeneralRomanCalendarData.ForMonthDay(item.Month, item.Day, Profile.CatalogRegion)
                    .Single(celebration => celebration.CalendarKey == item.Key);
            }
        }
    }

    private IReadOnlyList<LiturgicalTransfer> TransfersFor(DateOnly date)
    {
        var transfers = new List<LiturgicalTransfer>();
        foreach (var item in CalendarCatalog.TransferableSolemnities)
        {
            var nominal = new DateOnly(date.Year, item.Month, item.Day);
            if (TransferResolution(date.Year, item.Month, item.Day, item.Key) is not { } resolution)
            {
                continue;
            }

            if (date == nominal || date == resolution.Target)
            {
                transfers.Add(new LiturgicalTransfer(
                    item.Key, nominal, resolution.Target, resolution.Rule, resolution.SkippedDateRange));
            }
        }

        return transfers;
    }

    private LiturgicalTransferResolution? TransferResolution(int year, int month, int day, string celebrationKey)
    {
        var original = new DateOnly(year, month, day);
        return LiturgicalTransferResolver.Resolve(
            original,
            LiturgicalDates.Easter(year),
            SeasonFor(original),
            celebrationKey,
            Options.SaintJohnTheBaptistIsPatron,
            IsAvailableForTransferredSolemnity);
    }

    private bool IsAvailableForTransferredSolemnity(DateOnly date)
    {
        var season = SeasonFor(date);
        var candidates = NamedCelebrations(date, "saints-peter-and-paul").Candidates;
        return LiturgicalPrecedence.CanReceiveTransferredSolemnity(date, season, candidates);
    }

    /// <summary>Moveable dates remain explicit domain rules; their celebration facts come from the authored catalog.</summary>
    private LiturgicalCelebration? MoveableCelebration(DateOnly date)
    {
        var easter = LiturgicalDates.Easter(date.Year);
        var offset = date.DayNumber - easter.DayNumber;

        return offset switch
        {
            -46 => ComputedCelebrationData.Get("ash-wednesday"),
            -7 => ComputedCelebrationData.Get("palm-sunday"),
            -3 => ComputedCelebrationData.Get("holy-thursday"),
            -2 => ComputedCelebrationData.Get("good-friday"),
            -1 => ComputedCelebrationData.Get("holy-saturday"),
            0 => ComputedCelebrationData.Get("easter-sunday"),
            _ when offset == Profile.AscensionOffset => ComputedCelebrationData.Get("ascension"),
            49 => ComputedCelebrationData.Get("pentecost"),
            56 => ComputedCelebrationData.Get("trinity"),
            _ when offset == Profile.CorpusChristiOffset => ComputedCelebrationData.Get("corpus-christi"),
            50 => ComputedCelebrationData.Get("mother-of-the-church"),
            68 => ComputedCelebrationData.Get("sacred-heart"),
            69 => ComputedCelebrationData.Get("immaculate-heart"),
            _ when Profile.IncludesThanksgiving && IsThanksgiving(date) => ComputedCelebrationData.Get("thanksgiving"),
            _ => null,
        };
    }

    /// <summary>US Thanksgiving Day: the fourth Thursday of November.</summary>
    private static bool IsThanksgiving(DateOnly date) =>
        date is { Month: 11, DayOfWeek: DayOfWeek.Thursday, Day: >= 22 and <= 28 };

    /// <summary>Feasts of the Lord anchored to a Sunday (or the Christmas octave): Holy Family, the Baptism of the Lord, and Christ the King.</summary>
    private IEnumerable<LiturgicalCelebration> SundayLordCelebrations(DateOnly date)
    {
        var year = date.Year;

        if (date == HolyFamily(year))
        {
            yield return ComputedCelebrationData.Get("holy-family");
        }

        if (Profile.EpiphanyOnSunday && date == Epiphany(year))
        {
            yield return ComputedCelebrationData.Get("epiphany");
        }

        if (date == BaptismOfTheLord(year))
        {
            yield return ComputedCelebrationData.Get("baptism-of-the-lord");
        }

        if (date == AdventStart(year).AddDays(-7))
        {
            yield return ComputedCelebrationData.Get("christ-the-king");
        }
    }

    /// <summary>The Holy Family: the Sunday within the Octave of Christmas, or 30 December when Christmas itself is a Sunday.</summary>
    private DateOnly HolyFamily(int year)
    {
        var christmas = new DateOnly(year, 12, 25);
        if (christmas.DayOfWeek == DayOfWeek.Sunday)
        {
            return new DateOnly(year, 12, 30);
        }

        var daysUntilSunday = (7 - (int)christmas.DayOfWeek) % 7;
        return christmas.AddDays(daysUntilSunday == 0 ? 7 : daysUntilSunday);
    }

    // -----------------------------------------------------------------------
    // Seasons, derived from Easter and the fixed Christmas / Epiphany anchors.
    // -----------------------------------------------------------------------

    private LiturgicalSeason SeasonFor(DateOnly date)
    {
        var year = date.Year;
        var easter = LiturgicalDates.Easter(year);
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
    private static DateOnly AdventStart(int year) => LiturgicalDates.FirstSundayOfAdvent(year);

    /// <summary>
    /// The Baptism of the Lord, which closes Christmas Time: the Sunday after Epiphany (6 January).
    /// </summary>
    private DateOnly BaptismOfTheLord(int year) => LiturgicalDates.BaptismOfTheLord(year, Profile.EpiphanyOnSunday);

    /// <summary>The Epiphany: 6 January, or in the United States the Sunday between 2 and 8 January.</summary>
    private DateOnly Epiphany(int year) => LiturgicalDates.Epiphany(year, Profile.EpiphanyOnSunday);

    private LiturgicalColor SeasonColor(LiturgicalSeason season, DateOnly date) => season switch
    {
        LiturgicalSeason.Advent => IsGaudete(date) ? LiturgicalColor.Rose : LiturgicalColor.Violet,
        LiturgicalSeason.Lent => IsLaetare(date) ? LiturgicalColor.Rose : LiturgicalColor.Violet,
        LiturgicalSeason.Christmas => LiturgicalColor.White,
        LiturgicalSeason.Triduum => LiturgicalColor.White,
        LiturgicalSeason.Easter => LiturgicalColor.White,
        _ => LiturgicalColor.Green,
    };

    /// <summary>Gaudete Sunday: the Third Sunday of Advent (rose vestments).</summary>
    private bool IsGaudete(DateOnly date) => date.DayOfWeek == DayOfWeek.Sunday && date == AdventStart(date.Year).AddDays(14);

    /// <summary>Laetare Sunday: the Fourth Sunday of Lent (rose vestments).</summary>
    private bool IsLaetare(DateOnly date)
    {
        var easter = LiturgicalDates.Easter(date.Year);

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

    private string Ordinal(int n) => n >= 1 && n <= Ordinals.Length ? Ordinals[n - 1] : n.ToString();

    private string FerialName(DateOnly date, LiturgicalSeason season) => season switch
    {
        LiturgicalSeason.Advent => AdventFerialName(date),
        LiturgicalSeason.Christmas => ChristmasFerialName(date),
        LiturgicalSeason.Lent => LentFerialName(date),
        LiturgicalSeason.Triduum => $"{date.DayOfWeek} of the Paschal Triduum",
        LiturgicalSeason.Easter => EasterFerialName(date),
        _ => SeasonDayName(date, OrdinaryTimeWeek(date), "Ordinary Time", "in"),
    };

    /// <summary>"Third Sunday of Advent" or "Monday of the Third Week of Advent" etc.</summary>
    private string AdventFerialName(DateOnly date)
    {
        var week = ((date.DayNumber - AdventStart(date.Year).DayNumber) / 7) + 1;
        return SeasonDayName(date, week, "Advent", "of");
    }

    private string ChristmasFerialName(DateOnly date)
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

    private string LentFerialName(DateOnly date)
    {
        var easter = LiturgicalDates.Easter(date.Year);
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

    private string EasterFerialName(DateOnly date)
    {
        var easter = LiturgicalDates.Easter(date.Year);

        // The weekdays within the Octave of Easter (the Sunday a week later begins the Second Week).
        if (date > easter && date < easter.AddDays(7))
        {
            return $"{date.DayOfWeek} within the Octave of Easter";
        }

        var week = ((date.DayNumber - easter.DayNumber) / 7) + 1;
        return SeasonDayName(date, week, "Easter", "of");
    }

    /// <summary>The week of Ordinary Time, numbered 1-34 across the two spans before Lent and after Pentecost.</summary>
    private int OrdinaryTimeWeek(DateOnly date) => LiturgicalWeekResolver.OrdinaryTimeWeek(date, Options);

    private string SeasonDayName(DateOnly date, int week, string season, string preposition)
    {
        var ordinal = Ordinal(week);
        return date.DayOfWeek == DayOfWeek.Sunday
            ? $"{ordinal} Sunday {preposition} {season}"
            : $"{date.DayOfWeek} of the {ordinal} Week {preposition} {season}";
    }
}
