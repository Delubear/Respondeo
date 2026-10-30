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

/// <summary>A reference to a psalm or canticle in the psalter.</summary>
/// <param name="Citation">The citation using modern (Hebrew) numbering, e.g. "Psalm 63:2-9" or "Daniel 3:57-88, 56".</param>
/// <param name="DouayCitation">The equivalent Douay-Rheims (Vulgate) numbering for psalms, or <c>null</c> for canticles or where identical.</param>
public sealed record PsalmodyReference(string Citation, string? DouayCitation = null);

/// <summary>One element of an hour, presented as a single step in a walkthrough.</summary>
/// <param name="Kind">What sort of element this is.</param>
/// <param name="Title">A short heading for the step.</param>
public sealed record OfficeStep(OfficeStepKind Kind, string Title)
{
    /// <summary>Public-domain text to pray, when the engine can supply it. Null when the text must be read from an official source.</summary>
    public string? Text { get; init; }

    /// <summary>The Scripture reference for psalms, canticles, and readings.</summary>
    public PsalmodyReference? Reference { get; init; }

    /// <summary>A short explanation of what this element is and why it is prayed here.</summary>
    public string? Explanation { get; init; }

    /// <summary>True when the official, copyrighted text should be taken from <see cref="Office.OfficialSources"/>.</summary>
    public bool UseOfficialText { get; init; }

    /// <summary>The antiphon said before and after a psalm or canticle, from the licensed (or sample) texts.</summary>
    public string? Antiphon { get; init; }

    /// <summary>True when <see cref="Text"/> or <see cref="Antiphon"/> comes from the licensed texts and must carry the acknowledgment.</summary>
    public bool IsLicensedText { get; init; }
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
/// <param name="PsalterWeek">The week of the four-week psalter (1 to 4) in use.</param>
/// <param name="Steps">The ordered steps of the hour.</param>
public sealed record Office(DateOnly Date, Hour Hour, string Title, LiturgicalDay Day, int PsalterWeek, IReadOnlyList<OfficeStep> Steps)
{
    /// <summary>True when this is First Vespers (or Night Prayer after First Vespers) of the following Sunday or solemnity.</summary>
    public bool IsFirstVespers { get; init; }

    /// <summary>Links to official sources for the full copyrighted texts.</summary>
    public IReadOnlyList<OfficialSource> OfficialSources { get; init; } = [];

    /// <summary>The copyright acknowledgment required wherever licensed texts are shown.</summary>
    public string? Acknowledgment { get; init; }

    /// <summary>True when the proper texts are invented placeholders rather than the licensed translation.</summary>
    public bool IsSampleContent { get; init; }
}

/// <summary>
/// A pure Liturgy of the Hours engine. It is deterministic: the caller supplies the date, so there is no hidden clock.
/// </summary>
public interface ILiturgyOfTheHours
{
    /// <summary>Composes the given hour for the given date in the given form.</summary>
    Office Compose(DateOnly date, Hour hour, OfficeForm form = OfficeForm.OrdinaryForm1970);
}

/// <summary>Extension point for a single form (edition) of the Office.</summary>
internal interface IOfficeForm
{
    OfficeForm Form { get; }

    Office Compose(DateOnly date, Hour hour);
}
