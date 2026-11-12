using Microsoft.Extensions.DependencyInjection;
using Respondeo.LiturgicalCalendar;
using Respondeo.LiturgyOfTheHours;

namespace Respondeo.UnitTests.LiturgyOfTheHours;

public class FirstVespersPrecedenceTests
{
    private static ILiturgicalCalendar Calendar() =>
        new ServiceCollection().AddRespondeoLiturgy().AddRespondeoHours().BuildServiceProvider()
            .CreateScope().ServiceProvider.GetRequiredService<ILiturgicalCalendar>();

    private static bool FirstVespers(DateOnly date)
    {
        var calendar = Calendar();
        return calendar.ForEvening(date).IsFirstVespers;
    }

    private static DateOnly Find(Func<LiturgicalDay, LiturgicalDay, bool> predicate)
    {
        var calendar = Calendar();
        for (var date = new DateOnly(2020, 1, 1); date < new DateOnly(2040, 1, 1); date = date.AddDays(1))
        {
            if (predicate(calendar.ForDate(date), calendar.ForDate(date.AddDays(1))))
            {
                return date;
            }
        }

        throw new InvalidOperationException("No matching date in range.");
    }

    private static bool Sunday(LiturgicalDay day) => day.Date.DayOfWeek == DayOfWeek.Sunday;

    private static bool Solemnity(LiturgicalDay day) => day.Celebration?.Rank == CelebrationRank.Solemnity;

    [Fact]
    public void SaturdayMemorial_YieldsToSundayEveningPrayerI()
    {
        var date = Find((t, n) => Sunday(n) && t.Celebration?.Rank == CelebrationRank.Memorial);
        Assert.True(FirstVespers(date));
    }

    [Fact]
    public void WeekdayBeforeSolemnity_YieldsToEveningPrayerI()
    {
        var date = Find((t, n) => !Sunday(n) && Solemnity(n) && !Solemnity(t) && t.Season != LiturgicalSeason.Triduum);
        Assert.True(FirstVespers(date));
    }

    [Fact]
    public void SaturdaySolemnity_BeforeOrdinarySunday_KeepsItsVespers()
    {
        var date = Find((t, n) => Solemnity(t) && Sunday(n) && n.Season == LiturgicalSeason.OrdinaryTime && !Solemnity(n));
        Assert.False(FirstVespers(date));
    }

    [Fact]
    public void SaturdaySolemnity_BeforeAdventSunday_YieldsToSunday()
    {
        var date = Find((t, n) => Solemnity(t) && Sunday(n) && n.Season == LiturgicalSeason.Advent);
        Assert.True(FirstVespers(date));
    }

    [Fact]
    public void SaturdaySolemnity_BeforeLentSunday_YieldsToSunday()
    {
        var date = Find((t, n) => Solemnity(t) && Sunday(n) && n.Season == LiturgicalSeason.Lent);
        Assert.True(FirstVespers(date));
    }

    [Fact]
    public void SaturdaySolemnity_BeforeEasterSunday_YieldsToSunday()
    {
        // No real Saturday solemnity precedes an Easter Sunday in range, so promote an Easter Saturday synthetically.
        var calendar = Calendar();
        var date = Find((t, n) => Sunday(n) && n.Season == LiturgicalSeason.Easter && t.Season == LiturgicalSeason.Easter
            && t.Celebration is not null && !Solemnity(n));
        var today = calendar.ForDate(date);
        today = today with { Celebration = today.Celebration! with { Rank = CelebrationRank.Solemnity } };
        Assert.True(LiturgicalEveningResolver.Resolve(today, calendar.ForDate(date.AddDays(1)), calendar.Options).IsFirstVespers);
    }

    [Fact]
    public void FeastOfTheLordOnSunday_HasEveningPrayerI()
    {
        // The Transfiguration (August 6) fell on a Sunday in 2023.
        Assert.True(FirstVespers(new DateOnly(2023, 8, 5)));
    }

    [Fact]
    public void ConsecutiveSolemnities_OnWeekdays_FirstKeepsItsVespers()
    {
        var date = Find((t, n) => Solemnity(t) && Solemnity(n) && !Sunday(n) && t.Season != LiturgicalSeason.Triduum);
        Assert.False(FirstVespers(date));
    }

    [Fact]
    public void SacredHeart2022_JohnTheBaptistMovedToThursday_KeepsItsVespers()
    {
        // 2022: Sacred Heart (Fri Jun 24) displaced the Nativity of John the Baptist to Thu Jun 23.
        // GNLYC 61: equal rank, so today's Evening Prayer II prevails.
        var calendar = Calendar();
        var today = calendar.ForDate(new DateOnly(2022, 6, 23));
        Assert.Equal("The Nativity of Saint John the Baptist", today.Celebration?.Name);
        var tomorrow = calendar.ForDate(new DateOnly(2022, 6, 24));
        Assert.Equal("The Most Sacred Heart of Jesus", tomorrow.Celebration?.Name);
        Assert.False(calendar.ForEvening(today.Date).IsFirstVespers);
    }
}
