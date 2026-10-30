using System.Globalization;
using Respondeo.LiturgicalCalendar;

namespace Respondeo.LiturgyOfTheHours;

/// <summary>
/// Composes Morning, Evening, and Night Prayer of the post-1970 Liturgy of the Hours.
/// Precedence follows the General Instruction of the Liturgy of the Hours:
/// solemnities and feasts take the Sunday psalmody of Week I at Morning Prayer, memorials and ferias take the psalmody of the current psalter week,
/// Sundays and solemnities begin with First Vespers the evening before, and Night Prayer follows a weekly cycle (with the Sunday forms after either Vespers).
/// The order, titles, explanations, and texts of each hour come from the embedded data files (see <see cref="OfficeData"/>); only the rules live here.
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

		var context = new HourContext(hour, today, day, firstVespers, NightCycleDay(today, firstVespers));
		var psalmody = hour switch
		{
			Hour.MorningPrayer => MorningPsalmody(today, week),
			Hour.EveningPrayer => EveningPsalmody(today, tomorrow, week, firstVespers),
			_ => Psalter.NightPsalmody(context.NightCycleDay).Psalms,
		};

		var steps = BuildSteps(OfficeData.Hours.Hours[hour], context, psalmody);

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
		day.Celebration?.Name ?? day.FerialName ?? day.Date.ToString("dddd, MMMM d, yyyy", CultureInfo.InvariantCulture);

	private static string Title(Hour hour, LiturgicalDay day, bool firstVespers)
	{
		var key = firstVespers && hour != Hour.MorningPrayer ? $"{hour}FirstVespers" : hour.ToString();
		return OfficeData.Hours.Titles[key].Replace("{day}", DayName(day), StringComparison.Ordinal);
	}

	private static IReadOnlyList<string> MorningPsalmody(LiturgicalDay day, int week) =>
		IsFestal(day) || day.Date.DayOfWeek == DayOfWeek.Sunday
			? Psalter.MorningPsalmody(IsFestal(day) ? 1 : week, DayOfWeek.Sunday)
			: Psalter.MorningPsalmody(week, day.Date.DayOfWeek);

	private static IReadOnlyList<string> EveningPsalmody(LiturgicalDay today, LiturgicalDay tomorrow, int week, bool firstVespers)
	{
		if (firstVespers)
		{
			return Psalter.EveningPsalmody(IsSolemnity(tomorrow) ? 1 : NextSundayWeek(today, week), DayOfWeek.Saturday);
		}

		return IsFestal(today) || today.Date.DayOfWeek == DayOfWeek.Sunday
			? Psalter.EveningPsalmody(IsFestal(today) ? 1 : week, DayOfWeek.Sunday)
			: Psalter.EveningPsalmody(week, today.Date.DayOfWeek);
	}

	/// <summary>The night of the weekly Night Prayer cycle: Saturday after First Vespers, Sunday on Sundays, solemnities, and the Triduum.</summary>
	private static DayOfWeek NightCycleDay(LiturgicalDay today, bool firstVespers) =>
		firstVespers
			? DayOfWeek.Saturday
			: IsSolemnity(today) || today.Date.DayOfWeek == DayOfWeek.Sunday || today.Season == LiturgicalSeason.Triduum
				? DayOfWeek.Sunday
				: today.Date.DayOfWeek;

	private static int NextSundayWeek(LiturgicalDay today, int week)
	{
		var sunday = today.Date.AddDays(1);

		return sunday.DayOfWeek == DayOfWeek.Sunday
			&& (sunday == LiturgicalDates.FirstSundayOfAdvent(sunday.Year) || sunday == LiturgicalDates.FirstSundayOfLent(sunday.Year))
			? 1
			: (week % 4) + 1;
	}

	/// <summary>The facts about the day a template needs: the hour, the civil day, the day whose office is prayed, and the night cycle.</summary>
	private sealed record HourContext(Hour Hour, LiturgicalDay Today, LiturgicalDay Day, bool FirstVespers, DayOfWeek NightCycleDay);

	private static List<OfficeStep> BuildSteps(HourTemplate template, HourContext context, IReadOnlyList<string> psalmody)
	{
		var steps = new List<OfficeStep>();

		foreach (var raw in template.Steps)
		{
			var step = raw.ForSeason(context.Today.Season);

			switch (step.Source)
			{
				case StepSource.Psalmody:
					steps.AddRange(Psalmody(psalmody));
					break;
				case StepSource.Fixed:
					steps.Add(new OfficeStep(step.Kind, step.Title)
					{
						Text = OfficeData.ResolveText(step.Text ?? []),
						Reference = step.Reference is null ? null : new PsalmodyReference(step.Reference),
						Explanation = step.Explanation,
						Options = step.Invitatory ? InvitatoryPsalms() : null,
					});
					break;
				case StepSource.Proper:
					steps.Add(ProperStep(step.Kind, step.Title, step.Explanation));
					break;
				case StepSource.Collect:
					steps.Add(ProperStep(
						step.Kind,
						step.Title,
						context.Day.Celebration is { } celebration && step.CelebrationExplanation is { } format
							? format.Replace("{celebration}", celebration.Name, StringComparison.Ordinal)
							: step.Explanation));
					break;
				case StepSource.NightReading:
					steps.Add(new OfficeStep(step.Kind, step.Title)
					{
						Reference = new PsalmodyReference(Psalter.NightPsalmody(context.NightCycleDay).Reading),
						UseOfficialText = true,
						Explanation = step.Explanation,
					});
					break;
				case StepSource.NightPrayer:
					steps.Add(OfficeData.Texts.NightConcludingPrayers.TryGetValue(Psalter.Key(context.NightCycleDay), out var prayer)
						? new OfficeStep(step.Kind, step.Title) { Text = OfficeData.ResolveText(prayer), Explanation = step.Explanation }
						: ProperStep(step.Kind, step.Title, step.FallbackExplanation ?? step.Explanation));
					break;
				case StepSource.MarianAntiphon:
					var rule = OfficeData.Texts.MarianAntiphons.First(r => r.Matches(context.Today));
					steps.Add(new OfficeStep(step.Kind, rule.Title) { Text = OfficeData.Text(rule.Text), Explanation = step.Explanation });
					break;
				default:
					throw new InvalidOperationException($"Unsupported step source '{step.Source}'.");
			}
		}

		return steps;
	}

	private static IReadOnlyList<OfficeStep> InvitatoryPsalms()
	{
		var invitatory = OfficeData.Hours.Invitatory;

		return invitatory.Psalms
			.Select(psalm => new OfficeStep(OfficeStepKind.Psalm, psalm.Citation)
			{
				Reference = new PsalmodyReference(psalm.Citation, Psalter.DouayCitation(psalm.Citation)),
				Stanzas = psalm.Stanzas.Select(range => $"{psalm.Citation}:{range}").Select(c => new PsalmodyReference(c, Psalter.DouayCitation(c))).ToArray(),
				UseOfficialText = true,
				Explanation = psalm.Explanation,
				Antiphon = invitatory.Antiphon,
			})
			.ToArray();
	}

	private static OfficeStep ProperStep(OfficeStepKind kind, string title, string? explanation) => new(kind, title)
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
				Explanation = OfficeData.Hours.PsalmodyExplanation.Replace("{n}", (index++).ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal),
			};
		}
	}

	private static IReadOnlyList<OfficialSource> Sources(DateOnly date, Hour hour)
	{
		var page = OfficeData.Hours.Hours[hour].Page;

		return OfficeData.Hours.OfficialSources
			.Select(s => new OfficialSource(
				s.Name,
				s.Url.Replace("{date}", date.ToString("yyyyMMdd", CultureInfo.InvariantCulture), StringComparison.Ordinal)
					.Replace("{page}", page, StringComparison.Ordinal)))
			.ToArray();
	}
}
