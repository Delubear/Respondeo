namespace Respondeo.LiturgicalCalendar;

/// <summary>The decisive branch of the currently supported occurrence rules, not a complete precedence classification.</summary>
public enum LiturgicalOccurrenceRule
{
    /// <summary>A solemnity admitted after candidate generation and transfers (Universal Norms 59-60).</summary>
    Solemnity,

    /// <summary>A feast of the Lord replaces the Sunday under the existing candidate policy (Universal Norms 59).</summary>
    SundayFeastOfTheLord,

    /// <summary>The Sunday excludes the remaining named candidates (Universal Norms 59).</summary>
    Sunday,

    /// <summary>A weekday of Holy Week or the Easter octave excludes feasts and memorials (precedence table I.2).</summary>
    HolyWeekOrEasterOctave,

    /// <summary>A feast excludes memorials (Universal Norms 59).</summary>
    Feast,

    /// <summary>An obligatory memorial excludes competing memorials (Universal Norms 59-60).</summary>
    ObligatoryMemorial,

    /// <summary>Memorials may be commemorated in the limited form permitted on privileged weekdays (GILH 238-239).</summary>
    MemorialCommemorations,

    /// <summary>Memorial alternatives remain on an ordinary weekday.</summary>
    MemorialOptions,

    /// <summary>No named celebration is selected or offered.</summary>
    Ferial,

    /// <summary>All Souls occupies precedence table I.3, independently of its legacy Feast rank.</summary>
    AllSouls,

    /// <summary>Mary, Mother of the Church takes precedence over another memorial (CDW Notification, March 24, 2018).</summary>
    MotherOfTheChurchOverMemorials,

    /// <summary>Equal-precedence candidates lack an applicable tie rule; the selected candidate is provisional.</summary>
    UnresolvedEqualPrecedence,
}

/// <summary>An occurrence-stage diagnostic; candidate-generation exclusions and transfer provenance are not included.</summary>
public sealed record LiturgicalOccurrence(LiturgicalOccurrenceRule Rule, IReadOnlyList<LiturgicalCelebration> SuppressedCelebrations)
{
    /// <summary>Candidates in an unresolved equal-precedence tie, including the provisional principal.</summary>
    public IReadOnlyList<LiturgicalCelebration> ContendingCelebrations { get; init; } = [];

    /// <summary>A stable identifier within the plan's CAL-02 occurrence rule family.</summary>
    public string RuleId => Rule switch
    {
        LiturgicalOccurrenceRule.Solemnity => "CAL-02/solemnity",
        LiturgicalOccurrenceRule.SundayFeastOfTheLord => "CAL-02/sunday-feast-of-the-lord",
        LiturgicalOccurrenceRule.Sunday => "CAL-02/sunday",
        LiturgicalOccurrenceRule.HolyWeekOrEasterOctave => "CAL-02/holy-week-or-easter-octave",
        LiturgicalOccurrenceRule.Feast => "CAL-02/feast",
        LiturgicalOccurrenceRule.ObligatoryMemorial => "CAL-02/obligatory-memorial",
        LiturgicalOccurrenceRule.MemorialCommemorations => "CAL-02/memorial-commemorations",
        LiturgicalOccurrenceRule.MemorialOptions => "CAL-02/memorial-options",
        LiturgicalOccurrenceRule.Ferial => "CAL-02/ferial",
        LiturgicalOccurrenceRule.AllSouls => "CAL-02/all-souls",
        LiturgicalOccurrenceRule.MotherOfTheChurchOverMemorials => "CAL-02/mother-of-the-church-over-memorials",
        LiturgicalOccurrenceRule.UnresolvedEqualPrecedence => "CAL-02/unresolved-equal-precedence",
        _ => throw new InvalidOperationException($"Unknown occurrence rule: {Rule}."),
    };

    // Recomputed days must compare diagnostic contents, not newly allocated list identities.
    public bool Equals(LiturgicalOccurrence? other) => other is not null
        && Rule == other.Rule
        && SuppressedCelebrations.SequenceEqual(other.SuppressedCelebrations)
        && ContendingCelebrations.SequenceEqual(other.ContendingCelebrations);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Rule);
        foreach (var celebration in SuppressedCelebrations)
        {
            hash.Add(celebration);
        }
        foreach (var celebration in ContendingCelebrations)
        {
            hash.Add(celebration);
        }

        return hash.ToHashCode();
    }
}

internal readonly record struct CelebrationCandidate(LiturgicalCelebration Celebration)
{
    internal LiturgicalPrecedenceClass Precedence => LiturgicalPrecedence.ForCelebration(Celebration);
}

internal sealed record OccurrenceResolution(
    LiturgicalCelebration? Principal,
    IReadOnlyList<LiturgicalCelebration> OptionalMemorials,
    IReadOnlyList<LiturgicalCelebration> MemorialCommemorations,
    LiturgicalOccurrence Decision);

/// <summary>Pure selection from already generated/transferred candidates, preserving the existing bounded precedence policy.</summary>
internal static class LiturgicalOccurrenceResolver
{
    internal static OccurrenceResolution Resolve(DateOnly date, LiturgicalSeason season, IReadOnlyList<CelebrationCandidate> candidates)
    {
        var temporal = LiturgicalPrecedence.ForTemporalDay(date, season);
        CelebrationCandidate? selected = null;
        foreach (var candidate in candidates)
        {
            // The first candidate is retained provisionally; unresolved winning-class ties are diagnosed below.
            if (candidate.Precedence < LiturgicalPrecedenceClass.OptionalMemorial && candidate.Precedence <= temporal
                && (selected is null || candidate.Precedence < selected.Value.Precedence))
            {
                selected = candidate;
            }
        }

        if (selected is { } chosen)
        {
            var equalPrecedenceCandidates = candidates.Where(candidate => candidate.Precedence == chosen.Precedence).ToArray();
            if (equalPrecedenceCandidates.Length > 1)
            {
                var motherOfTheChurch = equalPrecedenceCandidates
                    .Where(candidate => candidate.Celebration.CalendarKey == "mother-of-the-church")
                    .ToArray();
                if (motherOfTheChurch.Length == 1 && equalPrecedenceCandidates.All(candidate => candidate.Celebration.Rank == CelebrationRank.Memorial))
                {
                    return Finish(LiturgicalOccurrenceRule.MotherOfTheChurchOverMemorials, motherOfTheChurch[0].Celebration, []);
                }

                return Finish(LiturgicalOccurrenceRule.UnresolvedEqualPrecedence, chosen.Celebration, [],
                    contenders: equalPrecedenceCandidates.Select(candidate => candidate.Celebration).ToArray());
            }

            var rule = chosen.Celebration.CalendarKey == "all-souls" ? LiturgicalOccurrenceRule.AllSouls
                : chosen.Celebration.Rank == CelebrationRank.Solemnity ? LiturgicalOccurrenceRule.Solemnity
                : chosen.Precedence == LiturgicalPrecedenceClass.FeastOfTheLord && date.DayOfWeek == DayOfWeek.Sunday
                    ? LiturgicalOccurrenceRule.SundayFeastOfTheLord
                : chosen.Celebration.Rank == CelebrationRank.Feast ? LiturgicalOccurrenceRule.Feast
                : LiturgicalOccurrenceRule.ObligatoryMemorial;
            return Finish(rule, chosen.Celebration, []);
        }

        if (date.DayOfWeek == DayOfWeek.Sunday)
        {
            return Finish(LiturgicalOccurrenceRule.Sunday, null, []);
        }

        if (temporal <= LiturgicalPrecedenceClass.PrincipalTemporalDay)
        {
            return Finish(LiturgicalOccurrenceRule.HolyWeekOrEasterOctave, null, []);
        }

        var memorials = candidates
            .Where(c => c.Celebration.Rank is CelebrationRank.Memorial or CelebrationRank.OptionalMemorial)
            .Select(c => c.Celebration)
            .ToArray();
        if (temporal == LiturgicalPrecedenceClass.PrivilegedWeekday)
        {
            return Finish(memorials.Length == 0 ? LiturgicalOccurrenceRule.Ferial : LiturgicalOccurrenceRule.MemorialCommemorations,
                null, [], memorials);
        }

        return Finish(memorials.Length == 0 ? LiturgicalOccurrenceRule.Ferial : LiturgicalOccurrenceRule.MemorialOptions, null, memorials);

        OccurrenceResolution Finish(
            LiturgicalOccurrenceRule rule,
            LiturgicalCelebration? principal,
            IReadOnlyList<LiturgicalCelebration> alternatives,
            IReadOnlyList<LiturgicalCelebration>? commemorations = null,
            IReadOnlyList<LiturgicalCelebration>? contenders = null)
        {
            commemorations ??= [];
            contenders ??= [];
            var suppressed = candidates
                .Select(c => c.Celebration)
                .Where(c => c != principal && !alternatives.Contains(c) && !commemorations.Contains(c) && !contenders.Contains(c))
                .ToArray();
            var decision = new LiturgicalOccurrence(rule, suppressed) { ContendingCelebrations = contenders };
            return new(principal, alternatives, commemorations, decision);
        }
    }
}
