using Respondeo.Content.Infrastructure;
using Respondeo.Content.Rendering;

namespace Respondeo.Content.Devotions;

/// <summary>
/// Maps an already-deserialized devotion JSON DTO onto the internal <see cref="DevotionDocument"/> domain model,
/// rendering the Markdown intro / reflections to HTML via the injected <see cref="IContentHtmlRenderer"/>.
/// Performs no I/O so it can be tested in isolation.
/// </summary>
internal sealed class DevotionParser(IContentHtmlRenderer html)
{
    /// <summary>Maps a devotion JSON DTO onto the domain model, rendering its Markdown fields to HTML.</summary>
    public DevotionDocument? Parse(DevotionDto? dto)
    {
        if (dto is null || string.IsNullOrWhiteSpace(dto.Id))
        {
            return null;
        }

        return new DevotionDocument
        {
            Id = dto.Id,
            Title = dto.Title,
            SortValue = string.IsNullOrWhiteSpace(dto.SortKey) ? dto.Title : dto.SortKey.Trim(),
            Summary = dto.Summary,
            Kind = NormalizeSlug(dto.Kind, "devotion"),
            IntroHtml = string.IsNullOrWhiteSpace(dto.Intro) ? null : html.ToHtml(dto.Intro),
            LatinAvailable = dto.LatinAvailable,
            MysterySets = [.. dto.MysterySets.Select(MapSet)],
            Sequence = [.. dto.Sequence.Select(MapStep)],
        };
    }

    private MysterySetDocument MapSet(MysterySetDto set) => new()
    {
        Id = set.Id,
        Name = set.Name,
        When = set.When,
        Summary = set.Summary,
        Mysteries = [.. set.Mysteries.Select(m => new MysteryDocument
        {
            Title = m.Title,
            ReflectionHtml = string.IsNullOrWhiteSpace(m.Reflection) ? null : html.ToHtml(m.Reflection),
        })],
    };

    private DevotionStepDocument MapStep(DevotionStepDto step) => new()
    {
        Kind = string.IsNullOrWhiteSpace(step.Kind) ? "prayer" : step.Kind.Trim().ToLowerInvariant(),
        Title = step.Title,
        PrayerId = step.PrayerId,
        Repeat = step.Repeat < 1 ? 1 : step.Repeat,
        Bead = string.IsNullOrWhiteSpace(step.Bead) ? null : step.Bead.Trim().ToLowerInvariant(),
        Note = string.IsNullOrWhiteSpace(step.Note) ? null : step.Note.Trim(),
        NoteLatin = string.IsNullOrWhiteSpace(step.NoteLatin) ? null : step.NoteLatin.Trim(),
        PerMystery = [.. step.PerMystery.Select(MapStep)],
    };

    private static string NormalizeSlug(string? slug, string fallback) => string.IsNullOrWhiteSpace(slug) ? fallback : slug.Trim().ToLowerInvariant();
}
