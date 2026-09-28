namespace Respondeo.Content.Credo;

// ---------------------------------------------------------------------------
// Prayers
// ---------------------------------------------------------------------------

/// <summary>
/// A single prayer as it appears in the browse index: enough to render a card without fetching the full text.
/// Only traditional / public-domain prayers are catalogued; each carries an optional attribution so the source can be shown where one is required.
/// </summary>
public sealed class PrayerSummary
{
    /// <summary>Stable id / URL slug for the prayer (e.g. "hail-mary").</summary>
    public required string Id { get; init; }

    /// <summary>Display title (e.g. "Hail Mary").</summary>
    public required string Title { get; init; }

    /// <summary>Short one-line description for cards and previews.</summary>
    public string Summary { get; init; } = string.Empty;

    /// <summary>The category slug the prayer belongs to (e.g. "marian", "daily", "mass").</summary>
    public string Category { get; init; } = "other";

    /// <summary>Free-text tags for extra searchable categorization.</summary>
    public IReadOnlyList<string> Tags { get; init; } = [];
}

/// <summary>
/// The full text of a single prayer, rendered as HTML, fetched on demand when a reader opens it.
/// </summary>
public sealed class Prayer
{
    /// <summary>Stable id / URL slug (matches <see cref="PrayerSummary.Id"/>).</summary>
    public required string Id { get; init; }

    /// <summary>Display title.</summary>
    public required string Title { get; init; }

    /// <summary>Short one-line description.</summary>
    public string Summary { get; init; } = string.Empty;

    /// <summary>The category slug the prayer belongs to.</summary>
    public string Category { get; init; } = "other";

    /// <summary>The language of this prayer's text (e.g. "en", "la"). Defaults to "en".</summary>
    public string Language { get; init; } = "en";

    /// <summary>
    /// Optional key shared with the prayer's translations.
    /// Files with the same key are the same prayer in different languages; the service uses it to pair an English prayer with its Latin.
    /// </summary>
    public string? TranslationKey { get; init; }

    /// <summary>The rendered HTML of the prayer text.</summary>
    public required string Html { get; init; }

    /// <summary>
    /// Optional Latin (or original-language) rendered HTML, shown alongside the vernacular.
    /// Populated by the service from the linked Latin file that shares this prayer's <see cref="TranslationKey"/>.
    /// </summary>
    public string? LatinHtml { get; set; }

    /// <summary>Free-text tags.</summary>
    public IReadOnlyList<string> Tags { get; init; } = [];

    /// <summary>Optional attribution / source note (e.g. a translation credit) for copyright care.</summary>
    public string? Attribution { get; init; }
}

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
    /// The per-mystery sub-steps for a "mysteries" step (e.g. one Our Father, ten Hail Marys, one Glory Be).
    /// Run once for each mystery of the selected set.
    /// </summary>
    public IReadOnlyList<DevotionStep> PerMystery { get; init; } = [];
}

// ---------------------------------------------------------------------------
// Articles (deeper dives: Confession, the Eucharist, Catholic stances, ...)
// ---------------------------------------------------------------------------

/// <summary>An article as it appears in the browse index.</summary>
public sealed class ArticleSummary
{
    /// <summary>Stable id / URL slug (e.g. "confession").</summary>
    public required string Id { get; init; }

    /// <summary>Display title (e.g. "Confession: Why We Confess to a Priest").</summary>
    public required string Title { get; init; }

    /// <summary>Short one-line description for cards and previews.</summary>
    public string Summary { get; init; } = string.Empty;

    /// <summary>The topic slug the article belongs to (e.g. "sacraments", "practice", "apologetics").</summary>
    public string Topic { get; init; } = "general";

    /// <summary>Free-text tags for extra searchable categorization.</summary>
    public IReadOnlyList<string> Tags { get; init; } = [];
}

/// <summary>The full content of a single article, fetched on demand.</summary>
public sealed class Article
{
    /// <summary>Stable id / URL slug (matches <see cref="ArticleSummary.Id"/>).</summary>
    public required string Id { get; init; }

    /// <summary>Display title.</summary>
    public required string Title { get; init; }

    /// <summary>Short one-line description.</summary>
    public string Summary { get; init; } = string.Empty;

    /// <summary>The topic slug the article belongs to.</summary>
    public string Topic { get; init; } = "general";

    /// <summary>Free-text tags.</summary>
    public IReadOnlyList<string> Tags { get; init; } = [];

    /// <summary>The prose body, split into titled sections (rendered HTML), in author order.</summary>
    public IReadOnlyList<ArticleSection> Sections { get; init; } = [];

    /// <summary>Citations and further-reading references.</summary>
    public IReadOnlyList<ArticleSource> Sources { get; init; } = [];
}

/// <summary>A titled prose section of an article (rendered HTML under an author-supplied heading).</summary>
public sealed class ArticleSection
{
    /// <summary>The section heading (empty for the untitled lead-in).</summary>
    public required string Heading { get; init; }

    /// <summary>The rendered HTML of the section body.</summary>
    public required string Html { get; init; }
}

/// <summary>A citation or further-reading reference for an article.</summary>
public sealed class ArticleSource
{
    /// <summary>The display label of the source.</summary>
    public required string Label { get; init; }

    /// <summary>Optional URL of the source.</summary>
    public string? Url { get; init; }
}

// ---------------------------------------------------------------------------
// Index
// ---------------------------------------------------------------------------

/// <summary>
/// The lightweight browse index for the whole Credo pillar: prayer, devotion, and article summaries,
/// loaded once so each section can be listed and searched without fetching full bodies.
/// </summary>
public sealed class CredoIndex
{
    /// <summary>Every catalogued prayer, in load order.</summary>
    public IReadOnlyList<PrayerSummary> Prayers { get; init; } = [];

    /// <summary>Every catalogued devotion, in load order.</summary>
    public IReadOnlyList<DevotionSummary> Devotions { get; init; } = [];

    /// <summary>Every catalogued article, in load order.</summary>
    public IReadOnlyList<ArticleSummary> Articles { get; init; } = [];
}
