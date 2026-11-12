namespace Respondeo.LiturgicalCalendar;

/// <summary>Whether the selected calendar profile identifies a Mass obligation for a date.</summary>
public enum LiturgicalObligationStatus
{
    /// <summary>The selected profile does not provide enough information to determine the status.</summary>
    Unknown,

    /// <summary>The selected calendar profile does not identify a holy day obligation on this date.</summary>
    NotObligatory,

    /// <summary>The date is a Sunday or a holy day of obligation under the selected calendar profile.</summary>
    Obligatory,
}

/// <summary>The rule that supports a calendar-level Mass-obligation status.</summary>
public enum LiturgicalObligationRule
{
    /// <summary>No applicable rule is available.</summary>
    Unknown,

    /// <summary>Sunday is the primordial holy day of obligation (Canon 1246 §1).</summary>
    Sunday,

    /// <summary>The date is on the universal list in Canon 1246 §1.</summary>
    UniversalCanon1246,

    /// <summary>The provisional United States national-profile rule applies; direct decree-level review remains open.</summary>
    UnitedStatesNationalPolicy,

    /// <summary>The provisional profile's Saturday/Monday exception applies; direct decree-level review remains open.</summary>
    UnitedStatesSaturdayMondayException,

    /// <summary>The selected profile's policy does not identify an obligation on this date.</summary>
    NoApplicableObligation,

    /// <summary>A transferred holy day is involved, and transfer of the obligation is not modeled.</summary>
    TransferEffectNotModeled,
}

/// <summary>A calendar-profile result, not a determination of local diocesan law or an individual dispensation.</summary>
public sealed record LiturgicalObligation(LiturgicalObligationStatus Status, LiturgicalObligationRule Rule)
{
    /// <summary>A stable identifier for the rule that produced this result.</summary>
    public string RuleId => Rule switch
    {
        LiturgicalObligationRule.Unknown => "OBL-unknown",
        LiturgicalObligationRule.Sunday => "OBL-01/sunday",
        LiturgicalObligationRule.UniversalCanon1246 => "OBL-01/universal-canon-1246",
        LiturgicalObligationRule.UnitedStatesNationalPolicy => "OBL-02/united-states-national-policy",
        LiturgicalObligationRule.UnitedStatesSaturdayMondayException => "OBL-02/united-states-saturday-monday-exception",
        LiturgicalObligationRule.NoApplicableObligation => "OBL-00/no-applicable-obligation",
        LiturgicalObligationRule.TransferEffectNotModeled => "OBL-03/transfer-effect-not-modeled",
        _ => throw new InvalidOperationException($"Unknown liturgical-obligation rule: {Rule}."),
    };

    /// <summary>An undetermined result for manually supplied days or unsupported calendar profiles.</summary>
    public static LiturgicalObligation Unknown { get; } = new(LiturgicalObligationStatus.Unknown, LiturgicalObligationRule.Unknown);
}

internal static class LiturgicalObligationResolver
{
    private static readonly (int Month, int Day)[] UniversalFixedObligations =
    [
        (1, 1),
        (1, 6),
        (3, 19),
        (6, 29),
        (8, 15),
        (11, 1),
        (12, 8),
        (12, 25),
    ];

    public static LiturgicalObligation Resolve(
        DateOnly date,
        CalendarProfile profile,
        IReadOnlyList<LiturgicalTransfer> transfers)
    {
        if (date.DayOfWeek == DayOfWeek.Sunday)
        {
            return new(LiturgicalObligationStatus.Obligatory, LiturgicalObligationRule.Sunday);
        }

        if (profile.Obligation == ObligationPolicy.UnitedStates)
        {
            return ResolveUnitedStates(date, profile, transfers);
        }

        if (profile.Obligation == ObligationPolicy.Unknown)
        {
            return LiturgicalObligation.Unknown;
        }

        if (transfers.Any(TransfersUniversalObligation))
        {
            return new(LiturgicalObligationStatus.Unknown, LiturgicalObligationRule.TransferEffectNotModeled);
        }

        var easter = LiturgicalDates.Easter(date.Year);
        var isUniversalObligation = UniversalFixedObligations.Contains((date.Month, date.Day))
            || date == easter.AddDays(39)
            || date == easter.AddDays(60);

        return isUniversalObligation
            ? new(LiturgicalObligationStatus.Obligatory, LiturgicalObligationRule.UniversalCanon1246)
            : new(LiturgicalObligationStatus.NotObligatory, LiturgicalObligationRule.NoApplicableObligation);
    }

    private static LiturgicalObligation ResolveUnitedStates(
        DateOnly date,
        CalendarProfile profile,
        IReadOnlyList<LiturgicalTransfer> transfers)
    {
        if (transfers.Any(TransfersUnitedStatesObligation))
        {
            return new(LiturgicalObligationStatus.Unknown, LiturgicalObligationRule.TransferEffectNotModeled);
        }

        if (date is { Month: 1, Day: 1 } or { Month: 8, Day: 15 } or { Month: 11, Day: 1 })
        {
            if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Monday)
            {
                return new(LiturgicalObligationStatus.NotObligatory, LiturgicalObligationRule.UnitedStatesSaturdayMondayException);
            }

            return new(LiturgicalObligationStatus.Obligatory, LiturgicalObligationRule.UnitedStatesNationalPolicy);
        }

        if (date is { Month: 12, Day: 8 } or { Month: 12, Day: 25 })
        {
            return new(LiturgicalObligationStatus.Obligatory, LiturgicalObligationRule.UnitedStatesNationalPolicy);
        }

        var ascensionThursday = LiturgicalDates.Easter(date.Year).AddDays(39);
        if (profile.AscensionOffset == 39 && date == ascensionThursday)
        {
            return new(LiturgicalObligationStatus.Obligatory, LiturgicalObligationRule.UnitedStatesNationalPolicy);
        }

        return new(LiturgicalObligationStatus.NotObligatory, LiturgicalObligationRule.NoApplicableObligation);
    }

    private static bool TransfersUniversalObligation(LiturgicalTransfer transfer) =>
        transfer.CelebrationKey is "saint-joseph" or "immaculate-conception";

    private static bool TransfersUnitedStatesObligation(LiturgicalTransfer transfer) =>
        transfer.CelebrationKey == "immaculate-conception";
}
