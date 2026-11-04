using Respondeo.LiturgicalCalendar;
using Respondeo.LiturgyOfTheHours;

namespace Respondeo.UnitTests.LiturgyOfTheHours;

public class CalendarAccuracyTests
{
    private static readonly RomanCalendar Universal = new();
    private static readonly RomanCalendar UnitedStates = new(LiturgicalCalendarOptions.Default with { Region = CalendarRegion.UnitedStates });

    [Theory]
    [InlineData(1900, 4, 15)]
    [InlineData(1954, 4, 18)]
    [InlineData(1981, 4, 19)]
    [InlineData(2000, 4, 23)]
    [InlineData(2008, 3, 23)]
    [InlineData(2011, 4, 24)]
    [InlineData(2016, 3, 27)]
    [InlineData(2019, 4, 21)]
    [InlineData(2024, 3, 31)]
    [InlineData(2025, 4, 20)]
    [InlineData(2026, 4, 5)]
    [InlineData(2027, 3, 28)]
    [InlineData(2028, 4, 16)]
    [InlineData(2029, 4, 1)]
    [InlineData(2030, 4, 21)]
    [InlineData(2038, 4, 25)]
    [InlineData(2285, 3, 22)]
    public void Easter_MatchesPublishedTables(int year, int month, int day) =>
        Assert.Equal(new DateOnly(year, month, day), LiturgicalDates.Easter(year));

    [Theory]
    [InlineData(2022, 11, 27)]
    [InlineData(2023, 12, 3)]
    [InlineData(2024, 12, 1)]
    [InlineData(2025, 11, 30)]
    [InlineData(2026, 11, 29)]
    [InlineData(2027, 11, 28)]
    [InlineData(2028, 12, 3)]
    public void FirstSundayOfAdvent_MatchesPublishedTables(int year, int month, int day) =>
        Assert.Equal(new DateOnly(year, month, day), LiturgicalDates.FirstSundayOfAdvent(year));

    [Theory]
    [InlineData(2023, 8, 9)]
    [InlineData(2024, 7, 8)]
    [InlineData(2025, 5, 12)]
    [InlineData(2026, 4, 11)]
    [InlineData(2027, 3, 10)]
    public void UnitedStates_EpiphanyAndBaptism(int year, int epiphanyDay, int baptismDay)
    {
        Assert.Equal(new DateOnly(year, 1, epiphanyDay), LiturgicalDates.Epiphany(year, true));
        Assert.Equal(new DateOnly(year, 1, baptismDay), LiturgicalDates.BaptismOfTheLord(year, true));
    }

    [Theory]
    [InlineData(2008, 3, 15, "Saint Joseph")]
    [InlineData(2023, 3, 20, "Saint Joseph")]
    [InlineData(2026, 3, 19, "Saint Joseph")]
    [InlineData(2008, 3, 31, "The Annunciation")]
    [InlineData(2016, 4, 4, "The Annunciation")]
    [InlineData(2024, 4, 8, "The Annunciation")]
    [InlineData(2029, 4, 9, "The Annunciation")]
    [InlineData(2025, 3, 25, "The Annunciation")]
    [InlineData(2019, 12, 9, "The Immaculate Conception")]
    [InlineData(2024, 12, 9, "The Immaculate Conception")]
    [InlineData(2025, 12, 8, "The Immaculate Conception")]
    public void Solemnities_LandOnCorrectDate(int year, int month, int day, string namePrefix)
    {
        var date = new DateOnly(year, month, day);
        Assert.StartsWith(namePrefix, Universal.ForDate(date).Celebration?.Name);

        foreach (var other in Enumerable.Range(-60, 121).Select(o => date.AddDays(o)).Where(d => d != date))
        {
            Assert.False(Universal.ForDate(other).Celebration?.Name?.StartsWith(namePrefix) == true, $"{namePrefix} also on {other}");
        }
    }

    [Theory]
    [InlineData(2016, 12, 30)]
    [InlineData(2022, 12, 30)]
    [InlineData(2024, 12, 29)]
    [InlineData(2025, 12, 28)]
    public void HolyFamily_FallsOnExpectedDate(int year, int month, int day) =>
        Assert.StartsWith("The Holy Family", Universal.ForDate(new DateOnly(year, month, day)).Celebration?.Name);

    [Fact]
    public void MoveableFeasts_ConsistentOffsets_AcrossCenturies()
    {
        for (var year = 1900; year <= 2200; year++)
        {
            var easter = LiturgicalDates.Easter(year);
            Assert.Equal(DayOfWeek.Sunday, easter.DayOfWeek);
            Assert.InRange(easter, new DateOnly(year, 3, 22), new DateOnly(year, 4, 25));

            Assert.Equal("Ash Wednesday", Universal.ForDate(easter.AddDays(-46)).Celebration?.Name);
            Assert.Equal("Pentecost Sunday", Universal.ForDate(easter.AddDays(49)).Celebration?.Name);
            Assert.Equal("The Most Holy Trinity", Universal.ForDate(easter.AddDays(56)).Celebration?.Name);
            Assert.Equal("The Most Sacred Heart of Jesus", Universal.ForDate(easter.AddDays(68)).Celebration?.Name);
            Assert.Equal("The Ascension of the Lord", Universal.ForDate(easter.AddDays(39)).Celebration?.Name);
            Assert.Equal("The Ascension of the Lord", UnitedStates.ForDate(easter.AddDays(42)).Celebration?.Name);
            Assert.Equal("The Most Holy Body and Blood of Christ", Universal.ForDate(easter.AddDays(60)).Celebration?.Name);
            Assert.Equal("The Most Holy Body and Blood of Christ", UnitedStates.ForDate(easter.AddDays(63)).Celebration?.Name);

            Assert.Equal(LiturgicalColor.Rose, Universal.ForDate(easter.AddDays(-21)).Color);
            var advent = LiturgicalDates.FirstSundayOfAdvent(year);
            Assert.Equal(DayOfWeek.Sunday, advent.DayOfWeek);
            Assert.InRange(advent, new DateOnly(year, 11, 27), new DateOnly(year, 12, 3));
            Assert.Equal(LiturgicalColor.Rose, Universal.ForDate(advent.AddDays(14)).Color);
            Assert.StartsWith("Our Lord Jesus Christ, King", Universal.ForDate(advent.AddDays(-7)).Celebration?.Name);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EveryDay_SeasonsAndWeeksAreContinuous_1900To2200(bool unitedStates)
    {
        var calendar = unitedStates ? UnitedStates : Universal;
        var previousSeason = calendar.ForDate(new DateOnly(1899, 12, 31)).Season;
        int? previousPsalter = null;

        for (var date = new DateOnly(1900, 1, 1); date <= new DateOnly(2200, 12, 31); date = date.AddDays(1))
        {
            var day = calendar.ForDate(date);
            var season = day.Season;
            var easter = LiturgicalDates.Easter(date.Year);

            if (season != previousSeason)
            {
                var expected = (previousSeason, season) switch
                {
                    (LiturgicalSeason.Christmas, LiturgicalSeason.OrdinaryTime) => LiturgicalDates.BaptismOfTheLord(date.Year, unitedStates).AddDays(1),
                    (LiturgicalSeason.OrdinaryTime, LiturgicalSeason.Lent) => LiturgicalDates.AshWednesday(date.Year),
                    (LiturgicalSeason.Lent, LiturgicalSeason.Triduum) => easter.AddDays(-3),
                    (LiturgicalSeason.Triduum, LiturgicalSeason.Easter) => easter.AddDays(1),
                    (LiturgicalSeason.Easter, LiturgicalSeason.OrdinaryTime) => easter.AddDays(50),
                    (LiturgicalSeason.OrdinaryTime, LiturgicalSeason.Advent) => LiturgicalDates.FirstSundayOfAdvent(date.Year),
                    (LiturgicalSeason.Advent, LiturgicalSeason.Christmas) => new DateOnly(date.Year, 12, 25),
                    _ => throw new Xunit.Sdk.XunitException($"Unexpected transition {previousSeason} -> {season} on {date}"),
                };
                Assert.Equal(expected, date);
            }

            var psalter = PsalterWeek.For(date, season, unitedStates);
            Assert.InRange(psalter, 1, 4);

            if (date.DayOfWeek == DayOfWeek.Sunday && previousPsalter is { } prior)
            {
                var restart = date == LiturgicalDates.FirstSundayOfAdvent(date.Year)
                    || date == LiturgicalDates.FirstSundayOfLent(date.Year)
                    || date == easter
                    || season == LiturgicalSeason.Christmas
                    || (season == LiturgicalSeason.OrdinaryTime && date.Month == 1)
                    || date == easter.AddDays(56);
                if (!restart)
                {
                    Assert.True((prior % 4) + 1 == psalter, $"Psalter week broke on {date}: {prior} -> {psalter}");
                }
            }

            if (date.DayOfWeek == DayOfWeek.Saturday)
            {
                previousPsalter = psalter;
            }

            if (season == LiturgicalSeason.OrdinaryTime)
            {
                var week = PsalterWeek.OrdinaryTimeWeek(date, easter, unitedStates);
                Assert.InRange(week, 1, 34);
                Assert.Equal(((week - 1) % 4) + 1, psalter);
            }

            previousSeason = season;
        }
    }

    [Fact]
    public void OrdinaryTime_HasExactly34Or33Weeks_AndEndsOnWeek34()
    {
        for (var year = 1901; year <= 2200; year++)
        {
            var easter = LiturgicalDates.Easter(year);
            var advent = LiturgicalDates.FirstSundayOfAdvent(year);
            Assert.Equal(34, PsalterWeek.OrdinaryTimeWeek(advent.AddDays(-1), easter));
            Assert.Equal(34, PsalterWeek.OrdinaryTimeWeek(advent.AddDays(-7), easter));

            var lastBeforeLent = PsalterWeek.OrdinaryTimeWeek(LiturgicalDates.AshWednesday(year).AddDays(-1), easter);
            var firstAfterPentecost = PsalterWeek.OrdinaryTimeWeek(easter.AddDays(50), easter);
            Assert.InRange(firstAfterPentecost - lastBeforeLent, 1, 2);
        }
    }
}
