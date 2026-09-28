using Respondeo.Content.Abstractions;
using Respondeo.Content.Miracles.Internal;

namespace Respondeo.Content.Miracles.Services;

/// <summary>
/// Turns a raw miracle Markdown file (with a "---" delimited YAML front-matter block) into the typed
/// <see cref="MiracleRecord"/> and its lightweight <see cref="MiracleIndexEntry"/> projection.
/// Front-matter reading is delegated to the shared <see cref="FrontMatterReader"/> and HTML rendering to the
/// injected <see cref="IContentHtmlRenderer"/> so the Markdown engine stays behind an abstraction. Performs no
/// I/O so it can be tested in isolation. The body is split into titled sections on top-level "## " headings.
/// </summary>
internal sealed class MiracleParser
{
    private readonly IContentHtmlRenderer _html;
    private readonly FrontMatterReader _reader = new();

    public MiracleParser(IContentHtmlRenderer html) => _html = html;

    /// <summary>
    /// Parses raw file content into a miracle record, or returns null when there is no valid
    /// front-matter block or the front-matter lacks an id.
    /// </summary>
    public MiracleRecord? Parse(string raw) =>
        _reader.TryRead<MiracleFrontMatter>(raw, out var meta, out var body) ? Map(meta!, body) : null;

    /// <summary>
    /// Maps an already-parsed front-matter block and body onto a <see cref="MiracleRecord"/>. Shared by
    /// <see cref="Parse"/> and the content loader so both produce identical records from the same input.
    /// </summary>
    public MiracleRecord Map(MiracleFrontMatter meta, string body) => new()
    {
        Id = meta.Id,
        Title = meta.Title,
        Summary = meta.Summary,
        Types = NormalizeTypes(meta.Types),
        Approval = NormalizeSlug(meta.Approval, "historical"),
        Region = NormalizeSlug(meta.Region, "unknown"),
        Country = meta.Country,
        Year = meta.Year,
        FeastDay = meta.FeastDay,
        Tags = meta.Tags,
        Sections = SplitSections(body),
        Sources = [.. meta.Sources.Select(s => new MiracleSource { Label = s.Label, Url = s.Url })],
    };

    /// <summary>Projects a full record down to its lightweight index entry.</summary>
    public static MiracleIndexEntry ToIndexEntry(MiracleRecord record) => new()
    {
        Id = record.Id,
        Title = record.Title,
        Summary = record.Summary,
        Types = record.Types,
        Approval = record.Approval,
        Region = record.Region,
        Country = record.Country,
        Year = record.Year,
        Tags = record.Tags,
    };

    // Normalizes a single facet slug: trims, lowercases, and falls back to the supplied default when blank.
    private static string NormalizeSlug(string? slug, string fallback) =>
        string.IsNullOrWhiteSpace(slug) ? fallback : slug.Trim().ToLowerInvariant();

    // Normalizes the category slugs: trims/lowercases, drops blanks and duplicates (order-preserving),
    // and falls back to a single "other" when none are supplied.
    private static IReadOnlyList<string> NormalizeTypes(IEnumerable<string>? slugs)
    {
        if (slugs is null)
        {
            return ["other"];
        }

        var result = new List<string>();
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

        return result.Count > 0 ? result : ["other"];
    }

    // Split the Markdown body into sections on each top-level "## " heading, rendering each section's
    // Markdown to HTML. Content before the first heading becomes an untitled lead-in section.
    private IReadOnlyList<MiracleSection> SplitSections(string body) =>
        [.. MarkdownSections.Split(body).Select(s => new MiracleSection { Heading = s.Heading, Html = _html.ToHtml(s.Markdown) })];
}
