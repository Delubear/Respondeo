using System.Text.RegularExpressions;
using Respondeo.Content.Infrastructure;
using Respondeo.Content.Rendering;
using Respondeo.Content.Shared;

namespace Respondeo.Content.Miracles;

/// <summary>
/// Turns a raw miracle Markdown file (with a "---" delimited YAML front-matter block) into the typed <see cref="MiracleRecordDocument"/>
/// domain model.
/// Front-matter reading is delegated to the shared <see cref="FrontMatterReader"/> and HTML rendering to the injected <see cref="IContentHtmlRenderer"/>
/// so the Markdown engine stays behind an abstraction.
/// Performs no I/O so it can be tested in isolation. The body is split into titled sections on top-level "## " headings.
/// </summary>
internal sealed partial class MiracleParser(IContentHtmlRenderer html)
{
    private readonly FrontMatterReader _reader = new();

    /// <summary>
    /// Parses raw file content into a miracle record, or returns null when there is no valid front-matter block or the front-matter lacks an id.
    /// </summary>
    public MiracleRecordDocument? Parse(string raw) => _reader.TryRead<MiracleFrontMatter>(raw, out var meta, out var body) ? Map(meta!, body) : null;

    /// <summary>
    /// Maps an already-parsed front-matter block and body onto a <see cref="MiracleRecordDocument"/>.
    /// Shared by <see cref="Parse"/> and the content loader so both produce identical records from the same input.
    /// </summary>
    public MiracleRecordDocument Map(MiracleFrontMatter meta, string body) => new()
    {
        Id = meta.Id,
        Title = meta.Title,
        SortValue = meta.SortValue,
        Summary = meta.Summary,
        Types = NormalizeTypes(meta.Types),
        Approval = NormalizeSlug(meta.Approval, "historical"),
        Region = NormalizeSlug(meta.Region, "unknown"),
        Country = meta.Country,
        Year = meta.Year,
        FeastDay = meta.FeastDay,
        Tags = meta.Tags,
        IsUnvetted = meta.IsUnvetted,
        BodyHtml = html.ToHtml(body),
        Sources = [.. meta.Sources.Select(s => new MiracleSourceDocument { Label = NormalizeLabel(s.Label), Url = s.Url })],
    };

    // Converts a plain hyphen used as a numeric range separator (e.g. "1093-1097" or "8:14-17") into an en dash,
    // so authors can type a normal "-" in source labels rather than a "&ndash;" entity.
    // Scoped to digit-bounded hyphens so ordinary hyphenated words in a label (e.g. "Pre-Congregation") are left alone.
    private static string NormalizeLabel(string label) => RangeSeparator().Replace(label, "\u2013");

    // Matches a hyphen (with optional surrounding spaces) that sits between two digits, i.e. a number range.
    [GeneratedRegex(@"(?<=\d)\s*-\s*(?=\d)")]
    private static partial Regex RangeSeparator();

    // Normalizes a single facet slug: trims, lowercases, and falls back to the supplied default when blank.
    private static string NormalizeSlug(string? slug, string fallback) => string.IsNullOrWhiteSpace(slug) ? fallback : slug.Trim().ToLowerInvariant();

    // Normalizes the category slugs: trims/lowercases, drops blanks and duplicates (order-preserving), and falls back to a single "other" when none are supplied.
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
}
