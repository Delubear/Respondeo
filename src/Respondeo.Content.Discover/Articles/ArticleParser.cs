using Respondeo.Content.Shared;

namespace Respondeo.Content.Discover.Articles;

/// <summary>
/// Turns a raw article Markdown file (with a "---" delimited YAML front-matter block) into the typed <see cref="Article"/>
/// and its lightweight <see cref="ArticleSummary"/> projection. The body is split into titled sections on top-level "## " headings.
/// Front-matter reading is delegated to the shared <see cref="FrontMatterReader"/> and HTML rendering to the injected <see cref="IContentHtmlRenderer"/>;
/// the parser performs no I/O so it can be tested in isolation.
/// </summary>
internal sealed class ArticleParser(IContentHtmlRenderer html)
{
    private readonly FrontMatterReader _reader = new();

    /// <summary>Parses an article Markdown file, or returns null when it lacks valid front-matter / an id.</summary>
    public Article? Parse(string raw)
    {
        if (!_reader.TryRead<ArticleFrontMatter>(raw, out var meta, out var body))
        {
            return null;
        }

        return new Article
        {
            Id = meta!.Id,
            Title = meta.Title,
            Summary = meta.Summary,
            Topic = NormalizeSlug(meta.Topic, "general"),
            Tags = meta.Tags,
            Sections = SplitSections(body),
            Sources = [.. meta.Sources.Select(s => new ArticleSource { Label = s.Label, Url = s.Url })],
        };
    }

    /// <summary>Projects a full article down to its lightweight browse-index summary.</summary>
    public static ArticleSummary ToSummary(Article article) => new()
    {
        Id = article.Id,
        Title = article.Title,
        Summary = article.Summary,
        Topic = article.Topic,
        Tags = article.Tags,
    };

    private static string NormalizeSlug(string? slug, string fallback) =>
        string.IsNullOrWhiteSpace(slug) ? fallback : slug.Trim().ToLowerInvariant();

    // Split the Markdown body into sections on each top-level "## " heading, rendering each section's Markdown to HTML.
    // Content before the first heading is an untitled lead-in section.
    private IReadOnlyList<ArticleSection> SplitSections(string body) =>
        [.. MarkdownSections.Split(body).Select(s => new ArticleSection { Heading = s.Heading, Html = html.ToHtml(s.Markdown) })];
}
