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
    /// <summary>
    /// The ferial description of the day when there is no principal <see cref="Celebration"/>, e.g. "Monday of the Third Week of Lent" or "Tuesday of the Twenty-first Week in Ordinary Time".
    /// Null whenever <see cref="Celebration"/> is set.
    /// </summary>
    public string? FerialName { get; init; }

    /// <summary>
    /// Optional memorials available on this day that are not the principal celebration (empty on most days).
    /// These are offered to the celebrant but may be omitted; they are suppressed entirely on Sundays, solemnities, feasts, and privileged weekdays.
    /// </summary>
    public IReadOnlyList<LiturgicalCelebration> OptionalMemorials { get; init; } = [];
}

/// <summary>
/// A pure liturgical-calendar engine for the Ordinary Form (modern Roman general calendar).
/// It is deterministic: the caller supplies the date, so there is no hidden clock.
/// </summary>
public interface ILiturgicalCalendar
{
    /// <summary>Returns the season, color, and principal celebration (if any) for the given date.</summary>
    LiturgicalDay ForDate(DateOnly date);
}
