using Respondeo.Content.Infrastructure;
using Respondeo.Content.Rendering;
using Respondeo.Content.Shared;
using System.Text.RegularExpressions;

namespace Respondeo.Content.Prayers;

/// <summary>
/// Turns a raw prayer Markdown file (with a "---" delimited YAML front-matter block) into the typed <see cref="PrayerDocument"/> domain model.
/// Front-matter reading is delegated to the shared <see cref="FrontMatterReader"/> and HTML rendering to the injected <see cref="IContentHtmlRenderer"/>;
/// the parser performs no I/O so it can be tested in isolation.
/// </summary>
internal sealed class PrayerParser(IContentHtmlRenderer html)
{
    private readonly FrontMatterReader _reader = new();

    /// <summary>Parses a prayer Markdown file, or returns null when it lacks valid front-matter / an id.</summary>
    public PrayerDocument? Parse(string raw) => _reader.TryRead<PrayerFrontMatter>(raw, out var meta, out var body) ? Map(meta!, body) : null;

    /// <summary>
    /// Maps an already-parsed front-matter block and body onto a <see cref="PrayerDocument"/>.
    /// Shared by <see cref="Parse"/> and the content loader so both produce identical documents from the same input.
    /// </summary>
    public PrayerDocument Map(PrayerFrontMatter meta, string body) => new()
    {
        Id = meta.Id,
        Title = meta.Title,
        SortValue = meta.SortValue,
        Summary = meta.Summary,
        Category = NormalizeSlug(meta.Category, "other"),
        Language = NormalizeSlug(meta.Language, "en"),
        TranslationKey = string.IsNullOrWhiteSpace(meta.TranslationKey) ? null : meta.TranslationKey.Trim(),
        Html = html.ToHtmlPreservingLineBreaks(AnnotateVersicleMarkers(body.Trim())),
        Tags = meta.Tags,
        Attribution = meta.Attribution,
        Unlisted = meta.Unlisted,
    };

    private static string NormalizeSlug(string? slug, string fallback) => string.IsNullOrWhiteSpace(slug) ? fallback : slug.Trim().ToLowerInvariant();

    // Lines that open with a versicle/response marker ("V." / "R.") would otherwise be parsed by Markdig as ordered-list items (e.g. <ol type="I">),
    // dropping the literal marker. Escaping the dot keeps "V." and "R." as plain text so the prayer reads as verse, not a numbered list.
    private static readonly Regex VersicleMarker = new(@"(?m)^(\s*[VR])\.(\s)", RegexOptions.Compiled);

    private static string AnnotateVersicleMarkers(string body) => VersicleMarker.Replace(body, "$1\\.$2");
}
