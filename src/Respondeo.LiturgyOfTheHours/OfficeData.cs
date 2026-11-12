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
    internal const string SanctoralResource = "Respondeo.LiturgyOfTheHours.Data.Sanctoral.json";
    internal const string TemporalPsalmodyResource = "Respondeo.LiturgyOfTheHours.Data.TemporalPsalmody.json";

    private static readonly Lazy<PsalterData> LazyPsalter = new(() => Load<PsalterData>(PsalterResource));
    private static readonly Lazy<OrdinaryTextsData> LazyTexts = new(() => Load<OrdinaryTextsData>(TextsResource));
    private static readonly Lazy<HoursData> LazyHours = new(() => Load<HoursData>(HoursResource));

    public static PsalterData Psalter => Validated(LazyPsalter);

    public static OrdinaryTextsData Texts => Validated(LazyTexts);

    public static HoursData Hours => Validated(LazyHours);

    private static readonly Lazy<SanctoralData> LazySanctoral = new(() => Load<SanctoralData>(SanctoralResource));

    public static SanctoralData Sanctoral => Validated(LazySanctoral);

    private static readonly Lazy<TemporalPsalmodyData> LazyTemporalPsalmody = new(() =>
        Load<TemporalPsalmodyData>(TemporalPsalmodyResource).Validate(TemporalPsalmodyResource));

    public static TemporalPsalmodyData TemporalPsalmody => Validated(LazyTemporalPsalmody);

    private static readonly Lazy<bool> Validation = new(() =>
    {
        OfficeDataValidation.Validate(LazyTexts.Value, LazyHours.Value, LazySanctoral.Value);
        _ = LazyPsalter.Value;
        _ = LazyTemporalPsalmody.Value;
        return true;
    });

    private static T Validated<T>(Lazy<T> data)
    {
        _ = Validation.Value;
        return data.Value;
    }

    /// <summary>Joins text lines with newlines, expanding each <c>{key}</c> with the named text from ordinary-texts.json.</summary>
    /// <remarks>Breviary tokens such as <c>{{cross}}</c> are expanded first so they are never mistaken for text references.</remarks>
    public static string ResolveText(IEnumerable<string> lines) =>
        TextReference().Replace(BreviaryText.ExpandTokens(string.Join('\n', lines)), match => Text(match.Groups[1].Value));

    /// <summary>The named fixed text, with any embedded texts expanded.</summary>
    public static string Text(string key) => Texts.Texts.TryGetValue(key, out var lines) ? ResolveText(lines) : throw new KeyNotFoundException($"Unknown ordinary text '{key}'.");

    private static T Load<T>(string resource)
    {
        using var stream = typeof(OfficeData).Assembly.GetManifestResourceStream(resource) ?? throw new InvalidOperationException($"Embedded resource '{resource}' was not found.");
        return OfficeJson.Read<T>(stream, resource);
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

/// <summary>Short-reading citations and explicit calendar-identity bindings to the existing Hours content IDs.</summary>
internal sealed record SanctoralData(
    Dictionary<string, SanctoralReadings> Commons,
    Dictionary<string, SanctoralReadings> Celebrations,
    Dictionary<string, string> CalendarBindings);

/// <param name="EveningPsalmody">Proper Evening Prayer psalmody, including memorials; Common lists apply only to feasts and solemnities.</param>
/// <param name="MorningPsalmody">Proper Morning Prayer psalmody, including memorials that replace the weekday psalter.</param>
/// <param name="FirstEveningCanticle">A prescribed First Vespers canticle; does not certify the still-unreviewed First Vespers psalms.</param>
internal sealed record SanctoralReadings(
    string? Morning = null,
    string? Evening = null,
    string? Common = null,
    string[]? EveningPsalmody = null,
    string[]? MorningPsalmody = null,
    string? FirstEveningCanticle = null);

internal sealed record OrdinaryTextsData(
    Dictionary<string, string[]> Texts,
    Dictionary<string, string[]> NightConcludingPrayers,
    IReadOnlyList<MarianAntiphonRule> MarianAntiphons,
    Dictionary<string, OfficePassage> PsalmodyPassages,
    Dictionary<string, OfficePassage> ReadingPassages);

/// <summary>A Marian antiphon and when it is used. A rule with no conditions always matches.</summary>
internal sealed record MarianAntiphonRule(string Title, string Text)
{
    public LiturgicalSeason[]? Seasons { get; init; }

    public int[]? Months { get; init; }

    /// <summary>Month-day pairs, e.g. <c>02-01</c>.</summary>
    public string[]? Dates { get; init; }

    public bool Matches(DateOnly date, LiturgicalSeason season)
    {
        if (Seasons is null && Months is null && Dates is null)
        {
            return true;
        }

        return (Seasons?.Contains(season) ?? false)
            || (Months?.Contains(date.Month) ?? false)
            || (Dates?.Contains(date.ToString("MM-dd", System.Globalization.CultureInfo.InvariantCulture)) ?? false);
    }
}

internal sealed record HoursData(
    Dictionary<string, string> Titles,
    string PsalmodyExplanation,
    InvitatoryData Invitatory,
    IReadOnlyList<OfficialSourceTemplate> OfficialSources,
    Dictionary<Hour, HourTemplate> Hours,
    Dictionary<LiturgicalParticipation, OfficeOmissionRule> ParticipationOmissions);

internal sealed record InvitatoryData(IReadOnlyList<InvitatoryPsalmData> Psalms);

internal sealed record InvitatoryPsalmData(string Citation, string Explanation, string[] Stanzas);

internal sealed record OfficialSourceTemplate(string Name, string Url);

internal sealed record HourTemplate(string Page, IReadOnlyList<StepTemplate> Steps);

/// <summary>How a step's content is supplied.</summary>
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

    [JsonStringEnumMemberName("night-responsory")]
    NightResponsory,

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

    public string[]? IntroText { get; init; }

    public string? Reference { get; init; }

    public string? Antiphon { get; init; }

    public bool Invitatory { get; init; }

    public string? CelebrationExplanation { get; init; }

    public string? FallbackExplanation { get; init; }

    public Dictionary<LiturgicalSeason, SeasonOverride>? Seasons { get; init; }

    /// <summary>Applies the override for the given season, if any.</summary>
    public StepTemplate ForSeason(LiturgicalSeason season) =>
        Seasons is not null && Seasons.TryGetValue(season, out var o)
            ? this with { Text = o.Text ?? Text, IntroText = o.IntroText ?? IntroText, Explanation = o.Explanation ?? Explanation }
            : this;
}

internal sealed record SeasonOverride(string[]? Text = null, string? Explanation = null, string[]? IntroText = null);
