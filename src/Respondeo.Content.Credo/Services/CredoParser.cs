using Respondeo.Content.Abstractions;
using Respondeo.Content.Credo.Internal;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Respondeo.Content.Credo.Services;

/// <summary>
/// Turns raw Credo content files into the typed public models. Prayers and articles are Markdown with
/// a "---" delimited YAML front-matter block; devotions arrive as already-deserialized JSON DTOs whose
/// Markdown intro / reflections are rendered here. HTML rendering is delegated to the injected
/// <see cref="IContentHtmlRenderer"/>; the parser performs no I/O so it can be tested in isolation.
/// </summary>
internal sealed class CredoParser
{
    private readonly IContentHtmlRenderer _html;

    public CredoParser(IContentHtmlRenderer html) => _html = html;

    private readonly IDeserializer _yaml = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    /// <summary>Parses a prayer Markdown file, or returns null when it lacks valid front-matter / an id.</summary>
    public Prayer? ParsePrayer(string raw)
    {
        var (frontMatter, body) = FrontMatter.Split(raw);
        if (frontMatter is null)
        {
            return null;
        }

        var meta = _yaml.Deserialize<PrayerFrontMatter>(frontMatter);
        if (meta is null || string.IsNullOrWhiteSpace(meta.Id))
        {
            return null;
        }

        return new Prayer
        {
            Id = meta.Id,
            Title = meta.Title,
            Summary = meta.Summary,
            Category = NormalizeSlug(meta.Category, "other"),
            Language = NormalizeSlug(meta.Language, "en"),
            TranslationKey = string.IsNullOrWhiteSpace(meta.TranslationKey) ? null : meta.TranslationKey.Trim(),
            Html = _html.ToHtml(body.Trim()),
            Tags = meta.Tags,
            Attribution = meta.Attribution,
        };
    }

    /// <summary>Parses an article Markdown file, or returns null when it lacks valid front-matter / an id.</summary>
    public Article? ParseArticle(string raw)
    {
        var (frontMatter, body) = FrontMatter.Split(raw);
        if (frontMatter is null)
        {
            return null;
        }

        var meta = _yaml.Deserialize<ArticleFrontMatter>(frontMatter);
        if (meta is null || string.IsNullOrWhiteSpace(meta.Id))
        {
            return null;
        }

        return new Article
        {
            Id = meta.Id,
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
            IntroHtml = string.IsNullOrWhiteSpace(dto.Intro) ? null : _html.ToHtml(dto.Intro),
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
            ReflectionHtml = string.IsNullOrWhiteSpace(m.Reflection) ? null : _html.ToHtml(m.Reflection),
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

    // Split the Markdown body into sections on each top-level "## " heading. Content before the first
    // heading is an untitled lead-in section with an empty heading.
    private IReadOnlyList<ArticleSection> SplitSections(string body)
    {
        var lines = body.Replace("\r\n", "\n").Split('\n');
        var sections = new List<ArticleSection>();

        var currentHeading = string.Empty;
        var buffer = new List<string>();

        void Flush()
        {
            if (buffer.Count == 0)
            {
                return;
            }

            var markdown = string.Join('\n', buffer).Trim();
            buffer.Clear();
            if (markdown.Length == 0)
            {
                return;
            }

            sections.Add(new ArticleSection
            {
                Heading = currentHeading,
                Html = _html.ToHtml(markdown),
            });
        }

        foreach (var line in lines)
        {
            if (line.StartsWith("## ", StringComparison.Ordinal))
            {
                Flush();
                currentHeading = line[3..].Trim();
                continue;
            }

            buffer.Add(line);
        }

        Flush();
        return sections;
    }
}
