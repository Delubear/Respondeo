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

    /// <summary>The content schema version; files with an unsupported version are rejected.</summary>
    [JsonPropertyName("schemaVersion")]
    public int SchemaVersion { get; init; } = 1;

    /// <summary>Celebration metadata keyed by celebration id, e.g. which common supplies missing propers.</summary>
    [JsonPropertyName("celebrations")]
    public Dictionary<string, HoursCelebration> Celebrations { get; init; } = [];

    /// <summary>Texts keyed from most to least specific, e.g. "ordinary/w2/monday/morning/hymn", "morning/hymn", "hymn".</summary>
    [JsonPropertyName("texts")]
    public Dictionary<string, LicensedText> Texts { get; init; } = [];

    /// <summary>The highest schema version this build understands.</summary>
    internal const int SupportedSchemaVersion = 2;

    /// <summary>
    /// Merges several content files into one. Every file must share a supported schema version,
    /// and a text or celebration key may be defined only once across all files.
    /// The merged content is a sample unless at least one file declares licensed texts.
    /// </summary>
    internal static HoursContentFile Merge(IEnumerable<(string Name, HoursContentFile File)> files)
    {
        var texts = new Dictionary<string, LicensedText>(StringComparer.Ordinal);
        var celebrations = new Dictionary<string, HoursCelebration>(StringComparer.Ordinal);
        var isSample = true;
        var acknowledgment = string.Empty;
        var schemaVersion = 1;
        var any = false;

        foreach (var (name, file) in files)
        {
            if (file.SchemaVersion < 1 || file.SchemaVersion > SupportedSchemaVersion)
            {
                throw new InvalidDataException(
                    $"Hours content file '{name}' declares schemaVersion {file.SchemaVersion}; this build supports 1 to {SupportedSchemaVersion}.");
            }

            any = true;
            schemaVersion = Math.Max(schemaVersion, file.SchemaVersion);
            isSample &= file.IsSample;

            if (string.IsNullOrWhiteSpace(acknowledgment) && !string.IsNullOrWhiteSpace(file.Acknowledgment))
            {
                acknowledgment = file.Acknowledgment;
            }

            foreach (var (key, text) in file.Texts)
            {
                if (!texts.TryAdd(key, text))
                {
                    throw new InvalidDataException($"Hours content key '{key}' is defined more than once (again in '{name}').");
                }
            }

            foreach (var (id, celebration) in file.Celebrations)
            {
                if (!celebrations.TryAdd(id, celebration))
                {
                    throw new InvalidDataException($"Hours celebration '{id}' is defined more than once (again in '{name}').");
                }
            }
        }

        return any
            ? new HoursContentFile
            {
                IsSample = isSample,
                Acknowledgment = acknowledgment,
                SchemaVersion = schemaVersion,
                Celebrations = celebrations,
                Texts = texts,
            }
            : new HoursContentFile();
    }
}

/// <summary>Metadata for one celebration in the content.</summary>
/// <param name="Common">The common (e.g. "martyrs", "virgins") whose texts fill in what the proper lacks.</param>
public sealed record HoursCelebration(string? Common = null);

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

    /// <summary>Returns the celebration metadata for an id, or null.</summary>
    HoursCelebration? FindCelebration(string id) => null;
}

/// <summary>Reads the single content file embedded in this assembly.</summary>
internal sealed class EmbeddedHoursTextProvider : IHoursTextProvider
{
    internal const string ResourcePrefix = "Respondeo.LiturgyOfTheHours.Content.";

    internal const string ResourceName = ResourcePrefix + "json";

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
        var assembly = typeof(EmbeddedHoursTextProvider).Assembly;
        var names = assembly.GetManifestResourceNames()
            .Where(name => name.StartsWith(ResourcePrefix, StringComparison.Ordinal)
                && name.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
                && !name.EndsWith(".schema.json", StringComparison.OrdinalIgnoreCase))
            .Order(StringComparer.Ordinal);

        return HoursContentFile.Merge(names.Select(name =>
        {
            using var stream = assembly.GetManifestResourceStream(name)!;
            return (name, Parse(stream));
        }).ToList());
    }

    /// <summary>Deserializes one content file with the loader's options.</summary>
    internal static HoursContentFile Parse(Stream stream) =>
        JsonSerializer.Deserialize<HoursContentFile>(stream, Options) ?? new HoursContentFile();

    /// <summary>Returns the celebration metadata for an id, or null.</summary>
    public HoursCelebration? FindCelebration(string id) =>
        Content.Value.Celebrations.TryGetValue(id, out var celebration) ? celebration : null;

    private static readonly JsonSerializerOptions Options = new()
    {
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        PropertyNameCaseInsensitive = true,
    };
}
