using Respondeo.Content.Contracts;
using Respondeo.Content.Liturgy;

namespace Respondeo.UnitTests.Content;

/// <summary>
/// Unit tests for the Ordinary Form liturgical calendar engine (<see cref="LiturgicalCalendar"/> and the
/// <see cref="Computus"/> Easter helper). Dates are checked against the published Roman calendar.
/// </summary>
public class LiturgicalCalendarTests
{
    private static readonly ILiturgicalCalendar Calendar = new LiturgicalCalendar();

    [Theory]
    [InlineData(2023, 4, 9)]
    [InlineData(2024, 3, 31)]
    [InlineData(2025, 4, 20)]
    [InlineData(2026, 4, 5)]
    [InlineData(2000, 4, 23)]
    public void Computus_returns_known_Easter_dates(int year, int month, int day) => Assert.Equal(new DateOnly(year, month, day), Computus.GregorianEaster(year));

    [Fact]
    public void Christmas_is_a_white_solemnity()
    {
        var day = Calendar.ForDate(new DateOnly(2025, 12, 25));

        Assert.Equal(LiturgicalSeason.Christmas, day.Season);
        Assert.Equal(LiturgicalColor.White, day.Color);
        Assert.Equal("The Nativity of the Lord", day.Celebration?.Name);
        Assert.Equal(CelebrationRank.Solemnity, day.Celebration?.Rank);
    }

    [Theory]
    [InlineData(2025, 12, 1)]
    [InlineData(2025, 12, 2)]
    public void Advent_days_are_violet(int year, int month, int day)
    {
        var result = Calendar.ForDate(new DateOnly(year, month, day));

        Assert.Equal(LiturgicalSeason.Advent, result.Season);
        Assert.Equal(LiturgicalColor.Violet, result.Color);
    }

    [Fact]
    public void First_Sunday_of_Advent_2025_is_the_thirtieth_of_November()
    {
        var day = Calendar.ForDate(new DateOnly(2025, 11, 30));

        Assert.Equal(LiturgicalSeason.Advent, day.Season);
    }

    [Fact]
    public void Day_before_Advent_is_Ordinary_Time()
    {
        var day = Calendar.ForDate(new DateOnly(2025, 11, 29));

        Assert.Equal(LiturgicalSeason.OrdinaryTime, day.Season);
        Assert.Equal(LiturgicalColor.Green, day.Color);
    }

    [Fact]
    public void Gaudete_Sunday_is_rose()
    {
        // Third Sunday of Advent 2025 = 14 December.
        var day = Calendar.ForDate(new DateOnly(2025, 12, 14));

        Assert.Equal(LiturgicalColor.Rose, day.Color);
    }

    [Fact]
    public void Ash_Wednesday_2025_opens_Lent()
    {
        var day = Calendar.ForDate(new DateOnly(2025, 3, 5));

        Assert.Equal(LiturgicalSeason.Lent, day.Season);
        Assert.Equal("Ash Wednesday", day.Celebration?.Name);
        Assert.Equal(LiturgicalColor.Violet, day.Color);
    }

    [Fact]
    public void Laetare_Sunday_2025_is_rose()
    {
        // Fourth Sunday of Lent 2025 = 30 March.
        var day = Calendar.ForDate(new DateOnly(2025, 3, 30));

        Assert.Equal(LiturgicalSeason.Lent, day.Season);
        Assert.Equal(LiturgicalColor.Rose, day.Color);
    }

    [Fact]
    public void Holy_Thursday_begins_the_Triduum()
    {
        var day = Calendar.ForDate(new DateOnly(2025, 4, 17));

        Assert.Equal(LiturgicalSeason.Triduum, day.Season);
    }

    [Fact]
    public void Easter_Sunday_2025_is_a_white_solemnity()
    {
        var day = Calendar.ForDate(new DateOnly(2025, 4, 20));

        Assert.Equal(LiturgicalSeason.Triduum, day.Season);
        Assert.Equal(LiturgicalColor.White, day.Color);
        Assert.Equal(CelebrationRank.Solemnity, day.Celebration?.Rank);
    }

    [Fact]
    public void Easter_Monday_is_Easter_Time()
    {
        var day = Calendar.ForDate(new DateOnly(2025, 4, 21));

        Assert.Equal(LiturgicalSeason.Easter, day.Season);
        Assert.Equal(LiturgicalColor.White, day.Color);
    }

    [Fact]
    public void Pentecost_2025_is_red_and_ends_Easter_Time()
    {
        // Easter 2025 = 20 Apr; Pentecost = +49 days = 8 June.
        var day = Calendar.ForDate(new DateOnly(2025, 6, 8));

        Assert.Equal(LiturgicalSeason.Easter, day.Season);
        Assert.Equal("Pentecost Sunday", day.Celebration?.Name);
        Assert.Equal(LiturgicalColor.Red, day.Color);
    }

    [Fact]
    public void Day_after_Pentecost_is_Ordinary_Time()
    {
        var day = Calendar.ForDate(new DateOnly(2025, 6, 9));

        Assert.Equal(LiturgicalSeason.OrdinaryTime, day.Season);
    }

    [Fact]
    public void New_Years_Day_is_still_Christmas_Time()
    {
        var day = Calendar.ForDate(new DateOnly(2025, 1, 1));

        Assert.Equal(LiturgicalSeason.Christmas, day.Season);
        Assert.Equal("Mary, the Holy Mother of God", day.Celebration?.Name);
    }

    [Fact]
    public void Baptism_of_the_Lord_2025_closes_Christmas_Time()
    {
        // Epiphany 6 Jan 2025 (Monday); Baptism = following Sunday = 12 Jan.
        var baptism = Calendar.ForDate(new DateOnly(2025, 1, 12));
        var dayAfter = Calendar.ForDate(new DateOnly(2025, 1, 13));

        Assert.Equal(LiturgicalSeason.Christmas, baptism.Season);
        Assert.Equal(LiturgicalSeason.OrdinaryTime, dayAfter.Season);
    }

    [Fact]
    public void Ordinary_weekday_has_no_celebration()
    {
        var day = Calendar.ForDate(new DateOnly(2025, 7, 15));

        Assert.Null(day.Celebration);
        Assert.Equal(LiturgicalSeason.OrdinaryTime, day.Season);
        Assert.Equal(LiturgicalColor.Green, day.Color);
    }

    [Fact]
    public void Assumption_is_a_fixed_white_solemnity()
    {
        var day = Calendar.ForDate(new DateOnly(2025, 8, 15));

        Assert.Equal("The Assumption of the Blessed Virgin Mary", day.Celebration?.Name);
        Assert.Equal(LiturgicalColor.White, day.Color);
    }
}
