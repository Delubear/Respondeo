using Respondeo.Content.Shared;

namespace Respondeo.Content.Discover.Devotions;

/// <summary>
/// Maps an already-deserialized devotion JSON DTO onto the public <see cref="Devotion"/> model and its lightweight <see cref="DevotionSummary"/> projection,
/// rendering the Markdown intro / reflections to HTML via the injected <see cref="IContentHtmlRenderer"/>.
/// Performs no I/O so it can be tested in isolation.
/// </summary>
internal sealed class DevotionParser(IContentHtmlRenderer html)
{
    /// <summary>Maps a devotion JSON DTO onto the public model, rendering its Markdown fields to HTML.</summary>
    public Devotion? Parse(DevotionDto? dto)
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

    /// <summary>Projects a full devotion down to its lightweight browse-index summary.</summary>
    public static DevotionSummary ToSummary(Devotion devotion) => new()
    {
        Id = devotion.Id,
        Title = devotion.Title,
        Summary = devotion.Summary,
        Kind = devotion.Kind,
    };

    private MysterySet MapSet(MysterySetDto set) => new()
    {
        Id = set.Id,
        Name = set.Name,
        When = set.When,
        Summary = set.Summary,
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
        Bead = string.IsNullOrWhiteSpace(step.Bead) ? null : step.Bead.Trim().ToLowerInvariant(),
        Note = string.IsNullOrWhiteSpace(step.Note) ? null : step.Note.Trim(),
        PerMystery = [.. step.PerMystery.Select(MapStep)],
    };

    private static string NormalizeSlug(string? slug, string fallback) => string.IsNullOrWhiteSpace(slug) ? fallback : slug.Trim().ToLowerInvariant();
}
