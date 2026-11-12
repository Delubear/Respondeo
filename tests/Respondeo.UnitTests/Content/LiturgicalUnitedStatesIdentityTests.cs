using Respondeo.LiturgicalCalendar;

namespace Respondeo.UnitTests.Content;

public class LiturgicalUnitedStatesIdentityTests
{
    [Theory]
    [InlineData(1, 4, "saint-elizabeth-ann-seton-religious", CelebrationRank.Memorial)]
    [InlineData(1, 5, "saint-john-neumann-bishop", CelebrationRank.Memorial)]
    [InlineData(1, 6, "saint-andre-bessette-religious", CelebrationRank.OptionalMemorial)]
    [InlineData(1, 22, "day-of-prayer-for-unborn-children", CelebrationRank.Memorial)]
    [InlineData(1, 23, "saint-vincent-deacon-and-martyr", CelebrationRank.OptionalMemorial)]
    [InlineData(1, 23, "saint-marianne-cope-virgin", CelebrationRank.OptionalMemorial)]
    [InlineData(3, 3, "saint-katharine-drexel-virgin", CelebrationRank.OptionalMemorial)]
    [InlineData(5, 10, "saint-damien-de-veuster-priest", CelebrationRank.OptionalMemorial)]
    [InlineData(5, 15, "saint-isidore", CelebrationRank.OptionalMemorial)]
    [InlineData(7, 1, "saint-junipero-serra-priest", CelebrationRank.OptionalMemorial)]
    [InlineData(7, 4, "independence-day", CelebrationRank.OptionalMemorial)]
    [InlineData(7, 14, "saint-kateri-tekakwitha-virgin", CelebrationRank.Memorial)]
    [InlineData(7, 18, "saint-camillus-de-lellis-priest", CelebrationRank.OptionalMemorial)]
    [InlineData(8, 18, "saint-jane-frances-de-chantal-religious", CelebrationRank.OptionalMemorial)]
    [InlineData(9, 9, "saint-peter-claver-priest", CelebrationRank.Memorial)]
    [InlineData(10, 6, "blessed-marie-rose-durocher-virgin", CelebrationRank.OptionalMemorial)]
    [InlineData(10, 19, "saints-john-de-brebeuf-and-isaac-jogues-priests-and-companions-martyrs", CelebrationRank.Memorial)]
    [InlineData(10, 20, "saint-paul-of-the-cross-priest", CelebrationRank.OptionalMemorial)]
    [InlineData(11, 13, "saint-frances-xavier-cabrini-virgin", CelebrationRank.Memorial)]
    [InlineData(11, 18, "saint-rose-philippine-duchesne-virgin", CelebrationRank.OptionalMemorial)]
    [InlineData(11, 23, "blessed-miguel-agustin-pro-priest-and-martyr", CelebrationRank.OptionalMemorial)]
    [InlineData(12, 12, "our-lady-of-guadalupe", CelebrationRank.Feast)]
    public void Every_US_proper_celebration_has_an_authored_identity_and_explicit_category(
        int month, int day, string key, CelebrationRank rank)
    {
        var expectedCategory = rank switch
        {
            CelebrationRank.Feast => LiturgicalPrecedenceClass.ProperFeast,
            CelebrationRank.Memorial => LiturgicalPrecedenceClass.ProperMemorial,
            _ => LiturgicalPrecedenceClass.OptionalMemorial,
        };
        foreach (var profile in new[] { "us", "us-thursday" })
        {
            var calendar = LiturgicalCalendars.Create(PreferredLiturgicalCalendar.Deserialize(profile));
            var source = Assert.Single(GeneralRomanCalendarData.ForMonthDay(month, day, calendar.Options.Region), value => value.CalendarKey == key);
            Assert.Equal(rank, source.Rank);
            Assert.Equal(expectedCategory, source.Precedence);
            Assert.Equal(expectedCategory, LiturgicalPrecedence.ForCelebration(source));

            var occurrences = Enumerable.Range(2027, 14).Select(year => calendar.ForDate(new(year, month, day)))
                .SelectMany(date => (date.Celebration is { } principal
                    ? date.OptionalMemorials.Concat(date.MemorialCommemorations).Prepend(principal)
                    : date.OptionalMemorials.Concat(date.MemorialCommemorations)))
                .Where(value => value.CalendarKey == key).ToArray();
            Assert.NotEmpty(occurrences);
            Assert.All(occurrences, actual => Assert.Equal(source, actual));
        }
    }

    [Theory]
    [InlineData(1, 22, 1, 23, "saint-vincent-deacon-and-martyr")]
    [InlineData(7, 14, 7, 18, "saint-camillus-de-lellis-priest")]
    [InlineData(8, 12, 8, 18, "saint-jane-frances-de-chantal-religious")]
    [InlineData(10, 19, 10, 20, "saint-paul-of-the-cross-priest")]
    public void Regional_relocations_share_the_General_identity_and_preserve_the_entire_record(
        int month, int day, int targetMonth, int targetDay, string key)
    {
        var general = Assert.Single(GeneralRomanCalendarData.ForMonthDay(month, day), value => value.CalendarKey == key);
        var relocated = Assert.Single(GeneralRomanCalendarData.ForMonthDay(targetMonth, targetDay, CalendarRegion.UnitedStates),
            value => value.CalendarKey == key);

        Assert.Equal(general, relocated);
        Assert.DoesNotContain(GeneralRomanCalendarData.ForMonthDay(month, day, CalendarRegion.UnitedStates), value => value.CalendarKey == key);
        Assert.DoesNotContain(GeneralRomanCalendarData.ForMonthDay(targetMonth, targetDay), value => value.CalendarKey == key);
    }

    [Theory]
    [InlineData(9, 9, "saint-peter-claver-priest", CelebrationRank.Memorial, LiturgicalPrecedenceClass.ProperMemorial)]
    [InlineData(10, 19, "saints-john-de-brebeuf-and-isaac-jogues-priests-and-companions-martyrs",
        CelebrationRank.Memorial, LiturgicalPrecedenceClass.ProperMemorial)]
    [InlineData(12, 12, "our-lady-of-guadalupe", CelebrationRank.Feast, LiturgicalPrecedenceClass.ProperFeast)]
    public void Regional_rank_overrides_preserve_identity_and_metadata_but_replace_the_category(
        int month, int day, string key, CelebrationRank rank, LiturgicalPrecedenceClass category)
    {
        var general = Assert.Single(GeneralRomanCalendarData.ForMonthDay(month, day), value => value.CalendarKey == key);
        var regional = Assert.Single(GeneralRomanCalendarData.ForMonthDay(month, day, CalendarRegion.UnitedStates), value => value.CalendarKey == key);

        Assert.Equal(CelebrationRank.OptionalMemorial, general.Rank);
        Assert.Equal(LiturgicalPrecedenceClass.OptionalMemorial, general.Precedence);
        Assert.Equal(general with { Rank = rank, Precedence = category }, regional);
        if (key == "our-lady-of-guadalupe")
        {
            Assert.False(string.IsNullOrWhiteSpace(regional.Summary));
        }
    }
}
