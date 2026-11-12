using Respondeo.LiturgicalCalendar;
using Respondeo.LiturgyOfTheHours;

namespace Respondeo.UnitTests.LiturgyOfTheHours;

public class CalendarWeekOwnershipTests
{
    [Theory]
    [InlineData("general")]
    [InlineData("us")]
    [InlineData("us-thursday")]
    public void Office_metadata_and_text_keys_follow_the_selected_days_background_week(string profile)
    {
        var provider = new NoTextProvider();
        var hours = new OrdinaryFormOffice(LiturgicalCalendars.Create(PreferredLiturgicalCalendar.Deserialize(profile)), provider);
        var cases = new (DateOnly Date, Hour Hour, int Week, string Prefix)[]
        {
            (new(2026, 11, 28), Hour.MorningPrayer, 2, "ordinarytime/w2/"),
            (new(2026, 11, 28), Hour.EveningPrayer, 1, "advent/w1/"),
            (new(2026, 11, 28), Hour.NightPrayer, 1, "advent/w1/"),
            (new(2027, 2, 13), Hour.MorningPrayer, 4, "lent/w4/"),
            (new(2027, 2, 13), Hour.EveningPrayer, 1, "lent/w1/"),
            (new(2027, 2, 13), Hour.NightPrayer, 1, "lent/w1/"),
            (new(2027, 3, 28), Hour.MorningPrayer, 1, "easter/w1/"),
            (new(2027, 3, 28), Hour.EveningPrayer, 1, "easter/w1/"),
            (new(2027, 3, 28), Hour.NightPrayer, 1, "easter/w1/"),
            (new(2027, 4, 3), Hour.EveningPrayer, 2, "easter/w2/"),
            (new(2027, 4, 3), Hour.NightPrayer, 2, "easter/w2/"),
            (new(2027, 5, 17), Hour.MorningPrayer, 3, "ordinarytime/w3/"),
            (new(2027, 6, 28), Hour.EveningPrayer, 1, "ordinarytime/w1/"),
            (new(2027, 6, 28), Hour.NightPrayer, 1, "ordinarytime/w1/"),
            (new(2028, 4, 16), Hour.MorningPrayer, 1, "easter/w1/"),
            (new(2028, 6, 5), Hour.MorningPrayer, 1, "ordinarytime/w1/"),
        };

        foreach (var item in cases)
        {
            provider.RequestedKeys.Clear();
            var office = hours.Compose(item.Date, item.Hour);

            Assert.Equal(item.Week, office.PsalterWeek);
            Assert.Contains(provider.RequestedKeys, key => key.StartsWith(item.Prefix, StringComparison.Ordinal)
                && key.EndsWith("/hymn", StringComparison.Ordinal));
            Assert.DoesNotContain(provider.RequestedKeys, key => key.EndsWith("/hymn", StringComparison.Ordinal)
                && key.Contains("/w", StringComparison.Ordinal) && !key.StartsWith(item.Prefix, StringComparison.Ordinal));
        }
    }

    [Fact]
    public void Ordinary_Sunday_First_Vespers_use_the_incoming_psalms_and_reading()
    {
        var calendar = LiturgicalCalendars.Create(LiturgicalCalendarOptions.Default);
        var hours = new OrdinaryFormOffice(calendar, new NoTextProvider());
        var office = hours.Compose(new(2027, 1, 16), Hour.EveningPrayer);

        Assert.True(office.IsFirstVespers);
        Assert.Equal(2, office.PsalterWeek);
        Assert.Equal(Psalter.EveningPsalmody(2, DayOfWeek.Saturday),
            office.Steps.Where(step => step.Kind is OfficeStepKind.Psalm or OfficeStepKind.Canticle).Select(step => step.Reference!.Citation));
        Assert.Equal(Psalter.ShortReading(Hour.EveningPrayer, 2, DayOfWeek.Saturday),
            Assert.Single(office.Steps, step => step.Kind == OfficeStepKind.Reading).Reference!.Citation);
    }

    [Fact]
    public void Hours_consumes_an_explicit_calendar_week_override()
    {
        var calendar = new WeekOverrideCalendar();
        var provider = new NoTextProvider();
        var office = new OrdinaryFormOffice(calendar, provider).Compose(new(2027, 1, 12), Hour.MorningPrayer);

        Assert.Equal(4, office.PsalterWeek);
        Assert.Equal(new DateOnly(2027, 1, 12), Assert.Single(calendar.Requests));
        Assert.Equal(Psalter.MorningPsalmody(4, DayOfWeek.Tuesday),
            office.Steps.Where(step => step.Kind is OfficeStepKind.Psalm or OfficeStepKind.Canticle).Select(step => step.Reference!.Citation));
        Assert.Equal(Psalter.ShortReading(Hour.MorningPrayer, 4, DayOfWeek.Tuesday),
            Assert.Single(office.Steps, step => step.Kind == OfficeStepKind.Reading).Reference!.Citation);
        Assert.Contains("ordinarytime/w4/tuesday/morning/hymn", provider.RequestedKeys);
    }

    private sealed class WeekOverrideCalendar : ILiturgicalCalendar
    {
        private readonly ILiturgicalCalendar calendar = LiturgicalCalendars.Create(LiturgicalCalendarOptions.Default);

        public List<DateOnly> Requests { get; } = [];

        public LiturgicalDay ForDate(DateOnly date) => calendar.ForDate(date);

        public LiturgicalWeek ForWeek(DateOnly date)
        {
            Requests.Add(date);
            return new(1, 4);
        }
    }

    private sealed class NoTextProvider : IHoursTextProvider
    {
        public bool IsSample => false;

        public string Acknowledgment => string.Empty;

        public List<string> RequestedKeys { get; } = [];

        public LicensedText? Find(IEnumerable<string> keys)
        {
            RequestedKeys.AddRange(keys);
            return null;
        }
    }
}
