using Respondeo.LiturgicalCalendar;

namespace Respondeo.UnitTests.Content;

public class LiturgicalWeekTests
{
    [Theory]
    [InlineData("general")]
    [InlineData("us")]
    [InlineData("us-thursday")]
    public void Background_weeks_distinguish_numbered_seasons_from_special_days(string profile)
    {
        var calendar = LiturgicalCalendars.Create(PreferredLiturgicalCalendar.Deserialize(profile));
        var cases = new (DateOnly Date, int? Temporal, int Psalter)[]
        {
            (new(2026, 11, 28), 34, 2),
            (new(2026, 11, 29), 1, 1),
            (new(2026, 12, 25), null, 1),
            (new(2027, 1, 11), 1, 1),
            (new(2027, 1, 17), 2, 2),
            (new(2027, 2, 10), null, 4),
            (new(2027, 2, 13), null, 4),
            (new(2027, 2, 14), 1, 1),
            (new(2027, 3, 21), 6, 2),
            (new(2027, 3, 25), null, 2),
            (new(2027, 3, 27), null, 2),
            (new(2027, 3, 28), null, 1),
            (new(2027, 3, 29), 1, 1),
            (new(2027, 4, 4), 2, 2),
            (new(2027, 5, 16), 8, 4),
            (new(2027, 5, 17), 7, 3),
            (new(2027, 11, 27), 34, 2),
            (new(2027, 11, 28), 1, 1),
            (new(2028, 4, 16), null, 1),
            (new(2028, 6, 5), 9, 1),
            (new(2028, 6, 12), 10, 2),
        };

        foreach (var item in cases)
        {
            Assert.Equal(new LiturgicalWeek(item.Temporal, item.Psalter), calendar.ForWeek(item.Date));
        }
    }

    [Theory]
    [InlineData("general", 1)]
    [InlineData("us", null)]
    [InlineData("us-thursday", null)]
    public void Ordinary_Time_waits_until_after_the_regional_Baptism(string profile, int? week)
    {
        var calendar = LiturgicalCalendars.Create(PreferredLiturgicalCalendar.Deserialize(profile));

        Assert.Equal(week, calendar.ForWeek(new(2029, 1, 8)).TemporalWeek);
        Assert.Equal(1, calendar.ForWeek(new(2029, 1, 9)).TemporalWeek);
        Assert.Equal(1, calendar.ForWeek(new(2029, 1, 9)).PsalterWeek);
    }
}
