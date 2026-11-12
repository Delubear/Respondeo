using Respondeo.LiturgicalCalendar;
using Respondeo.LiturgyOfTheHours;

namespace Respondeo.UnitTests.Content;

public class LiturgicalFixedFeastIdentityTests
{
    [Theory]
    [InlineData(1, 1, "mary-the-holy-mother-of-god", CelebrationRank.Solemnity, LiturgicalColor.White)]
    [InlineData(1, 25, "the-conversion-of-saint-paul-the-apostle", CelebrationRank.Feast, LiturgicalColor.White)]
    [InlineData(2, 22, "the-chair-of-saint-peter-the-apostle", CelebrationRank.Feast, LiturgicalColor.White)]
    [InlineData(4, 25, "saint-mark-evangelist", CelebrationRank.Feast, LiturgicalColor.Red)]
    [InlineData(5, 3, "saints-philip-and-james-apostles", CelebrationRank.Feast, LiturgicalColor.Red)]
    [InlineData(5, 14, "saint-matthias-apostle", CelebrationRank.Feast, LiturgicalColor.Red)]
    [InlineData(5, 31, "the-visitation-of-the-blessed-virgin-mary", CelebrationRank.Feast, LiturgicalColor.White)]
    [InlineData(7, 3, "saint-thomas-apostle", CelebrationRank.Feast, LiturgicalColor.Red)]
    [InlineData(7, 22, "saint-mary-magdalene", CelebrationRank.Feast, LiturgicalColor.White)]
    [InlineData(7, 25, "saint-james-apostle", CelebrationRank.Feast, LiturgicalColor.Red)]
    [InlineData(8, 10, "saint-lawrence-deacon-and-martyr", CelebrationRank.Feast, LiturgicalColor.Red)]
    [InlineData(8, 15, "the-assumption-of-the-blessed-virgin-mary", CelebrationRank.Solemnity, LiturgicalColor.White)]
    [InlineData(8, 24, "saint-bartholomew-apostle", CelebrationRank.Feast, LiturgicalColor.Red)]
    [InlineData(9, 8, "the-nativity-of-the-blessed-virgin-mary", CelebrationRank.Feast, LiturgicalColor.White)]
    [InlineData(9, 21, "saint-matthew-apostle-and-evangelist", CelebrationRank.Feast, LiturgicalColor.Red)]
    [InlineData(9, 29, "saints-michael-gabriel-and-raphael-archangels", CelebrationRank.Feast, LiturgicalColor.White)]
    [InlineData(10, 18, "saint-luke-evangelist", CelebrationRank.Feast, LiturgicalColor.Red)]
    [InlineData(10, 28, "saints-simon-and-jude-apostles", CelebrationRank.Feast, LiturgicalColor.Red)]
    [InlineData(11, 1, "all-saints", CelebrationRank.Solemnity, LiturgicalColor.White)]
    [InlineData(11, 30, "saint-andrew-apostle", CelebrationRank.Feast, LiturgicalColor.Red)]
    [InlineData(12, 26, "saint-stephen-the-first-martyr", CelebrationRank.Feast, LiturgicalColor.Red)]
    [InlineData(12, 27, "saint-john-apostle-and-evangelist", CelebrationRank.Feast, LiturgicalColor.White)]
    [InlineData(12, 28, "the-holy-innocents-martyrs", CelebrationRank.Feast, LiturgicalColor.Red)]
    public void Remaining_fixed_feasts_preserve_identity_metadata_and_precedence_across_profiles(
        int month, int day, string key, CelebrationRank rank, LiturgicalColor color)
    {
        var source = Assert.Single(GeneralRomanCalendarData.ForMonthDay(month, day));
        var category = rank == CelebrationRank.Solemnity
            ? LiturgicalPrecedenceClass.GeneralSolemnityOrAllSouls : LiturgicalPrecedenceClass.GeneralFeast;
        Assert.Equal(key, source.CalendarKey);
        Assert.Equal(rank, source.Rank);
        Assert.Equal(color, source.Color);
        Assert.Equal(category, source.Precedence);

        foreach (var profile in new[] { "general", "us", "us-thursday" })
        {
            var calendar = LiturgicalCalendars.Create(PreferredLiturgicalCalendar.Deserialize(profile));
            Assert.Equal(source, Assert.Single(GeneralRomanCalendarData.ForMonthDay(month, day, calendar.Options.Region)));
            var occurrences = Enumerable.Range(2027, 14).Select(year => calendar.ForDate(new(year, month, day)))
                .Where(date => date.Celebration?.CalendarKey == key).ToArray();
            Assert.NotEmpty(occurrences);
            Assert.All(occurrences, actual =>
            {
                Assert.Equal(source, actual.Celebration);
                Assert.Equal(color, actual.Color);
                Assert.Equal(category, actual.Precedence);
            });
        }

        var renamed = source with { Name = "Renamed fixed celebration", Id = "unrelated-profile" };
        var sunday = LiturgicalOccurrenceResolver.Resolve(new(2027, 7, 18), LiturgicalSeason.OrdinaryTime, [new(renamed)]);
        if (rank == CelebrationRank.Solemnity)
        {
            Assert.Equal(renamed, sunday.Principal);
            Assert.Equal(LiturgicalOccurrenceRule.Solemnity, sunday.Decision.Rule);
            Assert.Empty(sunday.Decision.SuppressedCelebrations);
        }
        else
        {
            Assert.Null(sunday.Principal);
            Assert.Equal(LiturgicalOccurrenceRule.Sunday, sunday.Decision.Rule);
            Assert.Equal([renamed], sunday.Decision.SuppressedCelebrations);
        }

        var lent = LiturgicalOccurrenceResolver.Resolve(new(2027, 2, 14), LiturgicalSeason.Lent, [new(renamed)]);
        Assert.Null(lent.Principal);
        Assert.Empty(lent.OptionalMemorials);
        Assert.Equal([renamed], lent.Decision.SuppressedCelebrations);
    }

    [Fact]
    public void Every_General_fixed_celebration_has_an_authored_identity_and_rank_category()
    {
        var entries = Enumerable.Range(0, 366).Select(offset => new DateOnly(2000, 1, 1).AddDays(offset))
            .SelectMany(date => GeneralRomanCalendarData.ForMonthDay(date.Month, date.Day)).ToArray();

        Assert.Equal(222, entries.Length);
        Assert.Equal(entries.Length, entries.Select(value => value.CalendarKey).Distinct().Count());
        Assert.All(entries, value =>
        {
            Assert.NotNull(value.CalendarKey);
            Assert.Matches("^[a-z0-9]+(-[a-z0-9]+)*$", value.CalendarKey);
            Assert.NotNull(value.Precedence);
            var hoursId = OfficeData.Sanctoral.CalendarBindings.GetValueOrDefault(value.CalendarKey, value.CalendarKey);
            Assert.Equal(OrdinaryFormOffice.CelebrationId(value.Name), hoursId);
        });
    }

    [Theory]
    [InlineData("general", 35)]
    [InlineData("us", 36)]
    [InlineData("us-thursday", 36)]
    public void Every_bundled_fixed_solemnity_and_feast_has_explicit_identity_and_precedence(string profile, int expectedCount)
    {
        var calendar = LiturgicalCalendars.Create(PreferredLiturgicalCalendar.Deserialize(profile));
        var entries = Enumerable.Range(0, 366).Select(offset => new DateOnly(2000, 1, 1).AddDays(offset))
            .SelectMany(date => GeneralRomanCalendarData.ForMonthDay(date.Month, date.Day, calendar.Options.Region))
            .Where(value => value.Rank is CelebrationRank.Solemnity or CelebrationRank.Feast).ToArray();

        Assert.Equal(expectedCount, entries.Length);
        Assert.Equal(expectedCount, entries.Select(value => value.CalendarKey).Distinct().Count());
        Assert.All(entries, value =>
        {
            Assert.NotNull(value.CalendarKey);
            Assert.Matches("^[a-z0-9]+(-[a-z0-9]+)*$", value.CalendarKey);
            Assert.NotNull(value.Precedence);
        });
    }
}
