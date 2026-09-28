namespace Respondeo.Content.Credo.Internal;

/// <summary>
/// The JSON shape of a devotion file. A devotion is authored entirely as data so new ones (a chaplet,
/// a different rosary form) are added without any code change. The parser maps this onto the public
/// <see cref="Devotion"/> model, rendering the Markdown intro / reflections to HTML on the way.
/// </summary>
internal sealed class DevotionDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;

    /// <summary>Markdown introduction, rendered to HTML by the parser.</summary>
    public string? Intro { get; set; }

    public List<MysterySetDto> MysterySets { get; set; } = [];
    public List<DevotionStepDto> Sequence { get; set; } = [];
}

internal sealed class MysterySetDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? When { get; set; }
    public List<MysteryDto> Mysteries { get; set; } = [];
}

internal sealed class MysteryDto
{
    public string Title { get; set; } = string.Empty;

    /// <summary>Markdown reflection, rendered to HTML by the parser.</summary>
    public string? Reflection { get; set; }
}

internal sealed class DevotionStepDto
{
    public string Kind { get; set; } = "prayer";
    public string? Title { get; set; }
    public string? PrayerId { get; set; }
    public int Repeat { get; set; } = 1;
    public List<DevotionStepDto> PerMystery { get; set; } = [];
}
