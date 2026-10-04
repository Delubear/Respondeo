namespace Respondeo.Content.Devotions;

// ---------------------------------------------------------------------------
// Devotion domain models (internal). Produced by DevotionParser from the JSON
// DTO, then mapped onto the public Respondeo.Content.Contracts DTOs at the
// service boundary. Never exposed to consumers.
// ---------------------------------------------------------------------------

/// <summary>A complete, data-driven devotion (internal domain model).</summary>
internal sealed class DevotionDocument
{
    public required string Id { get; init; }

    public required string Title { get; init; }

    public required string SortValue { get; init; }

    public string Summary { get; init; } = string.Empty;

    public string Kind { get; init; } = "devotion";

    public string? IntroHtml { get; init; }

    public bool LatinAvailable { get; init; }

    public bool Draft { get; init; }

    public IReadOnlyList<MysterySetDocument> MysterySets { get; init; } = [];

    public required IReadOnlyList<DevotionStepDocument> Sequence { get; init; }
}

/// <summary>A selectable group of meditations (internal domain model).</summary>
internal sealed class MysterySetDocument
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public string? When { get; init; }

    public string? Summary { get; init; }

    public required IReadOnlyList<MysteryDocument> Mysteries { get; init; }
}

/// <summary>A single meditation within a <see cref="MysterySetDocument"/> (internal domain model).</summary>
internal sealed class MysteryDocument
{
    public required string Title { get; init; }

    public string? ReflectionHtml { get; init; }
}

/// <summary>One step of a devotion (internal domain model).</summary>
internal sealed class DevotionStepDocument
{
    public string Kind { get; init; } = "prayer";

    public string? Title { get; init; }

    public string? PrayerId { get; init; }

    public int Repeat { get; init; } = 1;

    public string? Bead { get; init; }

    public string? Note { get; init; }

    public string? NoteLatin { get; init; }

    /// <summary>Optional rendered HTML explanation of this step, shown in the info dialog.</summary>
    public string? ExplanationHtml { get; init; }

    /// <summary>Optional rendered HTML inline words for a step with no catalogued prayer.</summary>
    public string? TextHtml { get; init; }

    /// <summary>Optional rendered HTML Latin form of <see cref="TextHtml"/>.</summary>
    public string? TextLatinHtml { get; init; }

    /// <summary>Optional "who says this" role: "priest", "people", "all", or "reader".</summary>
    public string? Role { get; init; }

    public IReadOnlyList<DevotionStepDocument> PerMystery { get; init; } = [];
}
