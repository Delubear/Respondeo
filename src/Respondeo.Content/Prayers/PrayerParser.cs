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
        PrayedAt = [.. meta.PrayedAt.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim().ToLowerInvariant()).Distinct()],
        Language = NormalizeSlug(meta.Language, "en"),
        TranslationKey = string.IsNullOrWhiteSpace(meta.TranslationKey) ? null : meta.TranslationKey.Trim(),
        Html = html.ToHtmlPreservingLineBreaks(AnnotateVersicleMarkers(body.Trim())),
        Tags = meta.Tags,
        Attribution = meta.Attribution,
        Unlisted = meta.Unlisted,
    };

    private static string NormalizeSlug(string? slug, string fallback) => string.IsNullOrWhiteSpace(slug) ? fallback : slug.Trim().ToLowerInvariant();

    // Lines that open with a versicle/response marker ("V." / "R.") would otherwise be parsed by Markdig as ordered-list items (e.g. <ol type="I">),
    // dropping the literal marker. They are replaced with the red breviary symbols (U+2123 / U+211F) used in the Liturgy of the Hours.
    private static readonly Regex VersicleMarker = new(@"(?m)^(\s*)([VR])\.(\s)", RegexOptions.Compiled);

    private static string AnnotateVersicleMarkers(string body) => VersicleMarker.Replace(body, m =>
        $"{m.Groups[1].Value}<span class=\"vr-mark\">{(m.Groups[2].Value == "V" ? "\u2123" : "\u211F")}</span>{m.Groups[3].Value}");
}
