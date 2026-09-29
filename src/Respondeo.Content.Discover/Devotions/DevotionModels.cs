namespace Respondeo.Content.Discover.Devotions;

// ---------------------------------------------------------------------------
// Devotions (data-driven prayer sequences: Rosary, chaplets, litanies, ...)
// ---------------------------------------------------------------------------

/// <summary>A single devotion as it appears in the browse index.</summary>
public sealed class DevotionSummary
{
    /// <summary>Stable id / URL slug (e.g. "holy-rosary").</summary>
    public required string Id { get; init; }

    /// <summary>Display title (e.g. "The Holy Rosary").</summary>
    public required string Title { get; init; }

    /// <summary>Short one-line description for cards and previews.</summary>
    public string Summary { get; init; } = string.Empty;

    /// <summary>The kind slug (e.g. "rosary", "chaplet", "litany") used for grouping and iconography.</summary>
    public string Kind { get; init; } = "devotion";
}

/// <summary>
/// A complete, data-driven devotion: an ordered sequence of steps referencing prayers by id, plus optional mystery sets.
/// The interactive player walks the sequence; a "mysteries" step is expanded once per mystery of the chosen set,
/// so a new devotion (a chaplet, a different rosary form) is added purely as data with no code change.
/// </summary>
public sealed class Devotion
{
    /// <summary>Stable id / URL slug (matches <see cref="DevotionSummary.Id"/>).</summary>
    public required string Id { get; init; }

    /// <summary>Display title.</summary>
    public required string Title { get; init; }

    /// <summary>Short one-line description.</summary>
    public string Summary { get; init; } = string.Empty;

    /// <summary>The kind slug (e.g. "rosary", "chaplet").</summary>
    public string Kind { get; init; } = "devotion";

    /// <summary>Optional rendered HTML introduction shown before the reader begins.</summary>
    public string? IntroHtml { get; init; }

    /// <summary>
    /// The selectable mystery / meditation sets (e.g. the Joyful, Sorrowful, Glorious, Luminous mysteries). Empty for devotions that have no per-mystery meditation.
    /// </summary>
    public IReadOnlyList<MysterySet> MysterySets { get; init; } = [];

    /// <summary>The ordered steps of the devotion.</summary>
    public required IReadOnlyList<DevotionStep> Sequence { get; init; }
}

/// <summary>A selectable group of meditations (e.g. "The Joyful Mysteries").</summary>
public sealed class MysterySet
{
    /// <summary>Stable id / slug for the set (e.g. "joyful").</summary>
    public required string Id { get; init; }

    /// <summary>Display name (e.g. "The Joyful Mysteries").</summary>
    public required string Name { get; init; }

    /// <summary>Optional note on when the set is prayed (e.g. "Mondays and Saturdays").</summary>
    public string? When { get; init; }

    /// <summary>The ordered meditations of the set.</summary>
    public required IReadOnlyList<Mystery> Mysteries { get; init; }
}

/// <summary>A single meditation within a <see cref="MysterySet"/>.</summary>
public sealed class Mystery
{
    /// <summary>Display title (e.g. "The Annunciation").</summary>
    public required string Title { get; init; }

    /// <summary>Optional rendered HTML reflection shown while the mystery is prayed.</summary>
    public string? ReflectionHtml { get; init; }
}

/// <summary>
/// One step of a devotion. A <see cref="Kind"/> of "prayer" prays <see cref="PrayerId"/> <see cref="Repeat"/> times;
/// a kind of "mysteries" iterates the chosen <see cref="MysterySet"/>, running <see cref="PerMystery"/> once for each mystery (so the decade template lives in data).
/// </summary>
public sealed class DevotionStep
{
    /// <summary>The step kind: "prayer" (default) or "mysteries".</summary>
    public string Kind { get; init; } = "prayer";

    /// <summary>Optional label / heading for the step (e.g. "Begin", "Closing prayers").</summary>
    public string? Title { get; init; }

    /// <summary>The prayer to pray, referenced by id, for a "prayer" step.</summary>
    public string? PrayerId { get; init; }

    /// <summary>How many times the prayer is repeated (e.g. 10 Hail Marys). Defaults to 1.</summary>
    public int Repeat { get; init; } = 1;

    /// <summary>
    /// Optional visual bead type for the rosary "thread" beside the step: "cross" (Sign of the Cross),
    /// "large" (a large / Our Father bead), "small" (a small / Hail Mary bead), or "between" (prayed
    /// between beads, drawn as three small pips). Null renders no marker.
    /// </summary>
    public string? Bead { get; init; }

    /// <summary>
    /// The per-mystery sub-steps for a "mysteries" step (e.g. one Our Father, ten Hail Marys, one Glory Be).
    /// Run once for each mystery of the selected set.
    /// </summary>
    public IReadOnlyList<DevotionStep> PerMystery { get; init; } = [];
}
