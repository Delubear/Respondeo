using System.Text.RegularExpressions;
using Respondeo.LiturgicalCalendar;

namespace Respondeo.LiturgyOfTheHours;

// ---------------------------------------------------------------------------
// Liturgy of the Hours. Public contracts returned by ILiturgyOfTheHours.
// The composition lives internally in the per-form engines (e.g. OrdinaryFormOffice).
// ---------------------------------------------------------------------------

/// <summary>The form (edition) of the Divine Office. Only the post-1970 form is implemented today.</summary>
public enum OfficeForm
{
    /// <summary>The Liturgy of the Hours as reformed after the Second Vatican Council (1970/1971).</summary>
    OrdinaryForm1970,
}

/// <summary>The hours this engine composes.</summary>
public enum Hour
{
    /// <summary>Morning Prayer (Lauds).</summary>
    MorningPrayer,

    /// <summary>Evening Prayer (Vespers).</summary>
    EveningPrayer,

    /// <summary>Night Prayer (Compline).</summary>
    NightPrayer,
}

/// <summary>Explicit participation in a liturgy that can replace an Hour on its service date.</summary>
public enum LiturgicalParticipation
{
    /// <summary>No participation has been supplied; compose the Hour normally.</summary>
    None,

    /// <summary>The evening Mass of the Lord's Supper on Holy Thursday.</summary>
    HolyThursdayEveningMass,

    /// <summary>The celebration of the Lord's Passion on Good Friday.</summary>
    GoodFridayPassion,

    /// <summary>The Easter Vigil on Holy Saturday.</summary>
    EasterVigil,

    /// <summary>The solemn Office of Readings vigil before the Midnight Mass of Christmas, not Mass attendance alone.</summary>
    ChristmasOfficeOfReadingsVigil,
}

/// <summary>A complete Sunday Compline schema, selectable during the Easter octave.</summary>
public enum ComplineSundayForm
{
    /// <summary>Keep the date's default schema: Sunday I after First Vespers, otherwise Sunday II.</summary>
    Automatic,

    /// <summary>The complete Sunday I schema, including Psalms 4 and 134.</summary>
    SundayI,

    /// <summary>The complete Sunday II schema, including Psalm 91.</summary>
    SundayII,
}

/// <summary>A weekday Compline psalm substitution (GILH 88), not a change to the complete Office schema.</summary>
public enum ComplinePsalmodyChoice
{
    /// <summary>Use the weekday's prescribed psalmody.</summary>
    Weekday,

    /// <summary>Substitute the Sunday I psalms, 4 and 134.</summary>
    SundayI,

    /// <summary>Substitute the Sunday II psalm, 91.</summary>
    SundayII,
}

/// <summary>A date-specific participation condition under which this Hour is not said separately.</summary>
public sealed record OfficeOmissionRule(LiturgicalParticipation Participation, string AttendanceLabel, string Explanation, string SourceLocator);

/// <summary>The kind of element a step represents within an hour.</summary>
public enum OfficeStepKind
{
    /// <summary>A rubric or instruction (e.g. "Make the Sign of the Cross").</summary>
    Rubric,

    /// <summary>A versicle and response, or another short exchange.</summary>
    Versicle,

    /// <summary>A hymn.</summary>
    Hymn,

    /// <summary>A psalm.</summary>
    Psalm,

    /// <summary>An Old or New Testament canticle used in place of a psalm.</summary>
    Canticle,

    /// <summary>A short reading from Scripture.</summary>
    Reading,

    /// <summary>The responsory after the reading.</summary>
    Responsory,

    /// <summary>A Gospel canticle (Benedictus, Magnificat, or Nunc Dimittis).</summary>
    GospelCanticle,

    /// <summary>An antiphon added as a distinct element, such as a restricted memorial antiphon.</summary>
    Antiphon,

    /// <summary>Intercessions or preces.</summary>
    Intercessions,

    /// <summary>The Lord's Prayer.</summary>
    LordsPrayer,

    /// <summary>The concluding prayer (collect).</summary>
    Prayer,

    /// <summary>An examination of conscience.</summary>
    Examen,

    /// <summary>The final antiphon to the Blessed Virgin Mary.</summary>
    MarianAntiphon,

    /// <summary>The concluding blessing or dismissal.</summary>
    Conclusion,
}

/// <summary>A Scripture reference for psalmody or a reading.</summary>
/// <param name="Citation">The citation using modern (Hebrew) numbering, e.g. "Psalm 63:2-9" or "Daniel 3:57-88, 56".</param>
/// <param name="DouayCitation">The equivalent Douay-Rheims (Vulgate) numbering for psalms, or <c>null</c> for canticles or where identical.</param>
public sealed partial record PsalmodyReference(string Citation, string? DouayCitation = null)
{
    /// <summary>Whether the citation requires an explicit a/b/c excerpt rather than whole-verse Bible resolution.</summary>
    public bool HasPartialVerses => PartialVerse().IsMatch(Citation);

    [GeneratedRegex(@"\d[abc]\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex PartialVerse();
}

/// <summary>One Scripture excerpt or fixed public-domain line of a liturgically arranged passage.</summary>
public sealed record OfficePassagePart(string? Citation = null, string? Text = null);

/// <summary>A complete passage, including its prescribed ending instead of an automatically appended Glory Be.</summary>
public sealed record OfficePassage(IReadOnlyList<OfficePassagePart> Parts, string Translation);

/// <summary>One element of an hour, presented as a single step in a walkthrough.</summary>
/// <param name="Kind">What sort of element this is.</param>
/// <param name="Title">A short heading for the step.</param>
public sealed record OfficeStep(OfficeStepKind Kind, string Title)
{
    /// <summary>Supplied text to pray, including public-domain, licensed, or explicitly marked sample content.</summary>
    public string? Text { get; init; }

    internal string? ContentElement { get; init; }

    /// <summary>The Scripture reference for psalms, canticles, and readings.</summary>
    public PsalmodyReference? Reference { get; init; }

    /// <summary>Explicit passage assembly where a bare Bible citation does not reproduce the liturgical form.</summary>
    public OfficePassage? Passage { get; init; }

    /// <summary>A short explanation of what this element is and why it is prayed here.</summary>
    public string? Explanation { get; init; }

    /// <summary>A missing-content notice directing the reader to OfficialSources; not the official-text preference.</summary>
    public bool UseOfficialText { get; init; }

    /// <summary>The antiphon said before and after a psalm or canticle, from the licensed (or sample) texts.</summary>
    public string? Antiphon { get; init; }

    /// <summary>True when supplied content comes from the non-sample provider and must carry the acknowledgment.</summary>
    public bool IsLicensedText { get; init; }

    /// <summary>True when supplied content is invented provider text, not an approved or licensed translation.</summary>
    public bool IsSampleText { get; init; }

    /// <summary>Whether this step or any option contains provider content requiring its source acknowledgment.</summary>
    public bool HasProviderText => IsLicensedText || IsSampleText || Options?.Any(option => option.HasProviderText) == true;

    /// <summary>
    /// The stanzas of a responsorial psalm, as canonical (modern-numbered) citations. The antiphon is repeated after each
    /// stanza and after the doxology. Each translation maps the citations to its own numbering, so stanzas are translation-agnostic.
    /// </summary>
    public IReadOnlyList<PsalmodyReference>? Stanzas { get; init; }

    /// <summary>Alternative elements the user may choose between, e.g. the Invitatory psalms.</summary>
    public IReadOnlyList<OfficeStep>? Options { get; init; }

    /// <summary>The usual opening ("O God, come to my assistance") used instead of the Invitatory when it was already prayed at another Hour.</summary>
    public string? IntroText { get; init; }
}

/// <summary>A link to an official or authoritative online source for the full texts of the hour.</summary>
/// <param name="Name">The source's display name.</param>
/// <param name="Url">The absolute URL for the requested date and hour.</param>
public sealed record OfficialSource(string Name, string Url);

/// <summary>A fully composed hour for a given date.</summary>
/// <param name="Date">The civil date the hour is prayed on.</param>
/// <param name="Hour">Which hour this is.</param>
/// <param name="Title">The display title, e.g. "Evening Prayer I of the Third Sunday of Advent".</param>
/// <param name="Day">The liturgical day whose office is being prayed (the following day for First Vespers).</param>
/// <param name="PsalterWeek">Calendar's background Psalter week (1-4) for Day; proper psalmody and Compline's weekly forms take precedence.</param>
/// <param name="Steps">The ordered steps of the hour.</param>
public sealed record Office(DateOnly Date, Hour Hour, string Title, LiturgicalDay Day, int PsalterWeek, IReadOnlyList<OfficeStep> Steps)
{
    /// <summary>True when this is First Vespers (or Night Prayer after First Vespers) of the following Sunday or solemnity.</summary>
    public bool IsFirstVespers { get; init; }

    /// <summary>The participation condition applicable to this service date and Hour, without assuming attendance.</summary>
    public OfficeOmissionRule? ConditionalOmission { get; init; }

    /// <summary>True only when explicit participation meets ConditionalOmission; Steps is then empty, not completed.</summary>
    public bool IsOmitted { get; init; }

    /// <summary>The resolved Sunday schema when either form is selectable; null outside Easter-octave Compline.</summary>
    public ComplineSundayForm? SelectedComplineForm { get; init; }

    /// <summary>The weekday psalm choice when substitution is available; null for other Hours, Sunday forms, or omitted Offices.</summary>
    public ComplinePsalmodyChoice? SelectedComplinePsalmody { get; init; }

    /// <summary>The explicitly selected contender's stable key, or null when no equal-precedence choice was supplied.</summary>
    public string? SelectedCelebrationCalendarKey { get; init; }

    /// <summary>The selected restricted memorial's stable calendar key, or null when no commemoration was requested.</summary>
    public string? SelectedMemorialCommemorationCalendarKey { get; init; }

    /// <summary>Links to official sources for the full copyrighted texts.</summary>
    public IReadOnlyList<OfficialSource> OfficialSources { get; init; } = [];

    /// <summary>The source acknowledgment for provider content actually used, including sample contributions in options.</summary>
    public string? Acknowledgment { get; init; }

    /// <summary>True when at least one step or option uses invented provider content rather than licensed text.</summary>
    public bool IsSampleContent { get; init; }
}

/// <summary>
/// A pure Liturgy of the Hours engine. It is deterministic: the caller supplies the date, so there is no hidden clock.
/// </summary>
public interface ILiturgyOfTheHours
{
    /// <summary>Composes the service date's Hour using the supplied choices and prefers matching provider text by default.</summary>
    /// <param name="memorialCommemorationCalendarKey">A restricted memorial to add after the collect, or null for the temporal Office alone.</param>
    /// <param name="celebrationCalendarKey">An explicit choice among unresolved equal-precedence contenders, or null for the calendar's provisional choice.</param>
    Office Compose(DateOnly date, Hour hour, OfficeForm form = OfficeForm.OrdinaryForm1970,
        LiturgicalParticipation participation = LiturgicalParticipation.None, ComplineSundayForm complineForm = ComplineSundayForm.Automatic,
        ComplinePsalmodyChoice complinePsalmody = ComplinePsalmodyChoice.Weekday, bool preferOfficialText = true,
        string? memorialCommemorationCalendarKey = null, string? celebrationCalendarKey = null);
}

/// <summary>Extension point for a single form (edition) of the Office.</summary>
internal interface IOfficeForm
{
    OfficeForm Form { get; }

    Office Compose(DateOnly date, Hour hour, LiturgicalParticipation participation = LiturgicalParticipation.None,
        ComplineSundayForm complineForm = ComplineSundayForm.Automatic, ComplinePsalmodyChoice complinePsalmody = ComplinePsalmodyChoice.Weekday,
        bool preferOfficialText = true, string? memorialCommemorationCalendarKey = null, string? celebrationCalendarKey = null);
}
