namespace Respondeo.LiturgicalCalendar;

/// <summary>The phase of Evening Prayer selected for a civil evening.</summary>
public enum EveningPrayerPhase
{
    /// <summary>The current day's ordinary Evening Prayer.</summary>
    Ordinary,

    /// <summary>First Vespers of the following day's Sunday or solemnity.</summary>
    FirstVespers,

    /// <summary>Second Vespers of the current day's Sunday or solemnity.</summary>
    SecondVespers,

    /// <summary>The Triduum's proper Office, not ordinary concurrence; participation-dependent omissions require separate handling.</summary>
    TriduumProper,
}

/// <summary>The civil day and selected Office day shared by Vespers and the following Compline.</summary>
public sealed record LiturgicalEvening(LiturgicalDay Today, LiturgicalDay OfficeDay, EveningPrayerPhase Phase)
{
    /// <summary>Whether the evening belongs to the following day's First Vespers.</summary>
    public bool IsFirstVespers => Phase == EveningPrayerPhase.FirstVespers;

    /// <summary>The temporal season for evening texts; Easter Sunday is also the first day of Easter Time (Universal Norms 19, 22).</summary>
    public LiturgicalSeason Season => OfficeDay.Date == LiturgicalDates.Easter(OfficeDay.Date.Year)
        ? LiturgicalSeason.Easter
        : OfficeDay.Season;

    /// <summary>The civil day's ordinal within the eight-day Easter octave (1-8), or null; First Vespers does not advance this date.</summary>
    public int? EasterOctaveDay => Today.EasterOctaveDay;
}

/// <summary>Sunday/solemnity concurrence for the supported General and US calendars (Universal Norms 59, 61; GILH 204-205, 225, 231).</summary>
internal static class LiturgicalEveningResolver
{
    internal static LiturgicalEvening Resolve(LiturgicalDay today, LiturgicalDay tomorrow, LiturgicalCalendarOptions options)
    {
        if (tomorrow.Date.DayNumber - today.Date.DayNumber != 1)
        {
            throw new ArgumentException("Evening resolution requires consecutive calendar days.", nameof(tomorrow));
        }

        if (today.Season == LiturgicalSeason.Triduum)
        {
            return new(today, today, EveningPrayerPhase.TriduumProper);
        }

        // The Triduum follows its Proper, not First Vespers inferred from the current solemnity-shaped day labels (GILH 208-213).
        var tomorrowHasFirstVespers = tomorrow.Season != LiturgicalSeason.Triduum && IsSundayOrSolemnity(tomorrow);
        if (tomorrowHasFirstVespers && ConcurrenceClass(tomorrow) < ConcurrenceClass(today))
        {
            return new(today, tomorrow, EveningPrayerPhase.FirstVespers);
        }

        var phase = IsSundayOrSolemnity(today) ? EveningPrayerPhase.SecondVespers : EveningPrayerPhase.Ordinary;
        return new(today, today, phase);
    }

    private static bool IsSundayOrSolemnity(LiturgicalDay day) =>
        day.Date.DayOfWeek == DayOfWeek.Sunday || day.Celebration?.Rank == CelebrationRank.Solemnity;

    // A weekday feast has no Second Vespers to compete with Sunday's First Vespers (GILH 225); special All Souls propers remain separate.
    private static LiturgicalPrecedenceClass ConcurrenceClass(LiturgicalDay day) => IsSundayOrSolemnity(day)
        ? LiturgicalPrecedence.ForDay(day)
        : LiturgicalPrecedenceClass.Ferial;
}
