namespace Respondeo.LiturgicalCalendar;

internal static class CalendarDataValidation
{
    internal static bool IsCalendarKey(string key) => key.Length > 0
        && key.Split('-').All(segment => segment.Length > 0
            && segment.All(character => character is >= 'a' and <= 'z' or >= '0' and <= '9'));

    internal static bool TryParseEnum<T>(string field, out T value) where T : struct, Enum
    {
        value = default;
        return Enum.GetNames<T>().Contains(field, StringComparer.OrdinalIgnoreCase)
            && Enum.TryParse(field, ignoreCase: true, out value);
    }

    internal static bool IsCompatible(CelebrationRank rank, LiturgicalPrecedenceClass precedence) => rank switch
    {
        CelebrationRank.Solemnity => precedence is LiturgicalPrecedenceClass.GeneralSolemnityOrAllSouls
            or LiturgicalPrecedenceClass.ProperSolemnity or LiturgicalPrecedenceClass.PrincipalTemporalDay
            or LiturgicalPrecedenceClass.PaschalTriduum,
        CelebrationRank.Feast => precedence is LiturgicalPrecedenceClass.GeneralFeast or LiturgicalPrecedenceClass.ProperFeast
            or LiturgicalPrecedenceClass.FeastOfTheLord or LiturgicalPrecedenceClass.GeneralSolemnityOrAllSouls
            or LiturgicalPrecedenceClass.PrincipalTemporalDay,
        CelebrationRank.Memorial => precedence is LiturgicalPrecedenceClass.GeneralMemorial or LiturgicalPrecedenceClass.ProperMemorial,
        CelebrationRank.OptionalMemorial => precedence == LiturgicalPrecedenceClass.OptionalMemorial,
        _ => false,
    };
}
