namespace Respondeo.LiturgyOfTheHours;

/// <summary>
/// The four-week psalter of the post-1970 Liturgy of the Hours: psalm and canticle citations for Morning, Evening, and Night Prayer.
/// The citations live in <c>Data/psalter.json</c>; the copyrighted translations are read from official sources.
/// </summary>
internal static class Psalter
{
	public static IReadOnlyList<string> MorningPsalmody(int week, DayOfWeek day) => OfficeData.Psalter.Morning[week - 1][Key(day)];

	public static IReadOnlyList<string> EveningPsalmody(int week, DayOfWeek day) => OfficeData.Psalter.Evening[week - 1][Key(day)];

	public static (IReadOnlyList<string> Psalms, string Reading) NightPsalmody(DayOfWeek day)
	{
		var night = OfficeData.Psalter.Night[Key(day)];
		return (night.Psalms, night.Reading);
	}

	/// <summary>The citation of the Ordinary Time ferial short reading for Morning or Evening Prayer, or null when none is recorded.</summary>
	public static string? ShortReading(Hour hour, int week, DayOfWeek day)
	{
		var readings = hour == Hour.MorningPrayer ? OfficeData.Psalter.MorningReadings : OfficeData.Psalter.EveningReadings;
		return readings is not null && week >= 1 && week <= readings.Count && readings[week - 1].TryGetValue(Key(day), out var citation) ? citation : null;
	}

	public static bool IsCanticle(string citation) => !citation.StartsWith("Psalm ", StringComparison.Ordinal);

	/// <summary>Converts a modern (Hebrew) psalm citation into the bundled translation's numbering for display.</summary>
	public static string? DouayCitation(string citation) => Respondeo.Bible.Contracts.BibleReferences.TranslationCitation(citation);

	/// <summary>The lower-case day name used as a key in the data files.</summary>
	internal static string Key(DayOfWeek day) => day.ToString().ToLowerInvariant();
}
