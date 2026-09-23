namespace Respondeo.Content.Summa;

/// <summary>
/// The lightweight browse/search index for the whole Summa Theologica: every part, question, and article
/// title with its stable id, but without the heavy article bodies. Loaded once so the hierarchy can be
/// browsed and titles/question text can be searched with a single fetch.
/// </summary>
public sealed class SummaIndex
{
    /// <summary>The five parts in reading order (First Part, First Part of the Second Part, etc.).</summary>
    public IReadOnlyList<SummaPartEntry> Parts { get; init; } = [];
}

/// <summary>A part of the Summa (e.g. "FP") in the browse index.</summary>
public sealed class SummaPartEntry
{
    /// <summary>Stable id / URL slug for the part (e.g. "fp", "fs", "ss", "tp", "xp").</summary>
    public required string Id { get; init; }

    /// <summary>Display title (e.g. "First Part").</summary>
    public required string Title { get; init; }

    /// <summary>The questions belonging to this part, in order.</summary>
    public IReadOnlyList<SummaQuestionEntry> Questions { get; init; } = [];
}

/// <summary>A question within a part in the browse index.</summary>
public sealed class SummaQuestionEntry
{
    /// <summary>Stable id / URL slug for the question (e.g. "fp-q1").</summary>
    public required string Id { get; init; }

    /// <summary>The question number within its part (1-based).</summary>
    public required int Number { get; init; }

    /// <summary>Display title of the question (e.g. "The nature and extent of sacred doctrine").</summary>
    public required string Title { get; init; }

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
/// The full content of a single question, including every article's rendered HTML. Fetched on demand
/// when a reader opens a question so the initial index stays small.
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

/// <summary>The full content of a single article.</summary>
public sealed class SummaArticleContent
{
    /// <summary>The article number within its question.</summary>
    public required int Number { get; init; }

    /// <summary>The article title ("Whether ...?").</summary>
    public required string Title { get; init; }

    /// <summary>Rendered HTML of the full article body (objections, sed contra, respondeo, replies).</summary>
    public required string BodyHtml { get; init; }
}
