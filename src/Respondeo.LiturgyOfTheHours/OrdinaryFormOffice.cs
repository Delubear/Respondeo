using Respondeo.LiturgicalCalendar;

namespace Respondeo.LiturgyOfTheHours;

/// <summary>
/// Composes Morning, Evening, and Night Prayer of the post-1970 Liturgy of the Hours.
/// Precedence follows the General Instruction of the Liturgy of the Hours:
/// solemnities and feasts take the Sunday psalmody of Week I at Morning Prayer, memorials and ferias take the psalmody of the current psalter week,
/// Sundays and solemnities begin with First Vespers the evening before, and Night Prayer follows a weekly cycle (with the Sunday forms after either Vespers).
/// </summary>
internal sealed class OrdinaryFormOffice(ILiturgicalCalendar calendar, IHoursTextProvider texts) : IOfficeForm
{
    public OfficeForm Form => OfficeForm.OrdinaryForm1970;

    public Office Compose(DateOnly date, Hour hour)
    {
        var today = calendar.ForDate(date);
        var tomorrow = calendar.ForDate(date.AddDays(1));
        var firstVespers = hour != Hour.MorningPrayer && BeginsWithFirstVespers(today, tomorrow);
        var day = firstVespers ? tomorrow : today;
        var week = PsalterWeek.For(today.Date, today.Season, calendar.Options?.Region == CalendarRegion.UnitedStates);

        var steps = hour switch
        {
            Hour.MorningPrayer => MorningPrayer(today, week),
            Hour.EveningPrayer => EveningPrayer(today, tomorrow, week, firstVespers),
            _ => NightPrayer(today, firstVespers),
        };

        return new Office(date, hour, Title(hour, day, firstVespers), day, week, ApplyLicensedTexts(steps, hour, day, week))
        {
            IsFirstVespers = firstVespers,
            OfficialSources = Sources(date, hour),
            Acknowledgment = texts.Acknowledgment,
            IsSampleContent = texts.IsSample,
        };
    }

    /// <summary>Fills proper elements from the licensed (or sample) texts, most specific key first.</summary>
    private List<OfficeStep> ApplyLicensedTexts(List<OfficeStep> steps, Hour hour, LiturgicalDay day, int week)
    {
        var hourKey = hour switch { Hour.MorningPrayer => "morning", Hour.EveningPrayer => "evening", _ => "night" };
        var prefix = $"{day.Season.ToString().ToLowerInvariant()}/w{week}/{day.Date.DayOfWeek.ToString().ToLowerInvariant()}/{hourKey}";
        var psalm = 0;
        var result = new List<OfficeStep>(steps.Count);

        foreach (var step in steps)
        {
            var element = step.Kind switch
            {
                OfficeStepKind.Psalm or OfficeStepKind.Canticle => $"psalm-antiphon-{++psalm}",
                OfficeStepKind.Hymn => "hymn",
                OfficeStepKind.Reading => "reading",
                OfficeStepKind.Responsory when step.Text is null => "responsory",
                OfficeStepKind.GospelCanticle when hour != Hour.NightPrayer => "gospel-canticle",
                OfficeStepKind.Intercessions => "intercessions",
                OfficeStepKind.Prayer when step.Text is null && hour != Hour.NightPrayer => "collect",
                _ => null,
            };

            var licensed = element is null ? null : texts.Find([$"{prefix}/{element}", $"{hourKey}/{element}", element]);
            if (licensed is null)
            {
                result.Add(step);
                continue;
            }

            result.Add(step with
            {
                Text = string.IsNullOrEmpty(licensed.Text) ? step.Text : licensed.Text,
                Antiphon = licensed.Antiphon ?? step.Antiphon,
                UseOfficialText = false,
                IsLicensedText = true,
            });
        }

        return result;
    }

    internal static bool BeginsWithFirstVespers(LiturgicalDay today, LiturgicalDay tomorrow)
    {
        if (today.Season == LiturgicalSeason.Triduum || IsSolemnity(today))
        {
            return false;
        }

        return tomorrow.Date.DayOfWeek == DayOfWeek.Sunday || IsSolemnity(tomorrow);
    }

    private static bool IsSolemnity(LiturgicalDay day) => day.Celebration?.Rank == CelebrationRank.Solemnity;

    private static bool IsFestal(LiturgicalDay day) => day.Celebration?.Rank is CelebrationRank.Solemnity or CelebrationRank.Feast;

    private static string DayName(LiturgicalDay day) =>
        day.Celebration?.Name ?? day.FerialName ?? day.Date.ToString("dddd, MMMM d, yyyy", System.Globalization.CultureInfo.InvariantCulture);

    private static string Title(Hour hour, LiturgicalDay day, bool firstVespers) => hour switch
    {
        Hour.MorningPrayer => $"Morning Prayer: {DayName(day)}",
        Hour.EveningPrayer => firstVespers ? $"Evening Prayer I: {DayName(day)}" : $"Evening Prayer: {DayName(day)}",
        _ => firstVespers ? $"Night Prayer after Evening Prayer I: {DayName(day)}" : $"Night Prayer: {DayName(day)}",
    };

    private static List<OfficeStep> MorningPrayer(LiturgicalDay day, int week)
    {
        var psalmody = IsFestal(day) || day.Date.DayOfWeek == DayOfWeek.Sunday
            ? Psalter.MorningPsalmody(IsFestal(day) ? 1 : week, DayOfWeek.Sunday)
            : Psalter.MorningPsalmody(week, day.Date.DayOfWeek);

        var steps = new List<OfficeStep>
        {
            new(OfficeStepKind.Versicle, "Introductory Verse")
            {
                Text = "V. Lord, open my lips.\nR. And my mouth will proclaim your praise.",
                Explanation = "When Morning Prayer is the first hour of the day, it opens with the Invitatory (Psalm 95, 100, 67, or 24) instead of \"O God, come to my assistance.\" Expand a psalm below to view it.",
                Options = InvitatoryPsalms(),
            },
            Hymn("Morning Prayer"),
        };

        steps.AddRange(Psalmody(psalmody));
        steps.Add(ProperStep(OfficeStepKind.Reading, "Reading", "A short passage of Scripture proper to the day or season."));
        steps.Add(ProperStep(OfficeStepKind.Responsory, "Responsory", "A brief responsory that echoes the reading."));
        steps.Add(new OfficeStep(OfficeStepKind.GospelCanticle, "Canticle of Zechariah (Benedictus)")
        {
            Reference = new PsalmodyReference("Luke 1:68-79"),
            Text = OrdinaryTexts.Benedictus,
            Explanation = "Zechariah's song at the birth of John the Baptist greets Christ, the dawn from on high. Pray it with the antiphon proper to the day, making the Sign of the Cross at the start.",
        });
        steps.Add(ProperStep(OfficeStepKind.Intercessions, "Intercessions", "Morning intercessions consecrate the day and its work to God."));
        steps.Add(new OfficeStep(OfficeStepKind.LordsPrayer, "The Lord's Prayer") { Text = OrdinaryTexts.LordsPrayer });
        steps.Add(Collect(day));
        steps.Add(new OfficeStep(OfficeStepKind.Conclusion, "Dismissal") { Text = OrdinaryTexts.Dismissal });

        return steps;
    }

    private static List<OfficeStep> EveningPrayer(LiturgicalDay today, LiturgicalDay tomorrow, int week, bool firstVespers)
    {
        IReadOnlyList<string> psalmody;

        if (firstVespers)
        {
            psalmody = Psalter.EveningPsalmody(IsSolemnity(tomorrow) ? 1 : NextSundayWeek(today, week), DayOfWeek.Saturday);
        }
        else
        {
            psalmody = IsFestal(today) || today.Date.DayOfWeek == DayOfWeek.Sunday
                ? Psalter.EveningPsalmody(IsFestal(today) ? 1 : week, DayOfWeek.Sunday)
                : Psalter.EveningPsalmody(week, today.Date.DayOfWeek);
        }

        var day = firstVespers ? tomorrow : today;
        var steps = new List<OfficeStep> { Opening(), Hymn("Evening Prayer") };

        steps.AddRange(Psalmody(psalmody));
        steps.Add(ProperStep(OfficeStepKind.Reading, "Reading", "A short passage of Scripture proper to the day or season."));
        steps.Add(ProperStep(OfficeStepKind.Responsory, "Responsory", "A brief responsory that echoes the reading."));
        steps.Add(new OfficeStep(OfficeStepKind.GospelCanticle, "Canticle of Mary (Magnificat)")
        {
            Reference = new PsalmodyReference("Luke 1:46-55"),
            Text = OrdinaryTexts.Magnificat,
            Explanation = "Our Lady's song of praise is the high point of Evening Prayer, giving thanks for the salvation God has worked. Pray it with the antiphon proper to the day, making the Sign of the Cross at the start.",
        });
        steps.Add(ProperStep(OfficeStepKind.Intercessions, "Intercessions", "Evening intercessions; the last petition is always for the faithful departed."));
        steps.Add(new OfficeStep(OfficeStepKind.LordsPrayer, "The Lord's Prayer") { Text = OrdinaryTexts.LordsPrayer });
        steps.Add(Collect(day));
        steps.Add(new OfficeStep(OfficeStepKind.Conclusion, "Dismissal") { Text = OrdinaryTexts.Dismissal });

        return steps;
    }

    private static List<OfficeStep> NightPrayer(LiturgicalDay today, bool firstVespers)
    {
        var cycleDay = firstVespers
            ? DayOfWeek.Saturday
            : IsSolemnity(today) || today.Date.DayOfWeek == DayOfWeek.Sunday || today.Season == LiturgicalSeason.Triduum
                ? DayOfWeek.Sunday
                : today.Date.DayOfWeek;
        var (psalms, reading) = Psalter.NightPsalmody(cycleDay);
        var easter = today.Season == LiturgicalSeason.Easter;

        var steps = new List<OfficeStep>
        {
            Opening(),
            new(OfficeStepKind.Examen, "Examination of Conscience")
            {
                Text = OrdinaryTexts.Examen,
                Explanation = "Night Prayer begins by looking back over the day in God's presence and asking forgiveness before sleep.",
            },
            Hymn("Night Prayer"),
        };

        steps.AddRange(Psalmody(psalms));
        steps.Add(new OfficeStep(OfficeStepKind.Reading, "Reading")
        {
            Reference = new PsalmodyReference(reading),
            UseOfficialText = true,
            Explanation = "A brief reading fixed for this night of the week.",
        });
        steps.Add(new OfficeStep(OfficeStepKind.Responsory, "Responsory")
        {
            Text = easter ? OrdinaryTexts.EasterNightResponsory : OrdinaryTexts.NightResponsory,
            Explanation = easter
                ? "In Easter Time the responsory gives way to the Easter verse from Psalm 118 (117), celebrating the day of the Resurrection."
                : "Christ's own words from the Cross (Luke 23:46; Psalm 31:6), entrusting ourselves to the Father as we go to rest.",
        });
        steps.Add(new OfficeStep(OfficeStepKind.GospelCanticle, "Canticle of Simeon (Nunc Dimittis)")
        {
            Reference = new PsalmodyReference("Luke 2:29-32"),
            Text = $"Antiphon: {OrdinaryTexts.NightAntiphon}\n\n{OrdinaryTexts.NuncDimittis}\n\nAntiphon: {OrdinaryTexts.NightAntiphon}",
            Explanation = "Simeon, having seen the Christ, is ready to depart in peace; so we entrust our sleep, and one day our death, to the Lord.",
        });
        steps.Add(OrdinaryTexts.NightConcludingPrayers.TryGetValue(cycleDay, out var prayer)
            ? new OfficeStep(OfficeStepKind.Prayer, "Concluding Prayer")
            {
                Text = prayer,
                Explanation = "The prayer fixed for this night of the week, given in the Latin of the typical edition.",
            }
            : ProperStep(OfficeStepKind.Prayer, "Concluding Prayer", "A short prayer fixed for this night of the week."));
        steps.Add(new OfficeStep(OfficeStepKind.Conclusion, "Blessing") { Text = OrdinaryTexts.NightBlessing });
        steps.Add(MarianAntiphon(today));

        return steps;
    }

    private static int NextSundayWeek(LiturgicalDay today, int week)
    {
        var sunday = today.Date.AddDays(1);

        return sunday.DayOfWeek == DayOfWeek.Sunday
            && (sunday == LiturgicalDates.FirstSundayOfAdvent(sunday.Year) || sunday == LiturgicalDates.FirstSundayOfLent(sunday.Year))
            ? 1
            : (week % 4) + 1;
    }

    private static OfficeStep Opening() => new(OfficeStepKind.Versicle, "Introductory Verse")
    {
        Text = $"{OrdinaryTexts.Invitation}\n\n{OrdinaryTexts.AlleluiaNote}",
        Explanation = "Make the Sign of the Cross. Each hour opens by asking God's help to pray (Psalm 70:2).",
    };

    private static IReadOnlyList<OfficeStep> InvitatoryPsalms() =>
    [
        InvitatoryPsalm("Psalm 95", "The customary Invitatory psalm, a call to praise and to hear God's voice today."),
        InvitatoryPsalm("Psalm 100", "May replace Psalm 95: all the earth is called to serve the Lord with gladness."),
        InvitatoryPsalm("Psalm 67", "May replace Psalm 95: a prayer that all the nations may praise God."),
        InvitatoryPsalm("Psalm 24", "May replace Psalm 95: the King of glory enters his holy place."),
    ];

    /// <summary>Stanza verse ranges (modern numbering) for each invitatory psalm, so any translation can supply its own text.</summary>
    internal static readonly IReadOnlyDictionary<string, string[]> InvitatoryStanzaRanges = new Dictionary<string, string[]>
    {
        ["Psalm 95"] = ["1-2", "3-5", "6-7", "8-9", "10-11"],
        ["Psalm 100"] = ["1-2", "3", "4", "5"],
        ["Psalm 67"] = ["2-3", "4-5", "6-7", "8"],
        ["Psalm 24"] = ["1-2", "3-4", "5-6", "7-8", "9-10"],
    };

    private static PsalmodyReference[]? InvitatoryStanzas(string citation) =>
        InvitatoryStanzaRanges.TryGetValue(citation, out var ranges)
            ? ranges.Select(range => $"{citation}:{range}").Select(c => new PsalmodyReference(c, Psalter.DouayCitation(c))).ToArray()
            : null;

    private static OfficeStep InvitatoryPsalm(string citation, string explanation) => new(OfficeStepKind.Psalm, citation)
    {
        Reference = new PsalmodyReference(citation, Psalter.DouayCitation(citation)),
        Stanzas = InvitatoryStanzas(citation),
        UseOfficialText = true,
        Explanation = explanation,
        Antiphon = "Come, let us worship the Lord, the King who is to come. (Sample antiphon; use the approved text of the day.)",
    };

    private static OfficeStep Hymn(string hour) => ProperStep(OfficeStepKind.Hymn, "Hymn", $"A hymn suited to {hour} and to the day or season.");

    private static OfficeStep Collect(LiturgicalDay day) => ProperStep(
        OfficeStepKind.Prayer,
        "Concluding Prayer",
        day.Celebration is { } celebration
            ? $"The collect proper to {celebration.Name}."
            : "The collect proper to the day, or of the psalter week in Ordinary Time.");

    private static OfficeStep ProperStep(OfficeStepKind kind, string title, string explanation) => new(kind, title)
    {
        UseOfficialText = true,
        Explanation = explanation,
    };

    private static IEnumerable<OfficeStep> Psalmody(IEnumerable<string> citations)
    {
        var index = 1;

        foreach (var citation in citations)
        {
            var canticle = Psalter.IsCanticle(citation);

            yield return new OfficeStep(canticle ? OfficeStepKind.Canticle : OfficeStepKind.Psalm, canticle ? $"Canticle: {citation}" : citation)
            {
                Reference = new PsalmodyReference(citation, Psalter.DouayCitation(citation)),
                UseOfficialText = true,
                Explanation = $"Psalmody {index++}: pray with its antiphon before and after, concluding with the Glory Be.",
            };
        }
    }

    private static OfficeStep MarianAntiphon(LiturgicalDay day)
    {
        var date = day.Date;
        var (title, text) = day.Season switch
        {
            LiturgicalSeason.Easter => ("Regina Caeli", OrdinaryTexts.ReginaCaeli),
            LiturgicalSeason.Advent => ("Alma Redemptoris Mater", OrdinaryTexts.AlmaRedemptoris),
            LiturgicalSeason.Christmas => ("Alma Redemptoris Mater", OrdinaryTexts.AlmaRedemptoris),
            LiturgicalSeason.Lent => ("Ave Regina Caelorum", OrdinaryTexts.AveReginaCaelorum),
            _ when date.Month == 1 || (date.Month == 2 && date.Day == 1) => ("Alma Redemptoris Mater", OrdinaryTexts.AlmaRedemptoris),
            _ => ("Salve Regina", OrdinaryTexts.SalveRegina),
        };

        return new OfficeStep(OfficeStepKind.MarianAntiphon, title)
        {
            Text = text,
            Explanation = "The day ends with an antiphon to Our Lady. Any of the Marian antiphons may be used; this one is traditional for the season.",
        };
    }

    private static IReadOnlyList<OfficialSource> Sources(DateOnly date, Hour hour)
    {
        var page = hour switch
        {
            Hour.MorningPrayer => "lauds",
            Hour.EveningPrayer => "vespers",
            _ => "comp",
        };

        return
        [
            new OfficialSource("Universalis", $"https://universalis.com/{date:yyyyMMdd}/{page}.htm"),
            new OfficialSource("Divine Office", "https://divineoffice.org/"),
            new OfficialSource("iBreviary", "https://www.ibreviary.com/m2/breviario.php"),
        ];
    }
}
