using Respondeo.LiturgicalCalendar;

namespace Respondeo.UnitTests.Content;

public class LiturgicalEveningTests
{
    public static IEnumerable<object[]> BoundaryCases()
    {
        var cases = new (DateOnly Date, EveningPrayerPhase Phase)[]
        {
            (new(2026, 11, 28), EveningPrayerPhase.FirstVespers),
            (new(2027, 3, 24), EveningPrayerPhase.Ordinary),
            (new(2027, 3, 25), EveningPrayerPhase.TriduumProper),
            (new(2027, 3, 26), EveningPrayerPhase.TriduumProper),
            (new(2027, 3, 27), EveningPrayerPhase.TriduumProper),
            (new(2027, 3, 28), EveningPrayerPhase.TriduumProper),
            (new(2027, 4, 3), EveningPrayerPhase.FirstVespers),
            (new(2027, 4, 4), EveningPrayerPhase.SecondVespers),
            (new(2027, 4, 5), EveningPrayerPhase.SecondVespers),
            (new(2027, 5, 15), EveningPrayerPhase.FirstVespers),
            (new(2027, 5, 16), EveningPrayerPhase.SecondVespers),
            (new(2027, 12, 24), EveningPrayerPhase.FirstVespers),
            (new(2027, 12, 25), EveningPrayerPhase.SecondVespers),
            (new(2027, 12, 26), EveningPrayerPhase.SecondVespers),
        };
        foreach (var profile in new[] { "general", "us", "us-thursday" })
        {
            foreach (var (date, phase) in cases)
            {
                yield return [profile, date, phase];
            }
        }
    }

    [Theory]
    [MemberData(nameof(BoundaryCases))]
    public void Calendar_selects_the_Office_day_and_phase_at_reviewed_boundaries(string profile, DateOnly date, EveningPrayerPhase phase)
    {
        var calendar = LiturgicalCalendars.Create(PreferredLiturgicalCalendar.Deserialize(profile));
        var evening = calendar.ForEvening(date);
        var expectedDate = phase == EveningPrayerPhase.FirstVespers ? date.AddDays(1) : date;

        Assert.Equal(phase, evening.Phase);
        AssertDay(calendar.ForDate(date), evening.Today);
        AssertDay(calendar.ForDate(expectedDate), evening.OfficeDay);
        Assert.Equal(phase == EveningPrayerPhase.FirstVespers, evening.IsFirstVespers);
    }

    [Fact]
    public void Calendar_methods_handle_DateOnly_boundaries_explicitly()
    {
        var calendar = LiturgicalCalendars.Create(LiturgicalCalendarOptions.Default);

        Assert.Equal(DateOnly.MinValue, calendar.ForDate(DateOnly.MinValue).Date);
        Assert.Equal(DateOnly.MaxValue, calendar.ForDate(DateOnly.MaxValue).Date);
        Assert.Equal(DateOnly.MinValue, calendar.ForEvening(DateOnly.MinValue).Today.Date);

        var weekException = Assert.Throws<ArgumentOutOfRangeException>(() => calendar.ForWeek(DateOnly.MinValue));
        Assert.Equal("date", weekException.ParamName);

        var eveningException = Assert.Throws<ArgumentOutOfRangeException>(() => calendar.ForEvening(DateOnly.MaxValue));
        Assert.Equal("date", eveningException.ParamName);
    }

    public static IEnumerable<object[]> SeasonCases()
    {
        var cases = new (DateOnly Date, LiturgicalSeason CivilSeason, LiturgicalSeason EveningSeason)[]
        {
            (new(2026, 11, 28), LiturgicalSeason.OrdinaryTime, LiturgicalSeason.Advent),
            (new(2026, 12, 24), LiturgicalSeason.Advent, LiturgicalSeason.Christmas),
            (new(2026, 12, 25), LiturgicalSeason.Christmas, LiturgicalSeason.Christmas),
            (new(2027, 1, 10), LiturgicalSeason.Christmas, LiturgicalSeason.Christmas),
            (new(2027, 1, 11), LiturgicalSeason.OrdinaryTime, LiturgicalSeason.OrdinaryTime),
            (new(2027, 2, 9), LiturgicalSeason.OrdinaryTime, LiturgicalSeason.OrdinaryTime),
            (new(2027, 2, 10), LiturgicalSeason.Lent, LiturgicalSeason.Lent),
            (new(2027, 3, 24), LiturgicalSeason.Lent, LiturgicalSeason.Lent),
            (new(2027, 3, 25), LiturgicalSeason.Triduum, LiturgicalSeason.Triduum),
            (new(2027, 3, 27), LiturgicalSeason.Triduum, LiturgicalSeason.Triduum),
            (new(2027, 3, 28), LiturgicalSeason.Triduum, LiturgicalSeason.Easter),
            (new(2027, 3, 29), LiturgicalSeason.Easter, LiturgicalSeason.Easter),
            (new(2027, 5, 16), LiturgicalSeason.Easter, LiturgicalSeason.Easter),
            (new(2027, 5, 17), LiturgicalSeason.OrdinaryTime, LiturgicalSeason.OrdinaryTime),
        };
        foreach (var profile in new[] { "general", "us", "us-thursday" })
        {
            foreach (var (date, civilSeason, eveningSeason) in cases)
            {
                yield return [profile, date, civilSeason, eveningSeason];
            }
        }
    }

    [Theory]
    [MemberData(nameof(SeasonCases))]
    public void Evening_temporal_season_follows_Universal_Norms_19_22_28_33_40_44(
        string profile, DateOnly date, LiturgicalSeason civilSeason, LiturgicalSeason eveningSeason)
    {
        var calendar = LiturgicalCalendars.Create(PreferredLiturgicalCalendar.Deserialize(profile));
        var evening = calendar.ForEvening(date);

        Assert.Equal(civilSeason, calendar.ForDate(date).Season);
        Assert.Equal(civilSeason, evening.Today.Season);
        Assert.Equal(eveningSeason, evening.Season);
        Assert.Equal(date, evening.Today.Date);
        if (date == new DateOnly(2027, 3, 28))
        {
            Assert.Equal(EveningPrayerPhase.TriduumProper, evening.Phase);
            Assert.Equal(LiturgicalSeason.Triduum, evening.OfficeDay.Season);
        }
    }

    [Theory]
    [InlineData(2027, 3, 28, "general")]
    [InlineData(2027, 3, 28, "us")]
    [InlineData(2027, 3, 28, "us-thursday")]
    [InlineData(2028, 4, 16, "general")]
    [InlineData(2028, 4, 16, "us")]
    [InlineData(2028, 4, 16, "us-thursday")]
    public void Easter_octave_ordinal_follows_the_civil_date_not_First_Vespers(int year, int month, int day, string profile)
    {
        var calendar = LiturgicalCalendars.Create(PreferredLiturgicalCalendar.Deserialize(profile));
        var easter = new DateOnly(year, month, day);
        for (var offset = -1; offset <= 8; offset++)
        {
            var evening = calendar.ForEvening(easter.AddDays(offset));
            int? expected = offset is >= 0 and <= 7 ? offset + 1 : null;

            Assert.Equal(expected, evening.EasterOctaveDay);
            if (offset == 6)
            {
                Assert.True(evening.IsFirstVespers);
                Assert.Equal(easter.AddDays(7), evening.OfficeDay.Date);
                Assert.Equal(7, evening.EasterOctaveDay);
            }
        }

        Assert.Null(calendar.ForEvening(new(year, 12, 31)).EasterOctaveDay);
    }

    [Theory]
    [InlineData("general", 2027, 3, 28)]
    [InlineData("us", 2027, 3, 28)]
    [InlineData("us-thursday", 2027, 3, 28)]
    [InlineData("general", 2028, 4, 16)]
    [InlineData("us", 2028, 4, 16)]
    [InlineData("us-thursday", 2028, 4, 16)]
    public void Morning_and_evening_facts_distinguish_the_Holy_Week_boundaries(string profile, int year, int month, int day)
    {
        var calendar = LiturgicalCalendars.Create(PreferredLiturgicalCalendar.Deserialize(profile));
        var easter = new DateOnly(year, month, day);
        var cases = new (int Offset, LiturgicalSeason Morning, LiturgicalSeason Evening, bool HolyWeekWeekday)[]
        {
            (-7, LiturgicalSeason.Lent, LiturgicalSeason.Lent, false),
            (-6, LiturgicalSeason.Lent, LiturgicalSeason.Lent, true),
            (-5, LiturgicalSeason.Lent, LiturgicalSeason.Lent, true),
            (-4, LiturgicalSeason.Lent, LiturgicalSeason.Lent, true),
            (-3, LiturgicalSeason.Lent, LiturgicalSeason.Triduum, true),
            (-2, LiturgicalSeason.Triduum, LiturgicalSeason.Triduum, false),
            (-1, LiturgicalSeason.Triduum, LiturgicalSeason.Triduum, false),
            (0, LiturgicalSeason.Easter, LiturgicalSeason.Easter, false),
            (1, LiturgicalSeason.Easter, LiturgicalSeason.Easter, false),
        };
        foreach (var item in cases)
        {
            var date = easter.AddDays(item.Offset);
            var actual = calendar.ForDate(date);
            Assert.Equal(item.Morning, actual.MorningPrayerSeason);
            Assert.Equal(item.Evening, calendar.ForEvening(date).Season);
            Assert.Equal(item.HolyWeekWeekday, actual.IsHolyWeekWeekday);
        }

        var ordinary = calendar.ForDate(new(year, 11, 11));
        Assert.Equal(LiturgicalSeason.OrdinaryTime, ordinary.MorningPrayerSeason);
        Assert.False(ordinary.IsHolyWeekWeekday);
    }

    private static void AssertDay(LiturgicalDay expected, LiturgicalDay actual)
    {
        Assert.Equal(expected.Date, actual.Date);
        Assert.Equal(expected.Season, actual.Season);
        Assert.Equal(expected.Color, actual.Color);
        Assert.Equal(expected.Celebration, actual.Celebration);
        Assert.Equal(expected.FerialName, actual.FerialName);
        Assert.Equal(expected.OptionalMemorials, actual.OptionalMemorials);
        Assert.Equal(expected.MemorialCommemorations, actual.MemorialCommemorations);
    }

    [Theory]
    [InlineData("general", EveningPrayerPhase.FirstVespers)]
    [InlineData("us", EveningPrayerPhase.Ordinary)]
    [InlineData("us-thursday", EveningPrayerPhase.Ordinary)]
    public void Epiphany_First_Vespers_follow_the_selected_region(string profile, EveningPrayerPhase phase)
    {
        var calendar = LiturgicalCalendars.Create(PreferredLiturgicalCalendar.Deserialize(profile));
        var evening = calendar.ForEvening(new(2027, 1, 5));

        Assert.Equal(phase, evening.Phase);
        Assert.Equal(phase == EveningPrayerPhase.FirstVespers ? new(2027, 1, 6) : new DateOnly(2027, 1, 5), evening.OfficeDay.Date);
    }

    [Fact]
    public void Equal_solemnities_keep_the_current_day_without_English_name_matching()
    {
        var today = new LiturgicalDay(new(2027, 6, 23), LiturgicalSeason.OrdinaryTime, LiturgicalColor.White,
            new("Synthetic A", CelebrationRank.Solemnity, LiturgicalColor.White));
        var tomorrow = today with { Date = today.Date.AddDays(1), Celebration = today.Celebration! with { Name = "Synthetic B" } };
        var evening = LiturgicalEveningResolver.Resolve(today, tomorrow, LiturgicalCalendarOptions.Default);

        Assert.Equal(EveningPrayerPhase.SecondVespers, evening.Phase);
        Assert.Equal(today, evening.OfficeDay);
        Assert.False(evening.IsFirstVespers);
    }

    [Fact]
    public void Resolver_rejects_nonconsecutive_inputs()
    {
        var calendar = LiturgicalCalendars.Create(LiturgicalCalendarOptions.Default);
        var today = calendar.ForDate(new(2027, 1, 3));
        var exception = Assert.Throws<ArgumentException>(() => LiturgicalEveningResolver.Resolve(
            today, calendar.ForDate(today.Date.AddDays(2)), calendar.Options));

        Assert.Equal("tomorrow", exception.ParamName);
    }
}
