namespace Respondeo.Content.Discover.Articles;

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
