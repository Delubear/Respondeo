using Respondeo.Content.Shared;

namespace Respondeo.Content.Discover.Prayers;

/// <summary>
/// Turns a raw prayer Markdown file (with a "---" delimited YAML front-matter block) into the typed <see cref="Prayer"/>
/// and its lightweight <see cref="PrayerSummary"/> projection.
/// Front-matter reading is delegated to the shared <see cref="FrontMatterReader"/> and HTML rendering to the injected <see cref="IContentHtmlRenderer"/>;
/// the parser performs no I/O so it can be tested in isolation.
/// </summary>
internal sealed class PrayerParser(IContentHtmlRenderer html)
{
    private readonly FrontMatterReader _reader = new();

    /// <summary>Parses a prayer Markdown file, or returns null when it lacks valid front-matter / an id.</summary>
    public Prayer? Parse(string raw)
    {
        if (!_reader.TryRead<PrayerFrontMatter>(raw, out var meta, out var body))
        {
            return null;
        }

        return new Prayer
        {
            Id = meta!.Id,
            Title = meta.Title,
            Summary = meta.Summary,
            Category = NormalizeSlug(meta.Category, "other"),
            Language = NormalizeSlug(meta.Language, "en"),
            TranslationKey = string.IsNullOrWhiteSpace(meta.TranslationKey) ? null : meta.TranslationKey.Trim(),
            Html = html.ToHtmlPreservingLineBreaks(body.Trim()),
            Tags = meta.Tags,
            Attribution = meta.Attribution,
        };
    }

    /// <summary>Projects a full prayer down to its lightweight browse-index summary.</summary>
    public static PrayerSummary ToSummary(Prayer prayer) => new()
    {
        Id = prayer.Id,
        Title = prayer.Title,
        Summary = prayer.Summary,
        Category = prayer.Category,
        Tags = prayer.Tags,
    };

    private static string NormalizeSlug(string? slug, string fallback) => string.IsNullOrWhiteSpace(slug) ? fallback : slug.Trim().ToLowerInvariant();
}
