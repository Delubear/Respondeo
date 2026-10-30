using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Respondeo.LiturgicalCalendar;

namespace Respondeo.LiturgyOfTheHours;

/// <summary>
/// The data that drives the Office, loaded once from the JSON files embedded in this assembly:
/// the psalter (<c>Data/psalter.json</c>), the fixed texts (<c>Data/ordinary-texts.json</c>), and the hour templates (<c>Data/hours.json</c>).
/// </summary>
internal static partial class OfficeData
{
    internal const string PsalterResource = "Respondeo.LiturgyOfTheHours.Data.Psalter.json";
    internal const string TextsResource = "Respondeo.LiturgyOfTheHours.Data.OrdinaryTexts.json";
    internal const string HoursResource = "Respondeo.LiturgyOfTheHours.Data.Hours.json";

    private static readonly JsonSerializerOptions Options = new()
    {
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private static readonly Lazy<PsalterData> LazyPsalter = new(() => Load<PsalterData>(PsalterResource));
    private static readonly Lazy<OrdinaryTextsData> LazyTexts = new(() => Load<OrdinaryTextsData>(TextsResource));
    private static readonly Lazy<HoursData> LazyHours = new(() => Load<HoursData>(HoursResource));

    public static PsalterData Psalter => LazyPsalter.Value;

    public static OrdinaryTextsData Texts => LazyTexts.Value;

    public static HoursData Hours => LazyHours.Value;

    /// <summary>Joins text lines with newlines, expanding each <c>{key}</c> with the named text from ordinary-texts.json.</summary>
    public static string ResolveText(IEnumerable<string> lines) => TextReference().Replace(string.Join('\n', lines), match => Text(match.Groups[1].Value));

    /// <summary>The named fixed text, with any embedded texts expanded.</summary>
    public static string Text(string key) => Texts.Texts.TryGetValue(key, out var lines) ? ResolveText(lines) : throw new KeyNotFoundException($"Unknown ordinary text '{key}'.");

    private static T Load<T>(string resource)
    {
        using var stream = typeof(OfficeData).Assembly.GetManifestResourceStream(resource) ?? throw new InvalidOperationException($"Embedded resource '{resource}' was not found.");
        return JsonSerializer.Deserialize<T>(stream, Options) ?? throw new InvalidOperationException($"Embedded resource '{resource}' is empty.");
    }

    [GeneratedRegex(@"\{([a-z0-9-]+)\}")]
    private static partial Regex TextReference();
}

/// <summary>The four-week psalter: citations keyed by week (index 0-3) and lower-case day name.</summary>
internal sealed record PsalterData(
    IReadOnlyList<Dictionary<string, string[]>> Morning,
    IReadOnlyList<Dictionary<string, string[]>> Evening,
    Dictionary<string, NightPsalmodyData> Night,
    IReadOnlyList<Dictionary<string, string>>? MorningReadings = null,
    IReadOnlyList<Dictionary<string, string>>? EveningReadings = null);

internal sealed record NightPsalmodyData(string[] Psalms, string Reading);

internal sealed record OrdinaryTextsData(Dictionary<string, string[]> Texts, Dictionary<string, string[]> NightConcludingPrayers, IReadOnlyList<MarianAntiphonRule> MarianAntiphons);

/// <summary>A Marian antiphon and when it is used. A rule with no conditions always matches.</summary>
internal sealed record MarianAntiphonRule(string Title, string Text)
{
    public LiturgicalSeason[]? Seasons { get; init; }

    public int[]? Months { get; init; }

    /// <summary>Month-day pairs, e.g. <c>02-01</c>.</summary>
    public string[]? Dates { get; init; }

    public bool Matches(LiturgicalDay day)
    {
        if (Seasons is null && Months is null && Dates is null)
        {
            return true;
        }

        return (Seasons?.Contains(day.Season) ?? false)
            || (Months?.Contains(day.Date.Month) ?? false)
            || (Dates?.Contains(day.Date.ToString("MM-dd", System.Globalization.CultureInfo.InvariantCulture)) ?? false);
    }
}

internal sealed record HoursData(
    Dictionary<string, string> Titles,
    string PsalmodyExplanation,
    InvitatoryData Invitatory,
    IReadOnlyList<OfficialSourceTemplate> OfficialSources,
    Dictionary<Hour, HourTemplate> Hours);

internal sealed record InvitatoryData(string Antiphon, IReadOnlyList<InvitatoryPsalmData> Psalms);

internal sealed record InvitatoryPsalmData(string Citation, string Explanation, string[] Stanzas);

internal sealed record OfficialSourceTemplate(string Name, string Url);

internal sealed record HourTemplate(string Page, IReadOnlyList<StepTemplate> Steps);

/// <summary>How a step's content is supplied.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<StepSource>))]
internal enum StepSource
{
    [JsonStringEnumMemberName("fixed")]
    Fixed,

    [JsonStringEnumMemberName("proper")]
    Proper,

    [JsonStringEnumMemberName("psalmody")]
    Psalmody,

    [JsonStringEnumMemberName("collect")]
    Collect,

    [JsonStringEnumMemberName("short-reading")]
    ShortReading,

    [JsonStringEnumMemberName("night-reading")]
    NightReading,

    [JsonStringEnumMemberName("night-prayer")]
    NightPrayer,

    [JsonStringEnumMemberName("marian-antiphon")]
    MarianAntiphon,
}

/// <summary>One step of an hour template. See the header of <c>Data/hours.json</c> for the meaning of each source.</summary>
internal sealed record StepTemplate
{
    public OfficeStepKind Kind { get; init; }

    public string Title { get; init; } = string.Empty;

    public StepSource Source { get; init; }

    public string? Explanation { get; init; }

    public string[]? Text { get; init; }

    public string? Reference { get; init; }

    public string? Antiphon { get; init; }

    public bool Invitatory { get; init; }

    public string? CelebrationExplanation { get; init; }

    public string? FallbackExplanation { get; init; }

    public Dictionary<LiturgicalSeason, SeasonOverride>? Seasons { get; init; }

    /// <summary>Applies the override for the given season, if any.</summary>
    public StepTemplate ForSeason(LiturgicalSeason season) =>
        Seasons is not null && Seasons.TryGetValue(season, out var o)
            ? this with { Text = o.Text ?? Text, Explanation = o.Explanation ?? Explanation }
            : this;
}

internal sealed record SeasonOverride(string[]? Text, string? Explanation);
