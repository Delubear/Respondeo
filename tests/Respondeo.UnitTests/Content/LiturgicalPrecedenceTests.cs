using Respondeo.LiturgicalCalendar;

namespace Respondeo.UnitTests.Content;

public class LiturgicalPrecedenceTests
{
    [Theory]
    [InlineData(2027, 3, 25, LiturgicalSeason.Triduum, LiturgicalPrecedenceClass.PaschalTriduum)]
    [InlineData(2027, 3, 28, LiturgicalSeason.Triduum, LiturgicalPrecedenceClass.PaschalTriduum)]
    [InlineData(2027, 2, 10, LiturgicalSeason.Lent, LiturgicalPrecedenceClass.PrincipalTemporalDay)]
    [InlineData(2027, 3, 22, LiturgicalSeason.Lent, LiturgicalPrecedenceClass.PrincipalTemporalDay)]
    [InlineData(2027, 4, 3, LiturgicalSeason.Easter, LiturgicalPrecedenceClass.PrincipalTemporalDay)]
    [InlineData(2027, 4, 4, LiturgicalSeason.Easter, LiturgicalPrecedenceClass.PrincipalTemporalDay)]
    [InlineData(2027, 4, 5, LiturgicalSeason.Easter, LiturgicalPrecedenceClass.Ferial)]
    [InlineData(2027, 1, 17, LiturgicalSeason.OrdinaryTime, LiturgicalPrecedenceClass.OrdinarySunday)]
    [InlineData(2027, 12, 19, LiturgicalSeason.Advent, LiturgicalPrecedenceClass.PrincipalTemporalDay)]
    [InlineData(2027, 12, 16, LiturgicalSeason.Advent, LiturgicalPrecedenceClass.Ferial)]
    [InlineData(2027, 12, 17, LiturgicalSeason.Advent, LiturgicalPrecedenceClass.PrivilegedWeekday)]
    [InlineData(2027, 12, 27, LiturgicalSeason.Christmas, LiturgicalPrecedenceClass.PrivilegedWeekday)]
    [InlineData(2027, 1, 4, LiturgicalSeason.Christmas, LiturgicalPrecedenceClass.Ferial)]
    [InlineData(2027, 2, 11, LiturgicalSeason.Lent, LiturgicalPrecedenceClass.PrivilegedWeekday)]
    public void Temporal_classes_follow_the_normative_boundaries(int year, int month, int day, LiturgicalSeason season, LiturgicalPrecedenceClass expected)
    {
        var date = new DateOnly(year, month, day);
        var temporalDay = new LiturgicalDay(date, season, LiturgicalColor.White, null);

        Assert.Equal(expected, LiturgicalPrecedence.ForTemporalDay(date, season));
        Assert.Equal(expected, temporalDay.Precedence);
    }

    [Theory]
    [InlineData("general")]
    [InlineData("us")]
    [InlineData("us-thursday")]
    public void Forward_calendars_expose_authored_keys_and_special_categories(string profile)
    {
        var calendar = LiturgicalCalendars.Create(PreferredLiturgicalCalendar.Deserialize(profile));
        (DateOnly Date, string Key, LiturgicalPrecedenceClass Category)[] cases =
        [
            (new(2027, 2, 2), "presentation", LiturgicalPrecedenceClass.FeastOfTheLord),
            (new(2027, 8, 6), "transfiguration", LiturgicalPrecedenceClass.FeastOfTheLord),
            (new(2027, 9, 14), "exaltation-of-the-holy-cross", LiturgicalPrecedenceClass.FeastOfTheLord),
            (new(2027, 11, 9), "lateran-dedication", LiturgicalPrecedenceClass.FeastOfTheLord),
            (new(2031, 11, 2), "all-souls", LiturgicalPrecedenceClass.GeneralSolemnityOrAllSouls),
            (new(2027, 12, 25), "christmas", LiturgicalPrecedenceClass.PrincipalTemporalDay),
            (new(2027, 2, 10), "ash-wednesday", LiturgicalPrecedenceClass.PrincipalTemporalDay),
            (new(2027, 3, 28), "easter-sunday", LiturgicalPrecedenceClass.PaschalTriduum),
            (new(2027, 5, 16), "pentecost", LiturgicalPrecedenceClass.PrincipalTemporalDay),
            (new(2027, 5, 17), "mother-of-the-church", LiturgicalPrecedenceClass.GeneralMemorial),
        ];

        foreach (var item in cases)
        {
            var resolvedDay = calendar.ForDate(item.Date);
            var actual = Assert.IsType<LiturgicalCelebration>(resolvedDay.Celebration);
            Assert.Equal(item.Key, actual.CalendarKey);
            Assert.Equal(item.Category, LiturgicalPrecedence.ForCelebration(actual));
            Assert.Equal(item.Category, resolvedDay.Precedence);
        }

        var allSouls = calendar.ForDate(new(2031, 11, 2));
        Assert.Equal(DayOfWeek.Sunday, allSouls.Date.DayOfWeek);
        Assert.Equal(LiturgicalOccurrenceRule.AllSouls, allSouls.Occurrence!.Rule);
        Assert.Equal("CAL-02/all-souls", allSouls.Occurrence.RuleId);
        Assert.Equal(CelebrationRank.Feast, allSouls.Celebration!.Rank);
        Assert.Empty(allSouls.OptionalMemorials);
    }

    [Theory]
    [InlineData(2027, 1, 17, LiturgicalSeason.OrdinaryTime, LiturgicalPrecedenceClass.GeneralMemorial, false)]
    [InlineData(2027, 1, 18, LiturgicalSeason.OrdinaryTime, LiturgicalPrecedenceClass.ProperFeast, false)]
    [InlineData(2027, 1, 18, LiturgicalSeason.OrdinaryTime, LiturgicalPrecedenceClass.ProperMemorial, true)]
    [InlineData(2027, 1, 18, LiturgicalSeason.OrdinaryTime, LiturgicalPrecedenceClass.OptionalMemorial, true)]
    [InlineData(2027, 1, 18, LiturgicalSeason.OrdinaryTime, LiturgicalPrecedenceClass.PrivilegedWeekday, true)]
    [InlineData(2027, 1, 18, LiturgicalSeason.OrdinaryTime, LiturgicalPrecedenceClass.GeneralMemorial, true)]
    [InlineData(2027, 2, 11, LiturgicalSeason.Lent, LiturgicalPrecedenceClass.GeneralMemorial, true)]
    [InlineData(2027, 2, 10, LiturgicalSeason.Lent, LiturgicalPrecedenceClass.GeneralMemorial, false)]
    public void Solemnity_transfer_destinations_exclude_precedence_classes_one_through_eight(
        int year, int month, int day, LiturgicalSeason season, LiturgicalPrecedenceClass candidateClass, bool expected)
    {
        var date = new DateOnly(year, month, day);
        var rank = candidateClass is LiturgicalPrecedenceClass.ProperFeast or LiturgicalPrecedenceClass.GeneralFeast
            ? CelebrationRank.Feast
            : candidateClass is LiturgicalPrecedenceClass.OptionalMemorial
                ? CelebrationRank.OptionalMemorial
                : CelebrationRank.Memorial;
        var celebration = new LiturgicalCelebration("Candidate", rank, LiturgicalColor.White)
        {
            Precedence = candidateClass,
        };

        Assert.Equal(expected, LiturgicalPrecedence.CanReceiveTransferredSolemnity(date, season, [new(celebration)]));
    }

    [Fact]
    public void Renaming_or_relocating_a_Lord_feast_does_not_change_its_category()
    {
        var original = Assert.Single(GeneralRomanCalendarData.ForMonthDay(8, 6));
        var renamed = original with { Name = "A different display label" };
        var sunday = new DateOnly(2027, 7, 18);
        var actual = LiturgicalOccurrenceResolver.Resolve(sunday, LiturgicalSeason.OrdinaryTime, [new(renamed)]);

        Assert.Equal(renamed, actual.Principal);
        Assert.Equal("transfiguration", actual.Principal!.CalendarKey);
        Assert.Equal(LiturgicalOccurrenceRule.SundayFeastOfTheLord, actual.Decision.Rule);
        Assert.Empty(actual.Decision.SuppressedCelebrations);

        var lentenSunday = LiturgicalOccurrenceResolver.Resolve(new(2027, 2, 14), LiturgicalSeason.Lent, [new(renamed)]);
        Assert.Null(lentenSunday.Principal);
        Assert.Equal([renamed], lentenSunday.Decision.SuppressedCelebrations);
    }

    [Fact]
    public void An_ordinary_feast_does_not_replace_Sunday_even_at_a_former_whitelisted_date()
    {
        var ordinaryFeast = new LiturgicalCelebration("Not a feast of the Lord", CelebrationRank.Feast, LiturgicalColor.Red);
        var actual = LiturgicalOccurrenceResolver.Resolve(new(2028, 8, 6), LiturgicalSeason.OrdinaryTime, [new(ordinaryFeast)]);

        Assert.Null(actual.Principal);
        Assert.Equal(LiturgicalOccurrenceRule.Sunday, actual.Decision.Rule);
        Assert.Equal([ordinaryFeast], actual.Decision.SuppressedCelebrations);
    }

    [Fact]
    public void Ash_Wednesday_excludes_memorials_even_without_a_named_candidate()
    {
        var memorial = new LiturgicalCelebration("Memorial", CelebrationRank.Memorial, LiturgicalColor.Red);
        var actual = LiturgicalOccurrenceResolver.Resolve(new(2027, 2, 10), LiturgicalSeason.Lent, [new(memorial)]);

        Assert.Null(actual.Principal);
        Assert.Empty(actual.OptionalMemorials);
        Assert.Equal([memorial], actual.Decision.SuppressedCelebrations);
    }

    [Fact]
    public void Concurrence_uses_category_not_a_special_date_or_display_name()
    {
        var today = new LiturgicalDay(new(2027, 1, 12), LiturgicalSeason.OrdinaryTime, LiturgicalColor.White,
            new("First solemnity", CelebrationRank.Solemnity, LiturgicalColor.White));
        var tomorrow = new LiturgicalDay(new(2027, 1, 13), LiturgicalSeason.OrdinaryTime, LiturgicalColor.White,
            new("Renamed principal temporal celebration", CelebrationRank.Solemnity, LiturgicalColor.White)
            {
                CalendarKey = "epiphany",
                Precedence = LiturgicalPrecedenceClass.PrincipalTemporalDay,
            });

        var actual = LiturgicalEveningResolver.Resolve(today, tomorrow, LiturgicalCalendarOptions.Default);

        Assert.Equal(EveningPrayerPhase.FirstVespers, actual.Phase);
        Assert.Equal(tomorrow, actual.OfficeDay);
    }

    [Theory]
    [InlineData(2033, 8, 6)]
    [InlineData(2030, 9, 14)]
    public void A_Saturday_feast_has_no_Second_Vespers_to_displace_Sunday(int year, int month, int day)
    {
        var calendar = LiturgicalCalendars.Create(LiturgicalCalendarOptions.Default);
        var date = new DateOnly(year, month, day);
        var feast = calendar.ForDate(date);
        var actual = calendar.ForEvening(date);

        Assert.Equal(DayOfWeek.Saturday, date.DayOfWeek);
        Assert.Equal(LiturgicalPrecedenceClass.FeastOfTheLord, feast.Celebration!.Precedence);
        Assert.Equal(EveningPrayerPhase.FirstVespers, actual.Phase);
        Assert.Equal(date.AddDays(1), actual.OfficeDay.Date);
    }
}
