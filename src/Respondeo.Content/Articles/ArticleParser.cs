using System.Text.RegularExpressions;
using Respondeo.Content.Infrastructure;
using Respondeo.Content.Rendering;

namespace Respondeo.Content.Articles;

/// <summary>
/// Turns a raw article Markdown file (with a "---" delimited YAML front-matter block) into the typed <see cref="ArticleDocument"/>
/// domain model. The body is split into titled sections on top-level "## " headings.
/// Front-matter reading is delegated to the shared <see cref="FrontMatterReader"/> and HTML rendering to the injected <see cref="IContentHtmlRenderer"/>;
/// the parser performs no I/O so it can be tested in isolation.
/// </summary>
internal sealed partial class ArticleParser(IContentHtmlRenderer html)
{
    private readonly FrontMatterReader _reader = new();

    /// <summary>Parses an article Markdown file, or returns null when it lacks valid front-matter / an id.</summary>
    public ArticleDocument? Parse(string raw) => _reader.TryRead<ArticleFrontMatter>(raw, out var meta, out var body) ? Map(meta!, body) : null;

    /// <summary>
    /// Maps an already-parsed front-matter block and body onto an <see cref="ArticleDocument"/>.
    /// Shared by <see cref="Parse"/> and the content loader so both produce identical documents from the same input.
    /// </summary>
    public ArticleDocument Map(ArticleFrontMatter meta, string body) => new()
    {
        Id = meta.Id,
        Title = meta.Title,
        SortValue = meta.SortValue,
        Summary = meta.Summary,
        Topic = NormalizeSlug(meta.Topic, "general"),
        Tags = meta.Tags,
        IsUnvetted = meta.IsUnvetted,
        BodyHtml = html.ToHtml(body),
        Sources = [.. meta.Sources.Select(s => new ArticleSourceDocument { Label = NormalizeLabel(s.Label), Url = s.Url })],
    };

    private static string NormalizeSlug(string? slug, string fallback) => string.IsNullOrWhiteSpace(slug) ? fallback : slug.Trim().ToLowerInvariant();

    // Converts a plain hyphen used as a numeric range separator (e.g. "1285-1321" or "8:14-17") into an en dash,
    // so authors can type a normal "-" in source labels rather than a "&ndash;" entity.
    // Scoped to digit-bounded hyphens so ordinary hyphenated words in a label (e.g. "Pre-Congregation") are left alone.
    private static string NormalizeLabel(string label) => RangeSeparator().Replace(label, "\u2013");

    // Matches a hyphen (with optional surrounding spaces) that sits between two digits, i.e. a number range.
    [GeneratedRegex(@"(?<=\d)\s*-\s*(?=\d)")]
    private static partial Regex RangeSeparator();
}
