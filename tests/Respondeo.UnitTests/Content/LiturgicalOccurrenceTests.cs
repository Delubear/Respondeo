using Respondeo.LiturgicalCalendar;

namespace Respondeo.UnitTests.Content;

public class LiturgicalOccurrenceTests
{
    public static TheoryData<string> Profiles => ["general", "us", "us-thursday"];

    [Theory]
    [MemberData(nameof(Profiles))]
    public void Calendar_exposes_the_decisive_rule_without_changing_observances(string profile)
    {
        var calendar = LiturgicalCalendars.Create(PreferredLiturgicalCalendar.Deserialize(profile));
        (DateOnly Date, LiturgicalOccurrenceRule Rule, string RuleId, string? Principal)[] cases =
        [
            (new(2027, 1, 1), LiturgicalOccurrenceRule.Solemnity, "CAL-02/solemnity", "Mary, the Holy Mother of God"),
            (new(2027, 1, 10), LiturgicalOccurrenceRule.SundayFeastOfTheLord, "CAL-02/sunday-feast-of-the-lord", "The Baptism of the Lord"),
            (new(2027, 7, 25), LiturgicalOccurrenceRule.Sunday, "CAL-02/sunday", null),
            (new(2027, 3, 23), LiturgicalOccurrenceRule.HolyWeekOrEasterOctave, "CAL-02/holy-week-or-easter-octave", null),
            (new(2027, 2, 22), LiturgicalOccurrenceRule.Feast, "CAL-02/feast", "The Chair of Saint Peter the Apostle"),
            (new(2028, 6, 5), LiturgicalOccurrenceRule.MotherOfTheChurchOverMemorials, "CAL-02/mother-of-the-church-over-memorials", "The Blessed Virgin Mary, Mother of the Church"),
            (new(2027, 6, 5), LiturgicalOccurrenceRule.MemorialOptions, "CAL-02/memorial-options", null),
            (new(2027, 1, 11), LiturgicalOccurrenceRule.Ferial, "CAL-02/ferial", null),
        ];

        foreach (var item in cases)
        {
            var day = calendar.ForDate(item.Date);
            var decision = Assert.IsType<LiturgicalOccurrence>(day.Occurrence);

            Assert.Equal(item.Rule, decision.Rule);
            Assert.Equal(item.RuleId, decision.RuleId);
            Assert.Equal(item.Principal, day.Celebration?.Name);
        }

        var motherOfTheChurch = calendar.ForDate(new(2028, 6, 5));
        Assert.Equal("Saint Boniface, Bishop and Martyr", Assert.Single(motherOfTheChurch.Occurrence!.SuppressedCelebrations).Name);
        Assert.Empty(motherOfTheChurch.OptionalMemorials);
        Assert.Equal("Saint James, Apostle", Assert.Single(calendar.ForDate(new(2027, 7, 25)).Occurrence!.SuppressedCelebrations).Name);
        Assert.Equal(2, calendar.ForDate(new(2027, 6, 5)).OptionalMemorials.Count);
        Assert.Empty(calendar.ForDate(new(2027, 6, 5)).Occurrence!.SuppressedCelebrations);
    }

    [Fact]
    public void Solemnity_precedes_feasts_and_memorials_and_reports_each_suppression()
    {
        var optional = Candidate("Optional", CelebrationRank.OptionalMemorial);
        var memorial = Candidate("Memorial", CelebrationRank.Memorial);
        var feast = Candidate("Feast", CelebrationRank.Feast);
        var solemnity = Candidate("Solemnity", CelebrationRank.Solemnity);

        var actual = LiturgicalOccurrenceResolver.Resolve(new(2027, 1, 11), LiturgicalSeason.OrdinaryTime, [optional, memorial, feast, solemnity]);

        Assert.Equal(solemnity.Celebration, actual.Principal);
        Assert.Empty(actual.OptionalMemorials);
        Assert.Equal(LiturgicalOccurrenceRule.Solemnity, actual.Decision.Rule);
        Assert.Equal([optional.Celebration, memorial.Celebration, feast.Celebration], actual.Decision.SuppressedCelebrations);
    }

    [Fact]
    public void Privileged_Sunday_rejects_an_impeded_non_dominical_solemnity()
    {
        var solemnity = Candidate("Transferred elsewhere by candidate generation", CelebrationRank.Solemnity);
        var memorial = Candidate("Memorial", CelebrationRank.Memorial);

        var actual = LiturgicalOccurrenceResolver.Resolve(new(2027, 2, 14), LiturgicalSeason.Lent, [solemnity, memorial]);

        Assert.Null(actual.Principal);
        Assert.Empty(actual.OptionalMemorials);
        Assert.Equal(LiturgicalOccurrenceRule.Sunday, actual.Decision.Rule);
        Assert.Equal([solemnity.Celebration, memorial.Celebration], actual.Decision.SuppressedCelebrations);
    }

    [Theory]
    [InlineData(CelebrationRank.Feast, LiturgicalOccurrenceRule.Feast)]
    [InlineData(CelebrationRank.Memorial, LiturgicalOccurrenceRule.ObligatoryMemorial)]
    public void Higher_rank_excludes_competing_memorial_options(CelebrationRank rank, LiturgicalOccurrenceRule rule)
    {
        var optional = Candidate("Optional", CelebrationRank.OptionalMemorial);
        var principal = Candidate("Principal", rank);

        var actual = LiturgicalOccurrenceResolver.Resolve(new(2027, 1, 11), LiturgicalSeason.OrdinaryTime, [optional, principal]);

        Assert.Equal(principal.Celebration, actual.Principal);
        Assert.Empty(actual.OptionalMemorials);
        Assert.Equal(rule, actual.Decision.Rule);
        Assert.Equal([optional.Celebration], actual.Decision.SuppressedCelebrations);
    }

    [Theory]
    [InlineData(2, 9, LiturgicalSeason.OrdinaryTime, true)]
    [InlineData(2, 11, LiturgicalSeason.Lent, false)]
    [InlineData(12, 16, LiturgicalSeason.Advent, true)]
    [InlineData(12, 17, LiturgicalSeason.Advent, false)]
    [InlineData(12, 31, LiturgicalSeason.Christmas, false)]
    [InlineData(1, 4, LiturgicalSeason.Christmas, true)]
    public void Privileged_weekday_boundaries_separate_full_memorial_options_from_commemorations(
        int month, int day, LiturgicalSeason season, bool obligatory)
    {
        var memorial = Candidate("Memorial", CelebrationRank.Memorial);
        var optional = Candidate("Optional", CelebrationRank.OptionalMemorial);

        var actual = LiturgicalOccurrenceResolver.Resolve(new(2027, month, day), season, [memorial, optional]);

        if (obligatory)
        {
            Assert.Equal(memorial.Celebration, actual.Principal);
            Assert.Equal(LiturgicalOccurrenceRule.ObligatoryMemorial, actual.Decision.Rule);
            Assert.Empty(actual.OptionalMemorials);
            Assert.Empty(actual.MemorialCommemorations);
            Assert.Equal([optional.Celebration], actual.Decision.SuppressedCelebrations);
        }
        else
        {
            Assert.Null(actual.Principal);
            Assert.Equal(LiturgicalOccurrenceRule.MemorialCommemorations, actual.Decision.Rule);
            Assert.Equal("CAL-02/memorial-commemorations", actual.Decision.RuleId);
            Assert.Empty(actual.OptionalMemorials);
            Assert.Equal([memorial.Celebration, optional.Celebration], actual.MemorialCommemorations);
            Assert.Empty(actual.Decision.SuppressedCelebrations);
        }
    }

    [Theory]
    [InlineData(3, 20, LiturgicalOccurrenceRule.Feast)]
    [InlineData(3, 22, LiturgicalOccurrenceRule.HolyWeekOrEasterOctave)]
    [InlineData(3, 27, LiturgicalOccurrenceRule.HolyWeekOrEasterOctave)]
    [InlineData(3, 29, LiturgicalOccurrenceRule.HolyWeekOrEasterOctave)]
    [InlineData(4, 3, LiturgicalOccurrenceRule.HolyWeekOrEasterOctave)]
    [InlineData(4, 5, LiturgicalOccurrenceRule.Feast)]
    public void Holy_Week_and_octave_weekdays_precede_a_feast(int month, int day, LiturgicalOccurrenceRule rule)
    {
        var feast = Candidate("Feast", CelebrationRank.Feast);
        var season = month == 3 && day < 28 ? LiturgicalSeason.Lent : LiturgicalSeason.Easter;

        var actual = LiturgicalOccurrenceResolver.Resolve(new(2027, month, day), season, [feast]);

        Assert.Equal(rule, actual.Decision.Rule);
        Assert.Empty(actual.OptionalMemorials);
        if (rule == LiturgicalOccurrenceRule.Feast)
        {
            Assert.Equal(feast.Celebration, actual.Principal);
            Assert.Empty(actual.Decision.SuppressedCelebrations);
        }
        else
        {
            Assert.Null(actual.Principal);
            Assert.Equal([feast.Celebration], actual.Decision.SuppressedCelebrations);
        }
    }

    [Theory]
    [InlineData(CelebrationRank.Solemnity, LiturgicalPrecedenceClass.PrincipalTemporalDay)]
    [InlineData(CelebrationRank.Feast, LiturgicalPrecedenceClass.FeastOfTheLord)]
    public void Explicit_precedence_wins_and_unresolved_ties_are_reported(
        CelebrationRank rank, LiturgicalPrecedenceClass precedence)
    {
        var first = Candidate("First", rank);
        var second = Candidate("Second", rank);
        var lower = Candidate("Lower precedence", rank == CelebrationRank.Solemnity ? CelebrationRank.Feast : CelebrationRank.Memorial);
        var higher = new CelebrationCandidate(new("Renamed higher-priority celebration", rank, LiturgicalColor.White) { Precedence = precedence });
        var date = new DateOnly(2027, 1, 11);

        var ordinaryTie = LiturgicalOccurrenceResolver.Resolve(date, LiturgicalSeason.OrdinaryTime, [first, lower, second]);
        var reverseTie = LiturgicalOccurrenceResolver.Resolve(date, LiturgicalSeason.OrdinaryTime, [second, lower, first]);
        var higherFirst = LiturgicalOccurrenceResolver.Resolve(date, LiturgicalSeason.OrdinaryTime, [higher, first]);
        var higherLast = LiturgicalOccurrenceResolver.Resolve(date, LiturgicalSeason.OrdinaryTime, [first, higher]);

        Assert.Equal(first.Celebration, ordinaryTie.Principal);
        Assert.Equal(LiturgicalOccurrenceRule.UnresolvedEqualPrecedence, ordinaryTie.Decision.Rule);
        Assert.Equal("CAL-02/unresolved-equal-precedence", ordinaryTie.Decision.RuleId);
        Assert.Equal([lower.Celebration], ordinaryTie.Decision.SuppressedCelebrations);
        Assert.Equal([first.Celebration, second.Celebration], ordinaryTie.Decision.ContendingCelebrations);
        Assert.Equal(second.Celebration, reverseTie.Principal);
        Assert.Equal(LiturgicalOccurrenceRule.UnresolvedEqualPrecedence, reverseTie.Decision.Rule);
        Assert.Equal("CAL-02/unresolved-equal-precedence", reverseTie.Decision.RuleId);
        Assert.Equal([lower.Celebration], reverseTie.Decision.SuppressedCelebrations);
        Assert.Equal([second.Celebration, first.Celebration], reverseTie.Decision.ContendingCelebrations);
        Assert.Equal(higher.Celebration, higherFirst.Principal);
        Assert.Equal(higher.Celebration, higherLast.Principal);
        var expectedHigherRule = rank == CelebrationRank.Solemnity
            ? LiturgicalOccurrenceRule.Solemnity
            : LiturgicalOccurrenceRule.Feast;
        Assert.Equal(expectedHigherRule, higherFirst.Decision.Rule);
        Assert.Equal(expectedHigherRule, higherLast.Decision.Rule);
        Assert.Equal([first.Celebration], higherFirst.Decision.SuppressedCelebrations);
        Assert.Equal([first.Celebration], higherLast.Decision.SuppressedCelebrations);
    }

    [Theory]
    [InlineData("general", 60, 4)]
    [InlineData("us", 63, 10)]
    [InlineData("us-thursday", 63, 10)]
    public void Corpus_Christi_and_the_Nativity_of_John_remain_equal_precedence_candidates(
        string profile, int corpusChristiOffset, int expectedCollisionCount)
    {
        var calendar = LiturgicalCalendars.Create(PreferredLiturgicalCalendar.Deserialize(profile));
        var collisionCount = 0;

        for (var year = 2000; year < 2400; year++)
        {
            var date = new DateOnly(year, 6, 24);
            if (Computus.GregorianEaster(year).AddDays(corpusChristiOffset) != date)
            {
                continue;
            }

            collisionCount++;
            var day = calendar.ForDate(date);
            var contenders = day.Occurrence!.ContendingCelebrations;

            Assert.Equal(LiturgicalOccurrenceRule.UnresolvedEqualPrecedence, day.Occurrence.Rule);
            Assert.Equal(2, contenders.Count);
            Assert.Contains(contenders, celebration => celebration.CalendarKey == "corpus-christi");
            Assert.Contains(contenders, celebration => celebration.CalendarKey == "nativity-of-saint-john-the-baptist");
            Assert.Contains(contenders, celebration => celebration.CalendarKey == day.Celebration?.CalendarKey);
            Assert.Empty(day.Occurrence.SuppressedCelebrations);
            Assert.Empty(day.Transfers);
        }

        Assert.Equal(expectedCollisionCount, collisionCount);
    }

    [Fact]
    public void Mother_of_the_Church_wins_memorial_ties_independent_of_candidate_order()
    {
        var boniface = Candidate("Boniface", CelebrationRank.Memorial);
        var motherOfTheChurch = new CelebrationCandidate(new("Mother of the Church", CelebrationRank.Memorial, LiturgicalColor.White)
        {
            CalendarKey = "mother-of-the-church",
        });
        var anotherMemorial = Candidate("Another memorial", CelebrationRank.Memorial);
        var date = new DateOnly(2027, 1, 11);

        var motherFirst = LiturgicalOccurrenceResolver.Resolve(
            date, LiturgicalSeason.OrdinaryTime, [motherOfTheChurch, boniface, anotherMemorial]);
        var motherLast = LiturgicalOccurrenceResolver.Resolve(
            date, LiturgicalSeason.OrdinaryTime, [boniface, anotherMemorial, motherOfTheChurch]);

        Assert.Equal(motherOfTheChurch.Celebration, motherFirst.Principal);
        Assert.Equal(motherOfTheChurch.Celebration, motherLast.Principal);
        Assert.Equal(LiturgicalOccurrenceRule.MotherOfTheChurchOverMemorials, motherFirst.Decision.Rule);
        Assert.Equal("CAL-02/mother-of-the-church-over-memorials", motherFirst.Decision.RuleId);
        Assert.Equal(motherFirst.Decision.Rule, motherLast.Decision.Rule);
        Assert.Equal([boniface.Celebration, anotherMemorial.Celebration], motherFirst.Decision.SuppressedCelebrations);
        Assert.Equal([boniface.Celebration, anotherMemorial.Celebration], motherLast.Decision.SuppressedCelebrations);
        Assert.Empty(motherFirst.OptionalMemorials);
        Assert.Empty(motherLast.OptionalMemorials);
    }

    [Fact]
    public void Caller_supplied_days_do_not_claim_engine_diagnostics()
    {
        var day = new LiturgicalDay(new(2027, 1, 11), LiturgicalSeason.OrdinaryTime, LiturgicalColor.Green, null);

        Assert.Null(day.Occurrence);
    }

    [Theory]
    [InlineData(2027, 7, 25)]
    [InlineData(2028, 6, 5)]
    public void Recomputed_days_preserve_value_equality_with_suppressed_candidates(int year, int month, int day)
    {
        var calendar = LiturgicalCalendars.Create(LiturgicalCalendarOptions.Default);
        var date = new DateOnly(year, month, day);
        var first = calendar.ForDate(date);
        var second = calendar.ForDate(date);

        Assert.Single(first.Occurrence!.SuppressedCelebrations);
        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Diagnostic_equality_compares_rule_and_ordered_celebration_values()
    {
        var first = Candidate("First", CelebrationRank.Feast).Celebration;
        var second = Candidate("Second", CelebrationRank.Memorial).Celebration;
        var decision = new LiturgicalOccurrence(LiturgicalOccurrenceRule.Sunday, new[] { first, second });
        var equivalent = new LiturgicalOccurrence(LiturgicalOccurrenceRule.Sunday, new[] { first with { }, second with { } });

        Assert.Equal(decision, equivalent);
        Assert.Equal(decision.GetHashCode(), equivalent.GetHashCode());
        Assert.NotEqual(decision, decision with { Rule = LiturgicalOccurrenceRule.Feast });
        Assert.NotEqual(decision, decision with { SuppressedCelebrations = [second, first] });
        Assert.NotEqual(decision, decision with { SuppressedCelebrations = [first] });
        Assert.NotEqual(decision, decision with { SuppressedCelebrations = [first, second with { Name = "Changed" }] });
        Assert.NotEqual(decision, decision with { ContendingCelebrations = [first] });
        Assert.False(decision.Equals(null));
    }

    private static CelebrationCandidate Candidate(string name, CelebrationRank rank) => new(new(name, rank, LiturgicalColor.White));
}
