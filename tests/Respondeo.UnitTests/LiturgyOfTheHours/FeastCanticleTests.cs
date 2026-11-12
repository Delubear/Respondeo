using Respondeo.LiturgicalCalendar;
using Respondeo.LiturgyOfTheHours;

namespace Respondeo.UnitTests.LiturgyOfTheHours;

public class FeastCanticleTests
{
    [Theory]
    [InlineData("general", 2027, 1, 6, "The Epiphany of the Lord")]
    [InlineData("us", 2027, 1, 3, "The Epiphany of the Lord")]
    [InlineData("us-thursday", 2027, 1, 3, "The Epiphany of the Lord")]
    [InlineData("general", 2028, 1, 6, "The Epiphany of the Lord")]
    [InlineData("us", 2028, 1, 2, "The Epiphany of the Lord")]
    [InlineData("us-thursday", 2028, 1, 2, "The Epiphany of the Lord")]
    [InlineData("general", 2028, 8, 6, "The Transfiguration of the Lord")]
    [InlineData("us", 2028, 8, 6, "The Transfiguration of the Lord")]
    [InlineData("us-thursday", 2028, 8, 6, "The Transfiguration of the Lord")]
    public void Timothy_canticle_follows_the_selected_feast_at_both_Vespers(
        string profile, int year, int month, int day, string celebration)
    {
        var calendar = LiturgicalCalendars.Create(PreferredLiturgicalCalendar.Deserialize(profile));
        var provider = new TextProvider();
        var hours = new OrdinaryFormOffice(calendar, provider);
        var date = new DateOnly(year, month, day);
        foreach (var first in new[] { true, false })
        {
            var hourKey = first ? "evening-i" : "evening";
            provider.Key = $"proper/{OrdinaryFormOffice.CelebrationId(celebration)}/{hourKey}/psalm-antiphon-3";
            var office = hours.Compose(first ? date.AddDays(-1) : date, Hour.EveningPrayer);
            var canticle = Assert.Single(office.Steps, step => step.Kind == OfficeStepKind.Canticle);

            Assert.Equal(celebration, office.Day.Celebration!.Name);
            Assert.Equal(date, office.Day.Date);
            Assert.Equal(first, office.IsFirstVespers);
            Assert.Equal("1 Timothy 3:16", canticle.Reference!.Citation);
            Assert.Equal("Synthetic feast antiphon", canticle.Antiphon);
            Assert.True(canticle.IsLicensedText);
            Assert.False(canticle.UseOfficialText);
        }
    }

    [Theory]
    [InlineData("general")]
    [InlineData("us")]
    [InlineData("us-thursday")]
    public void Weekday_Transfiguration_has_no_First_Vespers_and_keeps_its_existing_proper_set(string profile)
    {
        var hours = new OrdinaryFormOffice(LiturgicalCalendars.Create(PreferredLiturgicalCalendar.Deserialize(profile)), new TextProvider());
        var before = hours.Compose(new(2027, 8, 5), Hour.EveningPrayer);
        var feast = hours.Compose(new(2027, 8, 6), Hour.EveningPrayer);

        Assert.False(before.IsFirstVespers);
        Assert.Equal(new DateOnly(2027, 8, 5), before.Day.Date);
        Assert.Equal("Revelation 11:17-18; 12:10b-12a", Assert.Single(before.Steps, step => step.Kind == OfficeStepKind.Canticle).Reference!.Citation);
        Assert.False(feast.IsFirstVespers);
        Assert.Equal(new[] { "Psalm 110:1-5, 7", "Psalm 121", "1 Timothy 3:16" },
            feast.Steps.Where(step => step.Kind is OfficeStepKind.Psalm or OfficeStepKind.Canticle).Select(step => step.Reference!.Citation));
    }

    private sealed class TextProvider : IHoursTextProvider
    {
        public bool IsSample => false;

        public string Acknowledgment => string.Empty;

        public string? Key { get; set; }

        public LicensedText? Find(IEnumerable<string> keys) => Key is not null && keys.Contains(Key)
            ? new("", "Synthetic feast antiphon", "1 Timothy 3:16")
            : null;
    }
}
