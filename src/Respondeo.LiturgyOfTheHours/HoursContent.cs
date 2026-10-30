using System.Text.Json;
using System.Text.Json.Serialization;

namespace Respondeo.LiturgyOfTheHours;

// ---------------------------------------------------------------------------
// Licensed proper texts (hymns, antiphons, readings, responsories, intercessions, collects).
// The English translation of the Liturgy of the Hours is copyrighted by ICEL, so the real texts never live in this repository.
// The build embeds exactly one content file under the logical name below:
//   - by default, SampleContent/hours-content.sample.json (invented placeholder text, clearly marked as a sample);
//   - in the deployment build, the licensed file passed through the HoursLicensedContentPath MSBuild property,
//     checked out from a private repository by CI.
// ---------------------------------------------------------------------------

/// <summary>One licensed text, optionally with the antiphon that frames it.</summary>
/// <param name="Text">The text to pray, with line breaks preserved as authored.</param>
/// <param name="Antiphon">The antiphon said before and after, when the element has one.</param>
public sealed record LicensedText(string Text, string? Antiphon = null);

/// <summary>The serialized shape of an embedded content file.</summary>
internal sealed record HoursContentFile
{
    /// <summary>True for the placeholder file; false only for licensed texts.</summary>
    [JsonPropertyName("isSample")]
    public bool IsSample { get; init; } = true;

    /// <summary>The copyright acknowledgment the licensor requires to be displayed with its texts.</summary>
    [JsonPropertyName("acknowledgment")]
    public string Acknowledgment { get; init; } = string.Empty;

    /// <summary>Texts keyed from most to least specific, e.g. "ordinary/w2/monday/morning/hymn", "morning/hymn", "hymn".</summary>
    [JsonPropertyName("texts")]
    public Dictionary<string, LicensedText> Texts { get; init; } = [];
}

/// <summary>Checks that an acknowledgment carries the elements ICEL's publication policy requires.</summary>
public static class HoursAcknowledgment
{
    /// <summary>
    /// True when the text has the copyright symbol, ICEL's corporate title (full or abbreviated), and "All rights reserved".
    /// Licensed texts are never shown without such an acknowledgment.
    /// </summary>
    public static bool IsValid(string? acknowledgment) =>
        !string.IsNullOrWhiteSpace(acknowledgment)
        && acknowledgment.Contains('©')
        && (acknowledgment.Contains("International Commission on English in the Liturgy", StringComparison.Ordinal)
            || acknowledgment.Contains("International Committee on English in the Liturgy", StringComparison.Ordinal)
            || acknowledgment.Contains("ICEL", StringComparison.Ordinal))
        && acknowledgment.Contains("All rights reserved", StringComparison.OrdinalIgnoreCase);
}

/// <summary>Supplies licensed proper texts for the office.</summary>
public interface IHoursTextProvider
{
    /// <summary>True when the texts are the invented placeholders, not licensed texts.</summary>
    bool IsSample { get; }

    /// <summary>The acknowledgment to display wherever the texts are shown.</summary>
    string Acknowledgment { get; }

    /// <summary>Returns the first text found among the keys, tried in order, or null.</summary>
    LicensedText? Find(IEnumerable<string> keys);
}

/// <summary>Reads the single content file embedded in this assembly.</summary>
internal sealed class EmbeddedHoursTextProvider : IHoursTextProvider
{
    internal const string ResourceName = "Respondeo.LiturgyOfTheHours.Content.json";

    private static readonly Lazy<HoursContentFile> Content = new(Load);

    public bool IsSample => Content.Value.IsSample;

    public string Acknowledgment => Content.Value.Acknowledgment;

    public LicensedText? Find(IEnumerable<string> keys)
    {
        if (!IsSample && !HoursAcknowledgment.IsValid(Acknowledgment))
        {
            return null;
        }

        foreach (var key in keys)
        {
            if (Content.Value.Texts.TryGetValue(key, out var text))
            {
                return text;
            }
        }

        return null;
    }

    private static HoursContentFile Load()
    {
        using var stream = typeof(EmbeddedHoursTextProvider).Assembly.GetManifestResourceStream(ResourceName);

        return stream is null
            ? new HoursContentFile()
            : JsonSerializer.Deserialize<HoursContentFile>(stream, Options) ?? new HoursContentFile();
    }

    private static readonly JsonSerializerOptions Options = new()
    {
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        PropertyNameCaseInsensitive = true,
    };
}
