namespace Respondeo.Content.Devotions;

/// <summary>
/// The JSON shape of a devotion file. A devotion is authored entirely as data so new ones (a chaplet, a different rosary form) are added without any code change.
/// The parser maps this onto the public <see cref="Devotion"/> model, rendering the Markdown intro / reflections to HTML on the way.
/// </summary>
internal sealed class DevotionDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;

    /// <summary>Optional filing key for alphabetical sorting; falls back to the title when blank.</summary>
    public string? SortKey { get; set; }

    public string Summary { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;

    /// <summary>Markdown introduction, rendered to HTML by the parser.</summary>
    public string? Intro { get; set; }

    /// <summary>True when the referenced prayers have Latin translations, so the player can offer a Latin toggle.</summary>
    public bool LatinAvailable { get; set; }

    /// <summary>
    /// True while this devotion is still being authored and vetted. Draft devotions are flagged in the UI
    /// (a badge on the card and a notice before praying) so readers know the content is not yet verified.
    /// </summary>
    public bool Draft { get; set; }

    public List<MysterySetDto> MysterySets { get; set; } = [];
    public List<DevotionStepDto> Sequence { get; set; } = [];
}

internal sealed class MysterySetDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? When { get; set; }
    public string? Summary { get; set; }
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
    public string? Bead { get; set; }
    public string? Note { get; set; }
    public string? NoteLatin { get; set; }

    /// <summary>
    /// Optional plain-language explanation (Markdown) of what happens at this step and why, rendered
    /// to HTML by the parser and shown in the info dialog above the words. Used by the Mass walkthrough.
    /// </summary>
    public string? Explanation { get; set; }

    /// <summary>
    /// Optional inline words (Markdown) for a step that has no catalogued <see cref="PrayerId"/> — e.g.
    /// a short Mass response. Rendered to HTML and shown in the info dialog in place of a looked-up prayer.
    /// </summary>
    public string? Text { get; set; }

    /// <summary>Optional Latin form of <see cref="Text"/>, shown when the Latin view is active.</summary>
    public string? TextLatin { get; set; }

    /// <summary>
    /// Optional "who says this" role for liturgical steps: "priest", "people", "all", or "reader".
    /// Purely presentational; ignored by devotions that do not set it.
    /// </summary>
    public string? Role { get; set; }

    public List<DevotionStepDto> PerMystery { get; set; } = [];
}
