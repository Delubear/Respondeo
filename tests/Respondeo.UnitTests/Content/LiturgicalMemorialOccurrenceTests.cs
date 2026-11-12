using Respondeo.LiturgicalCalendar;

namespace Respondeo.UnitTests.Content;

public class LiturgicalMemorialOccurrenceTests
{
    public static TheoryData<string> Profiles => ["general", "us", "us-thursday"];

    [Theory]
    [MemberData(nameof(Profiles))]
    public void Mother_of_the_Church_suppresses_competing_memorials_instead_of_offering_them(string profile)
    {
        var calendar = CreateCalendar(profile);
        DateOnly[] dates = [new(2027, 5, 17), new(2028, 6, 5), new(2029, 5, 21), new(2033, 6, 6), new(2037, 5, 25)];

        foreach (var date in dates)
        {
            var actual = calendar.ForDate(date);

            Assert.Equal(new LiturgicalCelebration("The Blessed Virgin Mary, Mother of the Church", CelebrationRank.Memorial, LiturgicalColor.White)
            {
                CalendarKey = "mother-of-the-church",
                Precedence = LiturgicalPrecedenceClass.GeneralMemorial,
            }, actual.Celebration);
            Assert.Equal(LiturgicalSeason.OrdinaryTime, actual.Season);
            Assert.Equal(LiturgicalColor.White, actual.Color);
            Assert.Null(actual.FerialName);
            Assert.Empty(actual.OptionalMemorials);
        }
    }

    [Theory]
    [MemberData(nameof(Profiles))]
    public void Mother_of_the_Church_suppresses_every_fixed_memorial_collision_across_a_gregorian_cycle(
        string profile)
    {
        var calendar = CreateCalendar(profile);
        var collisionCount = 0;
        var obligatoryMemorialCollisionCount = 0;

        for (var year = 2000; year < 2400; year++)
        {
            var date = Computus.GregorianEaster(year).AddDays(50);
            var fixedMemorials = GeneralRomanCalendarData.ForMonthDay(date.Month, date.Day, calendar.Options.Region)
                .Where(celebration => celebration.Rank is CelebrationRank.Memorial or CelebrationRank.OptionalMemorial)
                .ToArray();
            if (fixedMemorials.Length == 0)
            {
                continue;
            }

            collisionCount++;
            var obligatoryMemorials = fixedMemorials
                .Where(celebration => celebration.Rank == CelebrationRank.Memorial)
                .ToArray();
            if (obligatoryMemorials.Length > 0)
            {
                obligatoryMemorialCollisionCount++;
            }

            var actual = calendar.ForDate(date);
            Assert.Equal("mother-of-the-church", actual.Celebration?.CalendarKey);
            Assert.Equal(obligatoryMemorials.Length > 0
                ? LiturgicalOccurrenceRule.MotherOfTheChurchOverMemorials
                : LiturgicalOccurrenceRule.ObligatoryMemorial, actual.Occurrence?.Rule);
            Assert.Equal(LiturgicalSeason.OrdinaryTime, actual.Season);
            Assert.Empty(actual.OptionalMemorials);
            foreach (var fixedMemorial in fixedMemorials)
            {
                Assert.Contains(actual.Occurrence!.SuppressedCelebrations, celebration =>
                    celebration.CalendarKey == fixedMemorial.CalendarKey);
            }
        }

        Assert.Equal(profile == "general" ? 209 : 222, collisionCount);
        Assert.Equal(73, obligatoryMemorialCollisionCount);
    }

    [Theory]
    [MemberData(nameof(Profiles))]
    public void Immaculate_Heart_collision_still_reduces_both_obligatory_memorials_to_options(string profile)
    {
        // USCCB 2027, printed p. 7 note 6c: this differs from the Mother of the Church rule.
        var actual = CreateCalendar(profile).ForDate(new DateOnly(2027, 6, 5));

        Assert.Null(actual.Celebration);
        Assert.Equal(LiturgicalSeason.OrdinaryTime, actual.Season);
        Assert.Equal(LiturgicalColor.Green, actual.Color);
        Assert.Collection(actual.OptionalMemorials,
            memorial => Assert.Equal(new LiturgicalCelebration("The Immaculate Heart of the Blessed Virgin Mary", CelebrationRank.OptionalMemorial, LiturgicalColor.White)
            {
                CalendarKey = "immaculate-heart",
            }, memorial),
            memorial => Assert.Equal(new LiturgicalCelebration("Saint Boniface, Bishop and Martyr", CelebrationRank.OptionalMemorial, LiturgicalColor.Red)
            {
                CalendarKey = "saint-boniface-bishop-and-martyr",
            }, memorial));
        Assert.Collection(actual.RankAdjustments,
            adjustment =>
            {
                Assert.Equal("immaculate-heart", adjustment.Celebration.CalendarKey);
                Assert.Equal(CelebrationRank.Memorial, adjustment.OriginalRank);
                Assert.Equal(CelebrationRank.OptionalMemorial, adjustment.AdjustedRank);
                Assert.Equal(LiturgicalRankAdjustmentRule.ImmaculateHeartMemorialCollision, adjustment.Rule);
                Assert.Equal("CAL-02/immaculate-heart-memorial-collision", adjustment.RuleId);
            },
            adjustment =>
            {
                Assert.Equal("saint-boniface-bishop-and-martyr", adjustment.Celebration.CalendarKey);
                Assert.Equal(CelebrationRank.Memorial, adjustment.OriginalRank);
                Assert.Equal(CelebrationRank.OptionalMemorial, adjustment.AdjustedRank);
                Assert.Equal(LiturgicalRankAdjustmentRule.ImmaculateHeartMemorialCollision, adjustment.Rule);
            });
        Assert.NotEqual(actual, actual with { RankAdjustments = [] });
    }

    [Theory]
    [MemberData(nameof(Profiles))]
    public void Immaculate_Heart_reduces_every_coinciding_obligatory_memorial_to_an_option_across_a_gregorian_cycle(
        string profile)
    {
        var calendar = CreateCalendar(profile);
        var collisionCount = 0;

        for (var year = 2000; year < 2400; year++)
        {
            var date = Computus.GregorianEaster(year).AddDays(69);
            var fixedMemorials = GeneralRomanCalendarData.ForMonthDay(date.Month, date.Day, calendar.Options.Region)
                .Where(celebration => celebration.Rank == CelebrationRank.Memorial)
                .ToArray();
            if (fixedMemorials.Length == 0)
            {
                continue;
            }

            collisionCount++;
            var actual = calendar.ForDate(date);
            Assert.Null(actual.Celebration);
            Assert.Equal(LiturgicalOccurrenceRule.MemorialOptions, actual.Occurrence?.Rule);
            Assert.Contains(actual.OptionalMemorials, celebration =>
                celebration.CalendarKey == "immaculate-heart" && celebration.Rank == CelebrationRank.OptionalMemorial);
            foreach (var fixedMemorial in fixedMemorials)
            {
                Assert.Contains(actual.OptionalMemorials, celebration =>
                    celebration.CalendarKey == fixedMemorial.CalendarKey
                    && celebration.Rank == CelebrationRank.OptionalMemorial);
            }
        }

        Assert.Equal(80, collisionCount);
    }

    [Theory]
    [MemberData(nameof(Profiles))]
    public void A_higher_ranking_feast_still_suppresses_Mother_of_the_Church(string profile)
    {
        var actual = CreateCalendar(profile).ForDate(new DateOnly(2035, 5, 14));

        Assert.Equal("Saint Matthias, Apostle", actual.Celebration?.Name);
        Assert.Equal(CelebrationRank.Feast, actual.Celebration?.Rank);
        Assert.Equal(LiturgicalColor.Red, actual.Color);
        Assert.Empty(actual.OptionalMemorials);
    }

    [Theory]
    [MemberData(nameof(Profiles))]
    public void Privileged_weekday_memorials_are_exposed_as_commemorations_not_full_Office_options(string profile)
    {
        var calendar = CreateCalendar(profile);
        (DateOnly Date, string Key, LiturgicalSeason Season, LiturgicalColor Color)[] dates =
        [
            (new(2028, 3, 7), "saints-perpetua-and-felicity-martyrs", LiturgicalSeason.Lent, LiturgicalColor.Violet),
            (new(2027, 12, 21), "saint-peter-canisius-priest-and-doctor-of-the-church", LiturgicalSeason.Advent, LiturgicalColor.Violet),
            (new(2027, 12, 29), "saint-thomas-becket-bishop-and-martyr", LiturgicalSeason.Christmas, LiturgicalColor.White),
        ];

        foreach (var (date, key, season, color) in dates)
        {
            var actual = calendar.ForDate(date);

            Assert.Null(actual.Celebration);
            Assert.Equal(season, actual.Season);
            Assert.Equal(color, actual.Color);
            Assert.Empty(actual.OptionalMemorials);
            Assert.Equal(key, Assert.Single(actual.MemorialCommemorations).CalendarKey);
            Assert.Equal(LiturgicalOccurrenceRule.MemorialCommemorations, actual.Occurrence?.Rule);
            Assert.Empty(actual.Occurrence!.SuppressedCelebrations);
        }
    }

    [Theory]
    [MemberData(nameof(Profiles))]
    public void Obligatory_memorials_never_offer_competing_memorials_under_current_forward_rules(string profile)
    {
        var calendar = CreateCalendar(profile);
        for (var date = new DateOnly(2027, 1, 1); date < new DateOnly(2041, 1, 1); date = date.AddDays(1))
        {
            var actual = calendar.ForDate(date);
            if (actual.Celebration?.Rank == CelebrationRank.Memorial)
            {
                Assert.True(actual.OptionalMemorials.Count == 0, $"{profile} {date:yyyy-MM-dd}: obligatory memorial has competing options.");
            }
        }
    }

    private static ILiturgicalCalendar CreateCalendar(string profile) => LiturgicalCalendars.Create(PreferredLiturgicalCalendar.Deserialize(profile));
}
