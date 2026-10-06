using Respondeo.LiturgicalCalendar;
using Respondeo.Services;

namespace Respondeo.UnitTests.Services;

/// <summary>
/// Unit tests for <see cref="SeasonalPortrait"/>: the pure day-to-asset mapping,
/// plus an end-to-end check over the real calendar with a deterministic injected date confirming Ash Wednesday resolves to its dedicated portrait.
/// </summary>
public class SeasonalPortraitTests
{
    private static LiturgicalDay Day(LiturgicalSeason season, LiturgicalCelebration? celebration = null) =>
        new(new DateOnly(2025, 1, 1), season, LiturgicalColor.White, celebration);

    private static LiturgicalCelebration Celebration(string name) =>
        new(name, CelebrationRank.Feast, LiturgicalColor.Violet);

    [Fact]
    public void Resolve_returns_the_ashes_portrait_on_Ash_Wednesday()
    {
        var day = Day(LiturgicalSeason.Lent, Celebration(SeasonalPortrait.AshWednesdayName));

        Assert.Equal(SeasonalPortrait.AshWednesdayImagePath, SeasonalPortrait.Resolve(day));
    }

    [Fact]
    public void Resolve_returns_the_festive_portrait_in_Christmas_Time()
    {
        var day = Day(LiturgicalSeason.Christmas);

        Assert.Equal(SeasonalPortrait.ChristmasImagePath, SeasonalPortrait.Resolve(day));
    }

    [Fact]
    public void Resolve_returns_the_default_portrait_on_an_ordinary_day()
    {
        var day = Day(LiturgicalSeason.OrdinaryTime);

        Assert.Equal(SeasonalPortrait.DefaultImagePath, SeasonalPortrait.Resolve(day));
    }

    [Fact]
    public void Ash_Wednesday_takes_precedence_over_its_season()
    {
        // Ash Wednesday falls in Lent, but the penitential day should win over any season mapping.
        var day = Day(LiturgicalSeason.Lent, Celebration(SeasonalPortrait.AshWednesdayName));

        Assert.Equal(SeasonalPortrait.AshWednesdayImagePath, SeasonalPortrait.Resolve(day));
    }

    [Fact]
    public void CurrentImagePath_resolves_Ash_Wednesday_from_the_real_calendar()
    {
        // 5 March 2025 is Ash Wednesday (Easter 2025 is 20 April, less 46 days).
        var sut = new SeasonalPortrait(new RomanCalendar(), () => new DateOnly(2025, 3, 5));

        Assert.Equal(SeasonalPortrait.AshWednesdayImagePath, sut.CurrentImagePath);
    }

    [Fact]
    public void CurrentImagePath_resolves_Christmas_Day_from_the_real_calendar()
    {
        var sut = new SeasonalPortrait(new RomanCalendar(), () => new DateOnly(2025, 12, 25));

        Assert.Equal(SeasonalPortrait.ChristmasImagePath, sut.CurrentImagePath);
    }

    [Fact]
    public void CurrentImagePath_resolves_the_default_portrait_on_an_ordinary_day()
    {
        var sut = new SeasonalPortrait(new RomanCalendar(), () => new DateOnly(2025, 7, 1));

        Assert.Equal(SeasonalPortrait.DefaultImagePath, sut.CurrentImagePath);
    }
}
