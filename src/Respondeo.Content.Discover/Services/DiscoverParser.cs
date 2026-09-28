using Respondeo.Content.Shared;
using Respondeo.Content.Discover.Internal;

namespace Respondeo.Content.Discover.Services;

/// <summary>
/// Turns raw Discover content files into the typed public models.
/// Prayers and articles are Markdown with a "---" delimited YAML front-matter block;
/// devotions arrive as already-deserialized JSON DTOs whose Markdown intro / reflections are rendered here.
/// Front-matter reading is delegated to the shared <see cref="FrontMatterReader"/> and HTML rendering to the injected <see cref="IContentHtmlRenderer"/>;
/// the parser performs no I/O so it can be tested in isolation.
/// </summary>
internal sealed class DiscoverParser(IContentHtmlRenderer html)
{
    private readonly FrontMatterReader _reader = new();

    /// <summary>Parses a prayer Markdown file, or returns null when it lacks valid front-matter / an id.</summary>
    public Prayer? ParsePrayer(string raw)
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
            Html = html.ToHtml(body.Trim()),
            Tags = meta.Tags,
            Attribution = meta.Attribution,
        };
    }

    /// <summary>Parses an article Markdown file, or returns null when it lacks valid front-matter / an id.</summary>
    public Article? ParseArticle(string raw)
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

    /// <summary>Maps a devotion JSON DTO onto the public model, rendering its Markdown fields to HTML.</summary>
    public Devotion? ParseDevotion(DevotionDto? dto)
    {
        if (dto is null || string.IsNullOrWhiteSpace(dto.Id))
        {
            return null;
        }

        return new Devotion
        {
            Id = dto.Id,
            Title = dto.Title,
            Summary = dto.Summary,
            Kind = NormalizeSlug(dto.Kind, "devotion"),
            IntroHtml = string.IsNullOrWhiteSpace(dto.Intro) ? null : html.ToHtml(dto.Intro),
            MysterySets = [.. dto.MysterySets.Select(MapSet)],
            Sequence = [.. dto.Sequence.Select(MapStep)],
        };
    }

    public static PrayerSummary ToSummary(Prayer prayer) => new()
    {
        Id = prayer.Id,
        Title = prayer.Title,
        Summary = prayer.Summary,
        Category = prayer.Category,
        Tags = prayer.Tags,
    };

    public static DevotionSummary ToSummary(Devotion devotion) => new()
    {
        Id = devotion.Id,
        Title = devotion.Title,
        Summary = devotion.Summary,
        Kind = devotion.Kind,
    };

    public static ArticleSummary ToSummary(Article article) => new()
    {
        Id = article.Id,
        Title = article.Title,
        Summary = article.Summary,
        Topic = article.Topic,
        Tags = article.Tags,
    };

    private MysterySet MapSet(MysterySetDto set) => new()
    {
        Id = set.Id,
        Name = set.Name,
        When = set.When,
        Mysteries = [.. set.Mysteries.Select(m => new Mystery
        {
            Title = m.Title,
            ReflectionHtml = string.IsNullOrWhiteSpace(m.Reflection) ? null : html.ToHtml(m.Reflection),
        })],
    };

    private DevotionStep MapStep(DevotionStepDto step) => new()
    {
        Kind = string.IsNullOrWhiteSpace(step.Kind) ? "prayer" : step.Kind.Trim().ToLowerInvariant(),
        Title = step.Title,
        PrayerId = step.PrayerId,
        Repeat = step.Repeat < 1 ? 1 : step.Repeat,
        PerMystery = [.. step.PerMystery.Select(MapStep)],
    };

    private static string NormalizeSlug(string? slug, string fallback) =>
        string.IsNullOrWhiteSpace(slug) ? fallback : slug.Trim().ToLowerInvariant();

    // Split the Markdown body into sections on each top-level "## " heading, rendering each section's Markdown to HTML.
    // Content before the first heading is an untitled lead-in section.
    private IReadOnlyList<ArticleSection> SplitSections(string body) =>
        [.. MarkdownSections.Split(body).Select(s => new ArticleSection { Heading = s.Heading, Html = html.ToHtml(s.Markdown) })];
}
