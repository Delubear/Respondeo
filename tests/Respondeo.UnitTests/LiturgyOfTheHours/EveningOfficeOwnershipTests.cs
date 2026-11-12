using Respondeo.LiturgicalCalendar;
using Respondeo.LiturgyOfTheHours;

namespace Respondeo.UnitTests.LiturgyOfTheHours;

public class EveningOfficeOwnershipTests
{
    [Theory]
    [InlineData("general", 2027, 4, 4, false, 4)]
    [InlineData("us", 2027, 4, 4, false, 4)]
    [InlineData("us-thursday", 2027, 4, 4, false, 4)]
    [InlineData("general", 2028, 1, 1, false, 1)]
    [InlineData("us", 2028, 1, 1, true, 2)]
    [InlineData("us-thursday", 2028, 1, 1, true, 2)]
    public void Vespers_and_Compline_follow_the_same_source_backed_evening_owner(
        string profile, int year, int month, int day, bool firstVespers, int officeDay)
    {
        var calendar = LiturgicalCalendars.Create(PreferredLiturgicalCalendar.Deserialize(profile));
        var hours = new OrdinaryFormOffice(calendar, new EmbeddedHoursTextProvider());
        var date = new DateOnly(year, month, day);
        var expectedDate = new DateOnly(year, month, officeDay);

        foreach (var hour in new[] { Hour.EveningPrayer, Hour.NightPrayer })
        {
            var office = hours.Compose(date, hour);

            Assert.Equal(firstVespers, office.IsFirstVespers);
            Assert.Equal(expectedDate, office.Day.Date);
            Assert.Equal(calendar.ForDate(expectedDate), office.Day);
        }

        var night = hours.Compose(date, Hour.NightPrayer);
        var psalms = night.Steps.Where(step => step.Kind == OfficeStepKind.Psalm).Select(step => step.Reference?.Citation);
        Assert.Equal(firstVespers ? new[] { "Psalm 4", "Psalm 134" } : new[] { "Psalm 91" }, psalms);
        Assert.Equal(firstVespers ? "Deuteronomy 6:4-7" : "Revelation 22:4-5",
            Assert.Single(night.Steps, step => step.Kind == OfficeStepKind.Reading).Reference?.Citation);

        var morning = hours.Compose(date, Hour.MorningPrayer);
        Assert.False(morning.IsFirstVespers);
        Assert.Equal(date, morning.Day.Date);
    }
}
