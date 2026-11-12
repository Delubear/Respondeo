using Respondeo.LiturgicalCalendar;
using Respondeo.LiturgyOfTheHours;

namespace Respondeo.UnitTests.LiturgyOfTheHours;

public class MemorialCommemorationTests
{
    [Theory]
    [InlineData(2028, 3, 7, "saints-perpetua-and-felicity-martyrs", Hour.MorningPrayer)]
    [InlineData(2028, 3, 7, "saints-perpetua-and-felicity-martyrs", Hour.EveningPrayer)]
    [InlineData(2027, 12, 21, "saint-peter-canisius-priest-and-doctor-of-the-church", Hour.MorningPrayer)]
    [InlineData(2027, 12, 21, "saint-peter-canisius-priest-and-doctor-of-the-church", Hour.EveningPrayer)]
    [InlineData(2027, 12, 29, "saint-thomas-becket-bishop-and-martyr", Hour.MorningPrayer)]
    [InlineData(2027, 12, 29, "saint-thomas-becket-bishop-and-martyr", Hour.EveningPrayer)]
    public void Selected_commemoration_adds_only_its_antiphon_and_prayer_after_the_temporal_collect(
        int year, int month, int day, string calendarKey, Hour hour)
    {
        var date = new DateOnly(year, month, day);
        var calendar = LiturgicalCalendars.Create(LiturgicalCalendarOptions.Default);
        var memorial = Assert.Single(calendar.ForDate(date).MemorialCommemorations,
            value => value.CalendarKey == calendarKey);
        var contentId = OrdinaryFormOffice.CelebrationId(memorial);
        var provider = new RecordingProvider(new()
        {
            [$"proper/{contentId}/commemoration/antiphon"] = new("Synthetic memorial antiphon"),
            [$"proper/{contentId}/commemoration/prayer"] = new("Synthetic memorial prayer"),
        });
        ILiturgyOfTheHours hours = new HoursEngine([new OrdinaryFormOffice(calendar, provider)]);
        var temporalOffice = hours.Compose(date, hour);
        var office = hours.Compose(date, hour, memorialCommemorationCalendarKey: calendarKey);

        var collectIndex = Array.FindIndex(temporalOffice.Steps.ToArray(), step => step.Kind == OfficeStepKind.Prayer);
        Assert.True(collectIndex >= 0);
        Assert.Equivalent(temporalOffice.Steps.Take(collectIndex + 1), office.Steps.Take(collectIndex + 1), strict: true);
        Assert.Equivalent(temporalOffice.Steps.Skip(collectIndex + 1), office.Steps.Skip(collectIndex + 3), strict: true);
        Assert.Equal(calendarKey, office.SelectedMemorialCommemorationCalendarKey);

        var antiphon = office.Steps[collectIndex + 1];
        var prayer = office.Steps[collectIndex + 2];
        Assert.Equal(OfficeStepKind.Antiphon, antiphon.Kind);
        Assert.Equal("Synthetic memorial antiphon", antiphon.Text);
        Assert.True(antiphon.IsLicensedText);
        Assert.Equal(OfficeStepKind.Prayer, prayer.Kind);
        Assert.Equal("Synthetic memorial prayer", prayer.Text);
        Assert.True(prayer.IsLicensedText);
        Assert.Contains(provider.Requests, request => request.SequenceEqual(
            [$"proper/{contentId}/commemoration/antiphon"]));
        Assert.Contains(provider.Requests, request => request.SequenceEqual(
            [$"proper/{contentId}/commemoration/prayer"]));
    }

    [Fact]
    public void Commemoration_selection_rejects_unavailable_dates_and_night_prayer()
    {
        var calendar = LiturgicalCalendars.Create(LiturgicalCalendarOptions.Default);
        var date = new DateOnly(2028, 3, 7);
        var key = Assert.Single(calendar.ForDate(date).MemorialCommemorations).CalendarKey!;
        var hours = new OrdinaryFormOffice(calendar, new RecordingProvider(new()));

        Assert.Throws<ArgumentException>(() => hours.Compose(date, Hour.NightPrayer, memorialCommemorationCalendarKey: key));
        Assert.Throws<ArgumentException>(() => hours.Compose(date, Hour.MorningPrayer, memorialCommemorationCalendarKey: "unknown-memorial"));
        Assert.Throws<ArgumentException>(() => hours.Compose(new(2028, 3, 8), Hour.MorningPrayer,
            memorialCommemorationCalendarKey: key));
    }

    [Fact]
    public void Disabling_official_text_lookup_keeps_the_selected_commemoration_and_marks_both_texts_unavailable()
    {
        var date = new DateOnly(2028, 3, 7);
        var calendar = LiturgicalCalendars.Create(LiturgicalCalendarOptions.Default);
        var key = Assert.Single(calendar.ForDate(date).MemorialCommemorations).CalendarKey!;
        var provider = new RecordingProvider(new());
        var office = new OrdinaryFormOffice(calendar, provider).Compose(date, Hour.MorningPrayer,
            preferOfficialText: false, memorialCommemorationCalendarKey: key);

        Assert.Empty(provider.Requests);
        Assert.Equal(key, office.SelectedMemorialCommemorationCalendarKey);
        Assert.Contains(office.Steps, step => step.Kind == OfficeStepKind.Antiphon && step.UseOfficialText);
        Assert.Contains(office.Steps, step => step.Title == "Memorial Prayer" && step.UseOfficialText);
    }

    private sealed class RecordingProvider(Dictionary<string, LicensedText> entries) : IHoursTextProvider
    {
        public bool IsSample => false;

        public string Acknowledgment => "Synthetic test acknowledgment";

        public List<string[]> Requests { get; } = [];

        public LicensedText? Find(IEnumerable<string> keys)
        {
            var requested = keys.ToArray();
            Requests.Add(requested);
            return requested.Select(key => entries.GetValueOrDefault(key)).FirstOrDefault(text => text is not null);
        }

        public HoursCelebration? FindCelebration(string id) => null;
    }
}
