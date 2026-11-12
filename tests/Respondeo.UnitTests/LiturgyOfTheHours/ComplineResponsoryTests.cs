using Respondeo.LiturgicalCalendar;
using Respondeo.LiturgyOfTheHours;

namespace Respondeo.UnitTests.LiturgyOfTheHours;

public class ComplineResponsoryTests
{
    public static TheoryData<int, int, int, string> EasterDates => new()
    {
        { 2027, 3, 28, "general" },
        { 2027, 3, 28, "us" },
        { 2027, 3, 28, "us-thursday" },
        { 2028, 4, 16, "general" },
        { 2028, 4, 16, "us" },
        { 2028, 4, 16, "us-thursday" },
    };

    [Theory]
    [MemberData(nameof(EasterDates))]
    public void Easter_Sunday_through_Saturday_use_the_special_response(int year, int month, int day, string profile)
    {
        var hours = Hours(profile);
        var easter = new DateOnly(year, month, day);
        for (var offset = 0; offset < 7; offset++)
        {
            var office = hours.Compose(easter.AddDays(offset), Hour.NightPrayer);
            var response = Assert.Single(office.Steps, step => step.Kind == OfficeStepKind.Responsory);

            Assert.Equal(OfficeData.Text("easter-night-responsory"), response.Text);
            Assert.False(response.UseOfficialText);
            if (offset == 6)
            {
                Assert.True(office.IsFirstVespers);
                Assert.Equal(easter.AddDays(7), office.Day.Date);
            }
        }
    }

    [Theory]
    [MemberData(nameof(EasterDates))]
    public void Second_Sunday_through_Pentecost_require_the_ordinary_Easter_response(int year, int month, int day, string profile)
    {
        var hours = Hours(profile);
        var easter = new DateOnly(year, month, day);
        foreach (var offset in new[] { 7, 8, 9, 13, 14, 39, 42, 48, 49 })
        {
            var office = hours.Compose(easter.AddDays(offset), Hour.NightPrayer);
            var response = Assert.Single(office.Steps, step => step.Kind == OfficeStepKind.Responsory);

            Assert.Null(response.Text);
            Assert.True(response.UseOfficialText);
            Assert.Contains("alleluias", response.Explanation);
            Assert.NotEmpty(office.OfficialSources);
        }
    }

    [Theory]
    [MemberData(nameof(EasterDates))]
    public void Monday_after_Pentecost_retains_the_ordinary_non_Easter_response(int year, int month, int day, string profile)
    {
        var office = Hours(profile).Compose(new DateOnly(year, month, day).AddDays(50), Hour.NightPrayer);
        var response = Assert.Single(office.Steps, step => step.Kind == OfficeStepKind.Responsory);

        Assert.Equal(OfficeData.Text("night-responsory"), response.Text);
        Assert.False(response.UseOfficialText);
    }

    private static OrdinaryFormOffice Hours(string profile) =>
        new(LiturgicalCalendars.Create(PreferredLiturgicalCalendar.Deserialize(profile)), new NoTextProvider());

    private sealed class NoTextProvider : IHoursTextProvider
    {
        public bool IsSample => false;

        public string Acknowledgment => string.Empty;

        public LicensedText? Find(IEnumerable<string> keys) => null;
    }
}
