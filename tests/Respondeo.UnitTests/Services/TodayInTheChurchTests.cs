using Respondeo.LiturgicalCalendar;
using Respondeo.Services.Calendar;

namespace Respondeo.UnitTests.Services;

/// <summary>
/// Unit tests for <see cref="TodayInTheChurch"/>: the "Today in the Church" dialog text derived from a liturgical day.
/// </summary>
public class TodayInTheChurchTests
{
    private static LiturgicalDay Day(
        LiturgicalSeason season,
        LiturgicalColor color,
        LiturgicalCelebration? celebration = null) =>
        new(new DateOnly(2025, 1, 1), season, color, celebration);

    [Theory]
    [InlineData(CelebrationRank.Solemnity, "The Solemnity of the Assumption")]
    [InlineData(CelebrationRank.Feast, "The Feast of the Assumption")]
    [InlineData(CelebrationRank.Memorial, "The Memorial of the Assumption")]
    public void DayLabel_names_a_celebration_by_rank_and_lowercases_a_leading_The(CelebrationRank rank, string expected)
    {
        var view = TodayInTheChurch.For(Day(LiturgicalSeason.OrdinaryTime, LiturgicalColor.White,
            new LiturgicalCelebration("The Assumption", rank, LiturgicalColor.White)));

        Assert.Equal(expected, view.DayLabel);
    }

    [Fact]
    public void DayLabel_uses_the_ferial_name_on_a_ferial_day()
    {
        var day = Day(LiturgicalSeason.Lent, LiturgicalColor.Violet) with { FerialName = "Monday of the Third Week of Lent" };

        Assert.Equal("Monday of the Third Week of Lent", TodayInTheChurch.For(day).DayLabel);
    }

    [Fact]
    public void ColorClue_is_null_when_the_day_wears_its_season_color()
    {
        Assert.Null(TodayInTheChurch.For(Day(LiturgicalSeason.OrdinaryTime, LiturgicalColor.Green)).ColorClue);
    }

    [Fact]
    public void ColorClue_explains_a_color_that_departs_from_the_season()
    {
        var clue = TodayInTheChurch.For(Day(LiturgicalSeason.OrdinaryTime, LiturgicalColor.Red)).ColorClue;

        Assert.StartsWith("Red today", clue);
    }

    [Fact]
    public void An_optional_memorial_fills_the_card_on_a_ferial_day()
    {
        var memorial = new LiturgicalCelebration("Saint Blaise", CelebrationRank.OptionalMemorial, LiturgicalColor.Red) { Id = "blaise" };
        var day = Day(LiturgicalSeason.OrdinaryTime, LiturgicalColor.Green) with { OptionalMemorials = [memorial] };

        var view = TodayInTheChurch.For(day);

        Assert.Same(memorial, view.Celebration);
        Assert.True(view.IsOptionalMemorial);
        Assert.Equal("The Church also remembers today", view.CelebrationIntro);
        Assert.Equal(["blaise"], view.LinkedSlugs());
    }

    [Fact]
    public void The_principal_celebration_takes_the_card_and_its_slug_is_linked_first()
    {
        var feast = new LiturgicalCelebration("Saint Benedict", CelebrationRank.Feast, LiturgicalColor.White) { Id = "benedict" };

        var view = TodayInTheChurch.For(Day(LiturgicalSeason.OrdinaryTime, LiturgicalColor.White, feast));

        Assert.Same(feast, view.Celebration);
        Assert.False(view.IsOptionalMemorial);
        Assert.Equal("Today the Church keeps the feast of", view.CelebrationIntro);
        Assert.Equal(["benedict"], view.LinkedSlugs());
    }
}
