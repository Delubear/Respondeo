using System.Globalization;
using Respondeo.LiturgicalCalendar;

namespace Respondeo.LiturgyOfTheHours;

/// <summary>
/// Composes Morning, Evening, and Night Prayer of the post-1970 Liturgy of the Hours.
/// Proper psalmody takes precedence at Morning and Evening Prayer, including on memorials (GILH 235a).
/// Otherwise Morning Prayer on solemnities and feasts uses Sunday Week I, while memorials keep the weekday psalter.
/// Sundays and solemnities begin with First Vespers the evening before, and Night Prayer follows a weekly cycle (with the Sunday forms after either Vespers).
/// The order, titles, explanations, and texts of each hour come from the embedded data files (see <see cref="OfficeData"/>); only the rules live here.
/// </summary>
internal sealed class OrdinaryFormOffice(ILiturgicalCalendar calendar, IHoursTextProvider texts) : IOfficeForm
{
	public OfficeForm Form => OfficeForm.OrdinaryForm1970;

	public Office Compose(DateOnly date, Hour hour, LiturgicalParticipation participation = LiturgicalParticipation.None,
		ComplineSundayForm complineForm = ComplineSundayForm.Automatic, ComplinePsalmodyChoice complinePsalmody = ComplinePsalmodyChoice.Weekday,
		bool preferOfficialText = true, string? memorialCommemorationCalendarKey = null, string? celebrationCalendarKey = null)
	{
		if (!Enum.IsDefined(participation))
		{
			throw new ArgumentOutOfRangeException(nameof(participation), participation, "Unknown liturgical participation.");
		}
		if (!Enum.IsDefined(complineForm))
		{
			throw new ArgumentOutOfRangeException(nameof(complineForm), complineForm, "Unknown Compline form.");
		}
		if (!Enum.IsDefined(complinePsalmody))
		{
			throw new ArgumentOutOfRangeException(nameof(complinePsalmody), complinePsalmody, "Unknown Compline psalmody choice.");
		}

		var evening = hour == Hour.MorningPrayer ? null : calendar.ForEvening(date);
		var today = evening?.Today ?? calendar.ForDate(date);
		var day = evening?.OfficeDay ?? today;
		if (celebrationCalendarKey is not null)
		{
			day = SelectCalendarCelebration(day, celebrationCalendarKey);
			if (evening is null)
			{
				today = day;
			}
			else
			{
				evening = evening with
				{
					Today = evening.Today.Date == day.Date ? day : evening.Today,
					OfficeDay = day,
				};
				today = evening.Today;
			}
		}
		var canChooseComplineForm = hour == Hour.NightPrayer && today.EasterOctaveDay is not null;
		if (complineForm != ComplineSundayForm.Automatic && !canChooseComplineForm)
		{
			throw new ArgumentException("A Sunday Compline form can be selected only for Night Prayer during the Easter octave.", nameof(complineForm));
		}

		var firstVespers = evening?.IsFirstVespers == true;
		var canChooseComplinePsalmody = hour == Hour.NightPrayer && !firstVespers && !IsSolemnity(today)
			&& today.Date.DayOfWeek != DayOfWeek.Sunday && today.Season != LiturgicalSeason.Triduum && today.EasterOctaveDay is null;
		if (complinePsalmody != ComplinePsalmodyChoice.Weekday && !canChooseComplinePsalmody)
		{
			throw new ArgumentException("Sunday psalm substitution is available only for weekday Compline, not a complete Sunday form.", nameof(complinePsalmody));
		}

		var memorialCommemoration = SelectMemorialCommemoration(day, hour, memorialCommemorationCalendarKey);
		var tomorrow = firstVespers ? day : today;
		var week = calendar.ForWeek(day.Date).PsalterWeek;
		var omission = ConditionalOmission(date, hour);
		if (omission is not null && omission.Participation == participation)
		{
			return new Office(date, hour, Title(hour, day, firstVespers), day, week, [])
			{
				IsFirstVespers = firstVespers,
				ConditionalOmission = omission,
				IsOmitted = true,
				SelectedCelebrationCalendarKey = celebrationCalendarKey,
				OfficialSources = Sources(date, hour),
			};
		}

		var nightCycleDay = complineForm switch
		{
			ComplineSundayForm.SundayI => DayOfWeek.Saturday,
			ComplineSundayForm.SundayII => DayOfWeek.Sunday,
			_ => NightCycleDay(today, firstVespers),
		};
		var nightPsalmodyDay = complinePsalmody switch
		{
			ComplinePsalmodyChoice.SundayI => DayOfWeek.Saturday,
			ComplinePsalmodyChoice.SundayII => DayOfWeek.Sunday,
			_ => nightCycleDay,
		};
		var context = new HourContext(hour, today, day, firstVespers, nightCycleDay, week,
			evening?.Season ?? today.MorningPrayerSeason, evening?.EasterOctaveDay, nightPsalmodyDay, memorialCommemoration);
		var psalmody = hour switch
		{
			Hour.MorningPrayer => MorningPsalmody(today, week),
			Hour.EveningPrayer => EveningPsalmody(today, tomorrow, week, firstVespers),
			_ => Psalter.NightPsalmody(context.NightPsalmodyDay).Psalms,
		};

		var steps = BuildSteps(OfficeData.Hours.Hours[hour], context, psalmody);
		var resolvedSteps = preferOfficialText ? ApplyProviderTexts(steps, context) : steps;
		var hasProviderText = resolvedSteps.Any(step => step.HasProviderText);

		return new Office(date, hour, Title(hour, day, firstVespers), day, week, resolvedSteps)
		{
			IsFirstVespers = firstVespers,
			ConditionalOmission = omission,
			SelectedComplineForm = canChooseComplineForm
				? nightCycleDay == DayOfWeek.Saturday ? ComplineSundayForm.SundayI : ComplineSundayForm.SundayII
				: null,
			SelectedComplinePsalmody = canChooseComplinePsalmody ? complinePsalmody : null,
			SelectedCelebrationCalendarKey = celebrationCalendarKey,
			SelectedMemorialCommemorationCalendarKey = memorialCommemoration?.CalendarKey,
			OfficialSources = Sources(date, hour),
			Acknowledgment = hasProviderText ? texts.Acknowledgment : null,
			IsSampleContent = hasProviderText && texts.IsSample,
		};
	}

	private static LiturgicalDay SelectCalendarCelebration(LiturgicalDay day, string celebrationCalendarKey)
	{
		if (day.Occurrence is not { Rule: LiturgicalOccurrenceRule.UnresolvedEqualPrecedence } occurrence)
		{
			throw new ArgumentException("A celebration can be selected only when the calendar reports unresolved equal precedence.",
				nameof(celebrationCalendarKey));
		}

		var celebration = occurrence.ContendingCelebrations.SingleOrDefault(candidate =>
			candidate.CalendarKey == celebrationCalendarKey);
		if (celebration is null)
		{
			throw new ArgumentException("The selected celebration is not a contender for this calendar day.",
				nameof(celebrationCalendarKey));
		}

		return day with { Celebration = celebration, Color = celebration.Color, FerialName = null };
	}

	private static LiturgicalCelebration? SelectMemorialCommemoration(
		LiturgicalDay day, Hour hour, string? memorialCommemorationCalendarKey)
	{
		if (memorialCommemorationCalendarKey is null)
		{
			return null;
		}

		if (hour is not (Hour.MorningPrayer or Hour.EveningPrayer))
		{
			throw new ArgumentException("Restricted memorial commemorations are available only at Morning or Evening Prayer.",
				nameof(memorialCommemorationCalendarKey));
		}

		return day.MemorialCommemorations.FirstOrDefault(
			commemoration => commemoration.CalendarKey == memorialCommemorationCalendarKey)
			?? throw new ArgumentException("The selected memorial is not available for this Office day.",
				nameof(memorialCommemorationCalendarKey));
	}

	private static OfficeOmissionRule? ConditionalOmission(DateOnly date, Hour hour)
	{
		var offset = date.DayNumber - LiturgicalDates.Easter(date.Year).DayNumber;
		var participation = (hour, offset) switch
		{
			(Hour.EveningPrayer, -3) => LiturgicalParticipation.HolyThursdayEveningMass,
			(Hour.EveningPrayer, -2) => LiturgicalParticipation.GoodFridayPassion,
			(Hour.NightPrayer, -1) => LiturgicalParticipation.EasterVigil,
			(Hour.NightPrayer, _) when date.Month == 12 && date.Day == 24 => LiturgicalParticipation.ChristmasOfficeOfReadingsVigil,
			_ => LiturgicalParticipation.None,
		};
		return participation == LiturgicalParticipation.None ? null : OfficeData.Hours.ParticipationOmissions[participation];
	}

	/// <summary>Resolves provider content after assignment, preserving citation bindings and scoped fixed-text variants.</summary>
	private List<OfficeStep> ApplyProviderTexts(List<OfficeStep> steps, HourContext context)
	{
		var hourKey = HourKey(context);
		var prefix = PsalterPrefix(context);
		var scopedPrefix = context.FirstVespers && context.Hour == Hour.EveningPrayer
			? prefix[..prefix.LastIndexOf('/')] + "/evening-i" : prefix;
		var psalm = 0;
		return steps.Select(Resolve).ToList();

		OfficeStep Resolve(OfficeStep step)
		{
			var isPsalmody = step.Kind is OfficeStepKind.Psalm or OfficeStepKind.Canticle;
			var element = step.ContentElement ?? (step.Kind switch
			{
				OfficeStepKind.Psalm or OfficeStepKind.Canticle => $"psalm-antiphon-{++psalm}",
				OfficeStepKind.Versicle => step.Options is null ? "opening" : "invitatory-verse",
				OfficeStepKind.Examen => "examen",
				OfficeStepKind.Hymn => "hymn",
				OfficeStepKind.Reading => "reading",
				OfficeStepKind.Responsory => "responsory",
				OfficeStepKind.GospelCanticle => "gospel-canticle",
				OfficeStepKind.Intercessions => "intercessions",
				OfficeStepKind.LordsPrayer => "lords-prayer",
				OfficeStepKind.Prayer => "collect",
				OfficeStepKind.Conclusion => "conclusion",
				_ => null,
			});
			var scoped = step.ContentElement is not null
				|| step.Kind is OfficeStepKind.Versicle or OfficeStepKind.Examen or OfficeStepKind.LordsPrayer or OfficeStepKind.Conclusion
				|| (step.Text is not null && step.Kind is OfficeStepKind.Responsory or OfficeStepKind.Prayer)
				|| (context.Hour == Hour.NightPrayer && step.Kind == OfficeStepKind.GospelCanticle);
			IEnumerable<string> keys = [];
			if (element is not null)
			{
				if (step.ContentElement is { } commemorationElement
					&& commemorationElement.StartsWith("commemoration/", StringComparison.Ordinal)
					&& context.MemorialCommemoration is { } commemoration)
				{
					keys = [$"proper/{CelebrationId(commemoration)}/{commemorationElement}"];
				}
				else if (isPsalmody && step.ContentElement is null)
				{
					keys = PsalmodyKeys(context, element);
				}
				else if (scoped)
				{
					keys = texts.IsSample && step.ContentElement is not null
						? [$"{scopedPrefix}/{element}", element] : [$"{scopedPrefix}/{element}"];
				}
				else
				{
					keys = LicensedKeys(context.Day, prefix, hourKey, element);
				}
			}
			var resolved = element is null ? step : Apply(step, element, texts.Find(keys));
			if (step.IntroText is not null && texts.Find([$"{scopedPrefix}/opening"]) is { } opening && !string.IsNullOrWhiteSpace(opening.Text))
			{
				resolved = resolved with { IntroText = opening.Text, IsLicensedText = !texts.IsSample, IsSampleText = texts.IsSample };
			}
			return step.Options is null ? resolved : resolved with { Options = step.Options.Select(Resolve).ToArray() };
		}

		OfficeStep Apply(OfficeStep step, string? element, LicensedText? content)
		{
			if (content is null)
			{
				return step;
			}
			if (step.Reference is { } reference && ((content.Citation is not null && content.Citation != reference.Citation)
				|| (step.Kind is OfficeStepKind.Psalm or OfficeStepKind.Canticle && !string.IsNullOrEmpty(content.Text) && content.Citation is null)))
			{
				throw new InvalidDataException($"Hours content for '{element}' must identify the selected citation '{reference.Citation}'.");
			}
			if (string.IsNullOrWhiteSpace(content.Text) && string.IsNullOrWhiteSpace(content.Antiphon))
			{
				return step;
			}

			var antiphon = string.IsNullOrWhiteSpace(content.Antiphon) ? step.Antiphon : content.Antiphon;
			var body = string.IsNullOrWhiteSpace(content.Text) ? step.Text : content.Text;
			return step with
			{
				Text = body,
				Antiphon = antiphon,
				UseOfficialText = step.Kind is OfficeStepKind.Psalm or OfficeStepKind.Canticle
					? string.IsNullOrWhiteSpace(antiphon) : step.UseOfficialText && body is null,
				IsLicensedText = !texts.IsSample,
				IsSampleText = texts.IsSample,
			};
		}
	}

	private static string HourKey(HourContext context) => context.Hour switch
	{
		Hour.MorningPrayer => "morning",
		Hour.EveningPrayer => context.FirstVespers ? "evening-i" : "evening",
		_ => "night",
	};

	private static string PsalterPrefix(HourContext context, bool psalmody = false)
	{
		var weekday = context.Hour == Hour.NightPrayer
			? psalmody ? context.NightPsalmodyDay : context.NightCycleDay
			: context.Today.Date.DayOfWeek;
		var hour = context.Hour == Hour.EveningPrayer ? "evening" : HourKey(context);
		return $"{context.Season.ToString().ToLowerInvariant()}/w{context.Week}/{weekday.ToString().ToLowerInvariant()}/{hour}";
	}

	private IEnumerable<string> PsalmodyKeys(HourContext context, string element)
	{
		var celebration = context.Day.Celebration;
		var proper = celebration is null ? null : Sanctoral.Celebration(CelebrationId(celebration));
		var ownPsalmody = context.Hour == Hour.MorningPrayer ? proper?.MorningPsalmody : proper?.EveningPsalmody;
		var festal = IsFestal(context.Day);
		var octaveDay = context.Hour == Hour.NightPrayer ? null : context.Day.EasterOctaveDay;
		var usesProper = context.Hour != Hour.NightPrayer && (ownPsalmody is not null || festal || octaveDay is not null
			|| TriduumPsalmody(context.Day, context.Hour == Hour.MorningPrayer) is not null);
		var hourKey = HourKey(context);

		if (octaveDay is >= 2)
		{
			yield return $"proper/easter-octave-day-{octaveDay}/{hourKey}/{element}";
		}
		else if (usesProper)
		{
			if (celebration is not null)
			{
				yield return $"proper/{CelebrationId(celebration)}/{hourKey}/{element}";
			}

			// A prescribed proper set must not silently borrow Common or weekday antiphons (GILH 235a).
			if (ownPsalmody is null && festal && proper?.Common is { } common)
			{
				yield return $"common/{common}/{hourKey}/{element}";
			}
		}
		else
		{
			yield return $"{PsalterPrefix(context, psalmody: true)}/{element}";
		}

		if (texts.IsSample)
		{
			yield return $"{hourKey}/{element}";
			yield return element;
		}
	}

	/// <summary>Non-psalmody elements may use the Common on memorials (GILH 235b); Night Prayer remains ferial (236).</summary>
	private IEnumerable<string> LicensedKeys(LiturgicalDay day, string prefix, string hourKey, string element)
	{
		if (hourKey != "night" && day.Celebration is { } celebration)
		{
			var id = CelebrationId(celebration);
			yield return $"proper/{id}/{hourKey}/{element}";
			if (hourKey != "evening-i")
			{
				yield return $"proper/{id}/{element}";
			}

			if ((Sanctoral.Celebration(id)?.Common ?? texts.FindCelebration(id)?.Common) is { Length: > 0 } common)
			{
				yield return $"common/{common}/{hourKey}/{element}";
				if (hourKey != "evening-i")
				{
					yield return $"common/{common}/{element}";
				}
			}
		}

		yield return $"{prefix}/{element}";
		yield return $"{hourKey}/{element}";
		yield return element;
	}

	/// <summary>Preserves established content IDs for migrated identities; only legacy entries derive an ID from the display name.</summary>
	internal static string CelebrationId(LiturgicalCelebration celebration) => celebration.CalendarKey is { } key
		? OfficeData.Sanctoral.CalendarBindings.GetValueOrDefault(key, key)
		: CelebrationId(celebration.Name);

	/// <summary>Turns a legacy celebration name into a content id, e.g. "Saint Agnes, Virgin and Martyr" becomes "saint-agnes-virgin-and-martyr".</summary>
	internal static string CelebrationId(string name)
	{
		var builder = new System.Text.StringBuilder(name.Length);
		foreach (var c in name.Normalize(System.Text.NormalizationForm.FormD))
		{
			if (char.IsLetterOrDigit(c))
			{
				builder.Append(char.ToLowerInvariant(c));
			}
			else if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark && builder.Length > 0 && builder[^1] != '-')
			{
				builder.Append('-');
			}
		}

		return builder.ToString().TrimEnd('-');
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
		day.IsHolyWeekWeekday
			? Psalter.MorningPsalmody(week, day.Date.DayOfWeek)
			: EasterOctavePsalmody(day, morning: true)
				?? TriduumPsalmody(day, morning: true)
				?? (day.Celebration is { } celebration ? Sanctoral.MorningPsalmody(CelebrationId(celebration)) : null)
				?? (IsFestal(day) || day.Date.DayOfWeek == DayOfWeek.Sunday
					? Psalter.MorningPsalmody(IsFestal(day) ? 1 : week, DayOfWeek.Sunday)
					: Psalter.MorningPsalmody(week, day.Date.DayOfWeek));

	/// <summary>Holy Thursday morning still belongs to Lent and uses the psalter.</summary>
	private static IReadOnlyList<string>? TriduumPsalmody(LiturgicalDay day, bool morning)
	{
		if (day.Season != LiturgicalSeason.Triduum)
		{
			return null;
		}

		var offset = day.Date.DayNumber - LiturgicalDates.Easter(day.Date.Year).DayNumber;
		var proper = OfficeData.TemporalPsalmody.Triduum.GetValueOrDefault(offset);
		return morning ? proper?.Morning : proper?.Evening;
	}

	private static IReadOnlyList<string>? EasterOctavePsalmody(LiturgicalDay day, bool morning)
	{
		if (day.EasterOctaveDay is null)
		{
			return null;
		}

		var easter = OfficeData.TemporalPsalmody.Triduum[0];
		return morning ? easter.Morning : easter.Evening;
	}

	private static IReadOnlyList<string> EveningPsalmody(LiturgicalDay today, LiturgicalDay tomorrow, int week, bool firstVespers)
	{
		var day = firstVespers ? tomorrow : today;
		if (EasterOctavePsalmody(day, morning: false) is { } octave)
		{
			return octave;
		}

		if (day.Season == LiturgicalSeason.Lent && day.Date.DayOfWeek == DayOfWeek.Sunday)
		{
			var psalms = Psalter.EveningPsalmody(week, firstVespers ? DayOfWeek.Saturday : DayOfWeek.Sunday);
			return firstVespers ? psalms : [psalms[0], psalms[1], OfficeData.TemporalPsalmody.LentenSundayEveningCanticle];
		}

		if (firstVespers)
		{
			var psalms = Psalter.EveningPsalmody(IsSolemnity(tomorrow) ? 1 : week, DayOfWeek.Saturday);
			var canticle = tomorrow.Celebration is { } incoming ? Sanctoral.Celebration(CelebrationId(incoming))?.FirstEveningCanticle : null;
			return canticle is not null ? [psalms[0], psalms[1], canticle] : psalms;
		}

		if (TriduumPsalmody(today, morning: false) is { } triduum)
		{
			return triduum;
		}

		if (today.Celebration is { } celebration && Sanctoral.EveningPsalmody(CelebrationId(celebration), includeCommon: IsFestal(today)) is { } proper)
		{
			return proper;
		}

		return IsFestal(today) || today.Date.DayOfWeek == DayOfWeek.Sunday
			? Psalter.EveningPsalmody(IsFestal(today) ? 1 : week, DayOfWeek.Sunday)
			: Psalter.EveningPsalmody(week, today.Date.DayOfWeek);
	}

	/// <summary>Saturday after First Vespers; otherwise Sunday on Sundays, solemnities, the Triduum, and throughout the Easter octave.</summary>
	private static DayOfWeek NightCycleDay(LiturgicalDay today, bool firstVespers) =>
		firstVespers
			? DayOfWeek.Saturday
			: IsSolemnity(today) || today.Date.DayOfWeek == DayOfWeek.Sunday || today.Season == LiturgicalSeason.Triduum || today.EasterOctaveDay is not null
				? DayOfWeek.Sunday
				: today.Date.DayOfWeek;

	/// <summary>The civil day, selected Office day and temporal season, and cycle facts needed to compose an hour.</summary>
	private sealed record HourContext(Hour Hour, LiturgicalDay Today, LiturgicalDay Day, bool FirstVespers, DayOfWeek NightCycleDay, int Week,
		LiturgicalSeason Season, int? EasterOctaveDay, DayOfWeek NightPsalmodyDay,
		LiturgicalCelebration? MemorialCommemoration);

	private static List<OfficeStep> BuildSteps(HourTemplate template, HourContext context, IReadOnlyList<string> psalmody)
	{
		var steps = new List<OfficeStep>();

		foreach (var raw in template.Steps)
		{
			var step = raw.ForSeason(context.Season);

			switch (step.Source)
			{
				case StepSource.Psalmody:
					steps.AddRange(Psalmody(psalmody));
					break;
				case StepSource.NightResponsory when context.Season == LiturgicalSeason.Easter && context.EasterOctaveDay is not (>= 1 and <= 7):
					steps.Add(ProperStep(step.Kind, step.Title, step.FallbackExplanation));
					break;
				case StepSource.NightResponsory:
				case StepSource.Fixed:
					steps.Add(new OfficeStep(step.Kind, step.Title)
					{
						Text = OfficeData.ResolveText(step.Text ?? []),
						Reference = step.Reference is null ? null : new PsalmodyReference(step.Reference),
						Explanation = step.Explanation,
						Antiphon = step.Antiphon is null ? null : OfficeData.ResolveText([step.Antiphon]),
						Options = step.Invitatory ? InvitatoryPsalms() : null,
						IntroText = step.IntroText is null ? null : OfficeData.ResolveText(step.IntroText),
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
					if (context.MemorialCommemoration is { } memorial)
					{
						steps.Add(MemorialCommemorationStep(OfficeStepKind.Antiphon, "Memorial Antiphon", "commemoration/antiphon", memorial));
						steps.Add(MemorialCommemorationStep(OfficeStepKind.Prayer, "Memorial Prayer", "commemoration/prayer", memorial));
					}
					break;
				case StepSource.ShortReading:
					steps.Add(ShortReadingCitation(context) is { } citation
						? ReadingStep(step, citation)
						: ProperStep(step.Kind, step.Title, step.FallbackExplanation ?? step.Explanation));
					break;
				case StepSource.NightReading:
					steps.Add(ReadingStep(step, Psalter.NightPsalmody(context.NightCycleDay).Reading));
					break;
				case StepSource.NightPrayer:
					steps.Add(OfficeData.Texts.NightConcludingPrayers.TryGetValue(Psalter.Key(context.NightCycleDay), out var prayer)
						? new OfficeStep(step.Kind, step.Title) { Text = OfficeData.ResolveText(prayer), Explanation = step.Explanation }
						: ProperStep(step.Kind, step.Title, step.FallbackExplanation ?? step.Explanation));
					break;
				case StepSource.MarianAntiphon:
					var rule = OfficeData.Texts.MarianAntiphons.First(r => r.Matches(context.Today.Date, context.Season));
					steps.Add(context.Season == LiturgicalSeason.Easter
						? new OfficeStep(step.Kind, rule.Title)
						{
							Text = OfficeData.Text(rule.Text), Explanation = step.Explanation, ContentElement = $"marian-antiphon/{rule.Text}",
						}
						: new OfficeStep(step.Kind, "Marian Antiphon")
						{
							Explanation = step.Explanation,
							Options = OfficeData.Texts.MarianAntiphons.Prepend(rule).DistinctBy(r => r.Text)
								.Select(r => new OfficeStep(step.Kind, r.Title)
								{
									Text = OfficeData.Text(r.Text), ContentElement = $"marian-antiphon/{r.Text}",
								}).ToArray(),
						});
					break;
				default:
					throw new InvalidOperationException($"Unsupported step source '{step.Source}'.");
			}
		}

		return steps;
	}

	private static OfficeStep MemorialCommemorationStep(
		OfficeStepKind kind, string title, string contentElement, LiturgicalCelebration memorial) => new(kind, title)
	{
		ContentElement = contentElement,
		UseOfficialText = true,
		Explanation = $"GILH 239 permits this limited addition for {memorial.Name}; the temporal Office remains the Office being prayed.",
	};

	/// <summary>The psalter short reading, used only on Ordinary Time days that are not solemnities or feasts (First Vespers follows the coming Sunday).</summary>
	private static string? ShortReadingCitation(HourContext context)
	{
		if (context.Day.Celebration is { } celebration && Sanctoral.ShortReading(context.Hour, CelebrationId(celebration)) is { } sanctoral)
		{
			return sanctoral;
		}

		if (context.Season != LiturgicalSeason.OrdinaryTime || IsFestal(context.Day))
		{
			return null;
		}

		if (context.FirstVespers)
		{
			return context.Day.Date.DayOfWeek == DayOfWeek.Sunday
				? Psalter.ShortReading(Hour.EveningPrayer, context.Week, DayOfWeek.Saturday)
				: null;
		}

		return Psalter.ShortReading(context.Hour, context.Week, context.Today.Date.DayOfWeek);
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
				ContentElement = $"invitatory/{CelebrationId(psalm.Citation)}",
			})
			.ToArray();
	}

	private static OfficeStep ProperStep(OfficeStepKind kind, string title, string? explanation) => new(kind, title)
	{
		UseOfficialText = true,
		Explanation = explanation,
	};

	private static OfficeStep ReadingStep(StepTemplate template, string citation)
	{
		var reference = new PsalmodyReference(citation);
		var passage = OfficeData.Texts.ReadingPassages.GetValueOrDefault(citation);
		return new OfficeStep(template.Kind, template.Title)
		{
			Reference = reference,
			Passage = passage,
			Explanation = template.Explanation,
		};
	}

	private static IEnumerable<OfficeStep> Psalmody(IEnumerable<string> citations)
	{
		var index = 1;

		foreach (var citation in citations)
		{
			var canticle = Psalter.IsCanticle(citation);

			yield return new OfficeStep(canticle ? OfficeStepKind.Canticle : OfficeStepKind.Psalm, canticle ? $"Canticle: {citation}" : citation)
			{
				Reference = new PsalmodyReference(citation, Psalter.DouayCitation(citation)),
				Passage = OfficeData.Texts.PsalmodyPassages.GetValueOrDefault(citation),
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
