namespace Respondeo.Content.Summa;

/// <summary>
/// The lightweight browse/search index for the whole Summa Theologica:
/// every part, question, and article title with its stable id, but without the heavy article bodies.
/// Loaded once so the hierarchy can be browsed and titles/question text can be searched with a single fetch.
/// </summary>
public sealed class SummaIndex
{
    /// <summary>The five parts in reading order (First Part, First Part of the Second Part, etc.).</summary>
    public IReadOnlyList<SummaPartEntry> Parts { get; init; } = [];
}

/// <summary>A part of the Summa (e.g. "First Part") in the browse index.</summary>
public sealed class SummaPartEntry
{
    /// <summary>Stable storage key for the part (e.g. "p1", "p2a", "p2b", "p3", "sup").</summary>
    public required string Id { get; init; }

    /// <summary>Display title (e.g. "First Part").</summary>
    public required string Title { get; init; }

    /// <summary>The questions belonging to this part, in order.</summary>
    public IReadOnlyList<SummaQuestionEntry> Questions { get; init; } = [];
}

/// <summary>A question within a part in the browse index.</summary>
public sealed class SummaQuestionEntry
{
    /// <summary>Stable storage id for the question (e.g. "p1-q001").</summary>
    public required string Id { get; init; }

    /// <summary>The question number within its part (1-based).</summary>
    public required int Number { get; init; }

    /// <summary>Display title of the question (e.g. "The nature and extent of sacred doctrine").</summary>
    public required string Title { get; init; }

    /// <summary>
    /// The treatise this question belongs to (e.g. "Treatise on the Passions"), used to group
    /// questions within a part. Null for the few questions that precede any treatise heading.
    /// </summary>
    public string? Treatise { get; init; }

    /// <summary>The article titles ("Whether ...?") in order, used for search and preview.</summary>
    public IReadOnlyList<SummaArticleEntry> Articles { get; init; } = [];
}

/// <summary>An article ("Whether ...?") within a question in the browse index.</summary>
public sealed class SummaArticleEntry
{
    /// <summary>The article number within its question (1-based).</summary>
    public required int Number { get; init; }

    /// <summary>The article title (e.g. "Whether, besides philosophy, any further doctrine is required?").</summary>
    public required string Title { get; init; }
}

/// <summary>
/// The full content of a single question, including every article's rendered HTML.
/// Fetched on demand when a reader opens a question so the initial index stays small.
/// </summary>
public sealed class SummaQuestionContent
{
    /// <summary>Stable id / URL slug for the question (matches <see cref="SummaQuestionEntry.Id"/>).</summary>
    public required string Id { get; init; }

    /// <summary>Id of the part this question belongs to.</summary>
    public required string PartId { get; init; }

    /// <summary>The question number within its part.</summary>
    public required int Number { get; init; }

    /// <summary>Display title of the question.</summary>
    public required string Title { get; init; }

    /// <summary>Optional prologue text (rendered HTML) that precedes the articles.</summary>
    public string PrologueHtml { get; init; } = string.Empty;

    /// <summary>The full articles in order.</summary>
    public IReadOnlyList<SummaArticleContent> Articles { get; init; } = [];
}

/// <summary>The full content of a single article, split into its classic structural sections.</summary>
public sealed class SummaArticleContent
{
    /// <summary>The article number within its question.</summary>
    public required int Number { get; init; }

    /// <summary>The article title ("Whether ...?").</summary>
    public required string Title { get; init; }

    /// <summary>
    /// Optional rendered HTML that precedes the first objection (rare lead-in text). Empty when the
    /// article opens directly with "Objection 1".
    /// </summary>
    public string PreambleHtml { get; init; } = string.Empty;

    /// <summary>The objections ("Objection N"), in order.</summary>
    public IReadOnlyList<SummaArticleSection> Objections { get; init; } = [];

    /// <summary>The "On the contrary" (sed contra) rendered HTML. Empty when the article has none.</summary>
    public string SedContraHtml { get; init; } = string.Empty;

    /// <summary>The "I answer that" (respondeo) rendered HTML. Empty when the article has none.</summary>
    public string RespondeoHtml { get; init; } = string.Empty;

    /// <summary>The replies to objections ("Reply to Objection N"), in order.</summary>
    public IReadOnlyList<SummaArticleSection> Replies { get; init; } = [];
}

/// <summary>A numbered article section (an objection or a reply to an objection).</summary>
public sealed class SummaArticleSection
{
    /// <summary>The objection/reply number (1-based).</summary>
    public required int Number { get; init; }

    /// <summary>The rendered HTML of the section.</summary>
    public required string Html { get; init; }
}
