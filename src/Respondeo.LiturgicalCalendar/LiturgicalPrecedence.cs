namespace Respondeo.LiturgicalCalendar;

/// <summary>Rows of the Universal Norms 59 Table of Liturgical Days; lower row numbers take precedence.</summary>
public enum LiturgicalPrecedenceClass
{
    PaschalTriduum = 1,
    PrincipalTemporalDay = 2,
    GeneralSolemnityOrAllSouls = 3,
    ProperSolemnity = 4,
    FeastOfTheLord = 5,
    OrdinarySunday = 6,
    GeneralFeast = 7,
    ProperFeast = 8,
    PrivilegedWeekday = 9,
    GeneralMemorial = 10,
    ProperMemorial = 11,
    OptionalMemorial = 12,
    Ferial = 13,
}

/// <summary>Shared occurrence/concurrence categories. Local-proper inventory and restricted commemorations remain separate work.</summary>
internal static class LiturgicalPrecedence
{
    internal static LiturgicalPrecedenceClass ForCelebration(LiturgicalCelebration celebration) => celebration.Precedence ?? celebration.Rank switch
    {
        CelebrationRank.Solemnity => LiturgicalPrecedenceClass.GeneralSolemnityOrAllSouls,
        CelebrationRank.Feast => LiturgicalPrecedenceClass.GeneralFeast,
        CelebrationRank.Memorial => LiturgicalPrecedenceClass.GeneralMemorial,
        CelebrationRank.OptionalMemorial => LiturgicalPrecedenceClass.OptionalMemorial,
        _ => throw new InvalidOperationException($"Unknown celebration rank: {celebration.Rank}."),
    };

    internal static LiturgicalPrecedenceClass ForTemporalDay(DateOnly date, LiturgicalSeason season)
    {
        var offset = date.DayNumber - LiturgicalDates.Easter(date.Year).DayNumber;
        if (offset is >= -3 and <= 0)
        {
            return LiturgicalPrecedenceClass.PaschalTriduum;
        }

        if (offset == -46 || offset is >= -7 and <= 7
            || date.DayOfWeek == DayOfWeek.Sunday && season is LiturgicalSeason.Advent or LiturgicalSeason.Lent or LiturgicalSeason.Easter)
        {
            return LiturgicalPrecedenceClass.PrincipalTemporalDay;
        }

        if (date.DayOfWeek == DayOfWeek.Sunday)
        {
            return LiturgicalPrecedenceClass.OrdinarySunday;
        }

        return season == LiturgicalSeason.Lent
            || season == LiturgicalSeason.Advent && date is { Month: 12, Day: >= 17 }
            || season == LiturgicalSeason.Christmas && date is { Month: 12, Day: >= 26 and <= 31 }
            ? LiturgicalPrecedenceClass.PrivilegedWeekday
            : LiturgicalPrecedenceClass.Ferial;
    }

    internal static LiturgicalPrecedenceClass ForDay(LiturgicalDay day)
    {
        var temporal = ForTemporalDay(day.Date, day.Season);
        var celebration = day.Celebration is { } principal ? ForCelebration(principal) : LiturgicalPrecedenceClass.Ferial;
        return celebration < temporal ? celebration : temporal;
    }

    internal static bool CanReceiveTransferredSolemnity(
        DateOnly date, LiturgicalSeason season, IReadOnlyList<CelebrationCandidate> candidates)
    {
        return ForTemporalDay(date, season) > LiturgicalPrecedenceClass.ProperFeast
            && candidates.All(candidate => candidate.Precedence > LiturgicalPrecedenceClass.ProperFeast);
    }
}
