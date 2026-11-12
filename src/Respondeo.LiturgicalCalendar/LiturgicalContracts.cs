namespace Respondeo.LiturgicalCalendar;

// ---------------------------------------------------------------------------
// Liturgical calendar. Public contracts returned by ILiturgicalCalendar.
// The computation lives internally in the engine (RomanCalendar).
// ---------------------------------------------------------------------------

/// <summary>The seasons of the Ordinary Form liturgical year.</summary>
public enum LiturgicalSeason
{
    /// <summary>Advent: the weeks of preparation leading to Christmas.</summary>
    Advent,

    /// <summary>Christmas Time: from the Nativity through the Baptism of the Lord.</summary>
    Christmas,

    /// <summary>Lent: from Ash Wednesday to the evening of Holy Thursday.</summary>
    Lent,

    /// <summary>The Sacred Paschal Triduum: Holy Thursday evening through Easter Sunday.</summary>
    Triduum,

    /// <summary>Easter Time: from Easter Sunday through Pentecost.</summary>
    Easter,

    /// <summary>Ordinary Time: the weeks outside the festal seasons.</summary>
    OrdinaryTime,
}

/// <summary>The liturgical color customarily worn for a day or season.</summary>
public enum LiturgicalColor
{
    /// <summary>Green: Ordinary Time.</summary>
    Green,

    /// <summary>Violet: Advent and Lent.</summary>
    Violet,

    /// <summary>White: Christmas, Easter, and feasts of the Lord, Mary, and non-martyr saints.</summary>
    White,

    /// <summary>Red: Palm Sunday, Good Friday, Pentecost, and feasts of martyrs.</summary>
    Red,

    /// <summary>Rose: Gaudete (3rd Advent) and Laetare (4th Lent) Sundays.</summary>
    Rose,
}

/// <summary>The liturgical rank of a named celebration, highest to lowest.</summary>
public enum CelebrationRank
{
    /// <summary>A solemnity: the highest-ranking celebrations (e.g. Christmas, Easter, Pentecost).</summary>
    Solemnity,

    /// <summary>A feast (e.g. the Baptism of the Lord, the Transfiguration).</summary>
    Feast,

    /// <summary>An obligatory memorial, which is always observed when it is not outranked.</summary>
    Memorial,

    /// <summary>An optional memorial, which may be observed or omitted at the celebrant's choice.</summary>
    OptionalMemorial,
}

/// <summary>A named celebration falling on a given day (fixed or moveable).</summary>
/// <param name="Name">The display name, e.g. "The Nativity of the Lord".</param>
/// <param name="Rank">The liturgical rank.</param>
/// <param name="Color">The liturgical color proper to the celebration.</param>
public sealed record LiturgicalCelebration(string Name, CelebrationRank Rank, LiturgicalColor Color)
{
    /// <summary>
    /// Optional slug that links this celebration to a saint profile (<c>discover/saints/{Id}</c>) when the day honors a saint in the catalog.
    /// Null for celebrations with no linked saint (e.g. feasts of the Lord).
    /// </summary>
    public string? Id { get; init; }

    /// <summary>
    /// Optional one-line description of the celebration,
    /// authored in the calendar dataset for days that have no linked saint profile to borrow a summary from (e.g. feasts of the Lord, Marian days, mysteries).
    /// Null when the day relies on a linked saint profile for its summary, or carries no summary at all.
    /// </summary>
    public string? Summary { get; init; }

    /// <summary>Authored stable calendar identity, independent of the display name and saint-profile Id; null for not-yet-migrated entries.</summary>
    public string? CalendarKey { get; init; }

    /// <summary>Explicit precedence-table category where rank alone is insufficient; null uses the general-calendar category for Rank.</summary>
    public LiturgicalPrecedenceClass? Precedence { get; init; }
}

/// <summary>A candidate-generation rank change made by a source-backed liturgical collision rule.</summary>
public enum LiturgicalRankAdjustmentRule
{
    /// <summary>The Immaculate Heart and coinciding obligatory memorials become optional memorials (USCCB 2027 note 6c).</summary>
    ImmaculateHeartMemorialCollision,
}

/// <summary>The original celebration and rank, adjusted rank, and rule responsible for a candidate-generation change.</summary>
public sealed record LiturgicalCelebrationRankAdjustment(
    LiturgicalCelebration Celebration,
    CelebrationRank OriginalRank,
    CelebrationRank AdjustedRank,
    LiturgicalRankAdjustmentRule Rule)
{
    /// <summary>A stable identifier within the CAL-02 occurrence rule family.</summary>
    public string RuleId => Rule switch
    {
        LiturgicalRankAdjustmentRule.ImmaculateHeartMemorialCollision => "CAL-02/immaculate-heart-memorial-collision",
        _ => throw new InvalidOperationException($"Unknown rank adjustment rule: {Rule}."),
    };
}

/// <summary>
/// What the Church keeps on a particular day: the season it falls in, the color of the day, the principal named celebration if there is one, and any optional memorials available that day.
/// </summary>
/// <param name="Date">The date this describes.</param>
/// <param name="Season">The liturgical season the date falls in.</param>
/// <param name="Color">The color of the day (the celebration's color when present, else the season's).</param>
/// <param name="Celebration">The principal named celebration on this day, or <c>null</c> for a ferial day.</param>
public sealed record LiturgicalDay(DateOnly Date, LiturgicalSeason Season, LiturgicalColor Color, LiturgicalCelebration? Celebration)
{
    /// <summary>The day's ordinal within the eight-day Easter octave (1-8), or null (Universal Norms 24).</summary>
    public int? EasterOctaveDay
    {
        get
        {
            var offset = Date.DayNumber - LiturgicalDates.Easter(Date.Year).DayNumber;
            return offset is >= 0 and <= 7 ? offset + 1 : null;
        }
    }

    /// <summary>Whether this is Monday through Thursday of Holy Week (Universal Norms 16a), independent of the legacy celebration rank.</summary>
    public bool IsHolyWeekWeekday => Date.DayNumber - LiturgicalDates.Easter(Date.Year).DayNumber is >= -6 and <= -3;

    /// <summary>The season for Lauds: Holy Thursday remains Lenten; Easter Sunday is Easter Time despite the overlapping Triduum (Universal Norms 19, 22, 28).</summary>
    public LiturgicalSeason MorningPrayerSeason => (Date.DayNumber - LiturgicalDates.Easter(Date.Year).DayNumber) switch
    {
        -3 => LiturgicalSeason.Lent,
        0 => LiturgicalSeason.Easter,
        _ => Season,
    };

    /// <summary>
    /// The ferial description of the day when there is no principal <see cref="Celebration"/>, e.g. "Monday of the Third Week of Lent" or "Tuesday of the Twenty-first Week in Ordinary Time".
    /// Null whenever <see cref="Celebration"/> is set.
    /// </summary>
    public string? FerialName { get; init; }

    /// <summary>Memorials available as full Office choices; never contains restricted privileged-weekday commemorations.</summary>
    public IReadOnlyList<LiturgicalCelebration> OptionalMemorials { get; init; } = [];

    /// <summary>Memorials that may be commemorated in the limited form allowed on privileged weekdays (GILH 238-239).</summary>
    public IReadOnlyList<LiturgicalCelebration> MemorialCommemorations { get; init; } = [];

    /// <summary>The decisive occurrence rule and candidates suppressed at that stage; null for manually supplied days without diagnostics.</summary>
    public LiturgicalOccurrence? Occurrence { get; init; }

    /// <summary>Candidate-generation rank changes, including their original celebration and source-backed rule.</summary>
    public IReadOnlyList<LiturgicalCelebrationRankAdjustment> RankAdjustments { get; init; } = [];

    /// <summary>Fixed solemnity transfers touching this date, including nominal dates and observed dates.</summary>
    public IReadOnlyList<LiturgicalTransfer> Transfers { get; init; } = [];

    /// <summary>Mass-obligation status under the calendar profile; local diocesan dispensations are not modeled.</summary>
    public LiturgicalObligation Obligation { get; init; } = LiturgicalObligation.Unknown;

    /// <summary>The precedence-table category of the resolved observance, combining the temporal day and principal celebration.</summary>
    public LiturgicalPrecedenceClass Precedence => LiturgicalPrecedence.ForDay(this);

    public bool Equals(LiturgicalDay? other) => other is not null
        && Date == other.Date
        && Season == other.Season
        && Color == other.Color
        && EqualityComparer<LiturgicalObligation>.Default.Equals(Obligation, other.Obligation)
        && EqualityComparer<LiturgicalCelebration?>.Default.Equals(Celebration, other.Celebration)
        && FerialName == other.FerialName
        && OptionalMemorials.SequenceEqual(other.OptionalMemorials)
        && MemorialCommemorations.SequenceEqual(other.MemorialCommemorations)
        && EqualityComparer<LiturgicalOccurrence?>.Default.Equals(Occurrence, other.Occurrence)
        && RankAdjustments.SequenceEqual(other.RankAdjustments)
        && Transfers.SequenceEqual(other.Transfers);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Date);
        hash.Add(Season);
        hash.Add(Color);
        hash.Add(Obligation);
        hash.Add(Celebration);
        hash.Add(FerialName);
        foreach (var memorial in OptionalMemorials)
        {
            hash.Add(memorial);
        }
        foreach (var commemoration in MemorialCommemorations)
        {
            hash.Add(commemoration);
        }
        hash.Add(Occurrence);
        foreach (var adjustment in RankAdjustments)
        {
            hash.Add(adjustment);
        }
        foreach (var transfer in Transfers)
        {
            hash.Add(transfer);
        }
        return hash.ToHashCode();
    }
}

/// <summary>
/// A pure liturgical-calendar engine for the Ordinary Form (modern Roman general calendar).
/// It is deterministic: the caller supplies the date, so there is no hidden clock.
/// </summary>
public interface ILiturgicalCalendar
{
    /// <summary>Returns the season, color, and principal celebration (if any) for the given date.</summary>
    LiturgicalDay ForDate(DateOnly date);

    /// <summary>Returns the date's background temporal and Psalter weeks; proper psalmody and weekly Compline forms are separate.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The date is DateOnly.MinValue, whose prior-year Christmas cannot be represented.</exception>
    LiturgicalWeek ForWeek(DateOnly date)
    {
        if (date == DateOnly.MinValue)
        {
            throw new ArgumentOutOfRangeException(nameof(date), date,
                "The calendar week requires the preceding Christmas date, which is outside the DateOnly range.");
        }

        return LiturgicalWeekResolver.Resolve(ForDate(date), Options);
    }

    /// <summary>Resolves Sunday/solemnity evening concurrence for Vespers and Compline, preserving the Triduum's proper Office.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The date is DateOnly.MaxValue, which has no following civil date.</exception>
    LiturgicalEvening ForEvening(DateOnly date)
    {
        if (date == DateOnly.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(date), date,
                "Evening context requires the following civil date, which is outside the DateOnly range.");
        }

        return LiturgicalEveningResolver.Resolve(ForDate(date), ForDate(date.AddDays(1)), Options);
    }

    /// <summary>The options this calendar was built with.</summary>
    LiturgicalCalendarOptions Options => LiturgicalCalendarOptions.Default;
}
