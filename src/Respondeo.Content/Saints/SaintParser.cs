using Respondeo.Content.Infrastructure;
using Respondeo.Content.Rendering;
using Respondeo.Content.Shared;

namespace Respondeo.Content.Saints;

/// <summary>
/// Turns a raw saint Markdown file (with a "---" delimited YAML front-matter block) into the typed <see cref="SaintRecordDocument"/> domain model.
/// Front-matter reading is delegated to the shared <see cref="FrontMatterReader"/> and HTML rendering to the injected <see cref="IContentHtmlRenderer"/>
/// so the Markdown engine stays behind an abstraction.
/// Performs no I/O so it can be tested in isolation. The body is split into titled sections on top-level "## " headings.
/// </summary>
internal sealed class SaintParser(IContentHtmlRenderer html)
{
    private readonly FrontMatterReader _reader = new();

    /// <summary>
    /// Parses raw file content into a saint record, or returns null when there is no valid front-matter block or the front-matter lacks an id.
    /// </summary>
    public SaintRecordDocument? Parse(string raw) => _reader.TryRead<SaintFrontMatter>(raw, out var meta, out var body) ? Map(meta!, body) : null;

    /// <summary>
    /// Maps an already-parsed front-matter block and body onto a <see cref="SaintRecordDocument"/>.
    /// Shared by <see cref="Parse"/> and the content loader so both produce identical records from the same input.
    /// </summary>
    public SaintRecordDocument Map(SaintFrontMatter meta, string body) => new()
    {
        Id = meta.Id,
        Title = meta.Title,
        SortValue = meta.SortValue,
        Summary = meta.Summary,
        Era = NormalizeSlug(meta.Era, "unknown"),
        Region = NormalizeSlug(meta.Region, "unknown"),
        Patronages = NormalizeText(meta.Patronages),
        StatesOfLife = NormalizeList(meta.StatesOfLife, fallback: null),
        Canonizations = NormalizeList(meta.Canonizations, fallback: null),
        Dates = meta.Dates,
        FeastDay = meta.FeastDay,
        Tags = meta.Tags,
        IsUnvetted = meta.IsUnvetted,
        BodyHtml = html.ToHtml(body),
        Sources = [.. meta.Sources.Select(s => new SaintSourceDocument { Label = s.Label, Url = s.Url })],
    };

    // Normalizes a single facet slug: trims, lowercases, and falls back to the supplied default when blank.
    private static string NormalizeSlug(string? slug, string fallback) => string.IsNullOrWhiteSpace(slug) ? fallback : slug.Trim().ToLowerInvariant();

    // Normalizes a list of facet slugs: trims/lowercases, drops blanks and duplicates (order-preserving).
    // When a fallback is supplied it is used for an otherwise-empty list; a null fallback leaves the list empty
    // (so a content-integrity test can require authors to populate it).
    private static IReadOnlyList<string> NormalizeList(IEnumerable<string>? slugs, string? fallback)
    {
        var result = new List<string>();
        if (slugs is not null)
        {
            foreach (var raw in slugs)
            {
                if (string.IsNullOrWhiteSpace(raw))
                {
                    continue;
                }

                var slug = raw.Trim().ToLowerInvariant();
                if (!result.Contains(slug))
                {
                    result.Add(slug);
                }
            }
        }

        return result.Count > 0 || fallback is null ? result : [fallback];
    }

    // Normalizes a list of free-text display values: trims, drops blanks and case-insensitive duplicates,
    // and preserves the author's casing (these are shown verbatim, not resolved through a facet map).
    private static IReadOnlyList<string> NormalizeText(IEnumerable<string>? values)
    {
        var result = new List<string>();
        if (values is not null)
        {
            foreach (var raw in values)
            {
                if (string.IsNullOrWhiteSpace(raw))
                {
                    continue;
                }

                var value = raw.Trim();
                if (!result.Contains(value, StringComparer.OrdinalIgnoreCase))
                {
                    result.Add(value);
                }
            }
        }

        return result;
    }
}
