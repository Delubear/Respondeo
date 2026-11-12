using Respondeo.LiturgicalCalendar;
using Respondeo.LiturgyOfTheHours;

namespace Respondeo.UnitTests.LiturgyOfTheHours;

public class ComplinePsalmodyTests
{
    [Theory]
    [MemberData(nameof(ComplineResponsoryTests.EasterDates), MemberType = typeof(ComplineResponsoryTests))]
    public void Easter_octave_uses_a_complete_Sunday_schema(int year, int month, int day, string profile)
    {
        ILiturgicalCalendar calendar = LiturgicalCalendars.Create(PreferredLiturgicalCalendar.Deserialize(profile));
        var hours = new OrdinaryFormOffice(calendar, new NoTextProvider());
        var easter = new DateOnly(year, month, day);
        for (var offset = 0; offset < 8; offset++)
        {
            var date = easter.AddDays(offset);
            var office = hours.Compose(date, Hour.NightPrayer);
            var firstVespers = calendar.ForEvening(date).IsFirstVespers;
            var schema = firstVespers ? DayOfWeek.Saturday : DayOfWeek.Sunday;

            Assert.Equal(firstVespers, office.IsFirstVespers);
            Assert.Equal(firstVespers ? ComplineSundayForm.SundayI : ComplineSundayForm.SundayII, office.SelectedComplineForm);
            Assert.Equal(firstVespers ? new[] { "Psalm 4", "Psalm 134" } : new[] { "Psalm 91" }, Psalms(office));
            Assert.Equal(firstVespers ? "Deuteronomy 6:4-7" : "Revelation 22:4-5",
                Assert.Single(office.Steps, step => step.Kind == OfficeStepKind.Reading).Reference?.Citation);
            Assert.Equal(OfficeData.ResolveText(OfficeData.Texts.NightConcludingPrayers[Psalter.Key(schema)]),
                Assert.Single(office.Steps, step => step.Kind == OfficeStepKind.Prayer).Text);
        }
    }

    [Theory]
    [InlineData("general")]
    [InlineData("us")]
    [InlineData("us-thursday")]
    public void Weekday_after_octave_retains_its_ferial_schema(string profile)
    {
        var office = Hours(profile).Compose(new(2027, 4, 6), Hour.NightPrayer);

        Assert.False(office.IsFirstVespers);
        Assert.Equal(new[] { "Psalm 143:1-11" }, Psalms(office));
        Assert.Equal(Psalter.NightPsalmody(DayOfWeek.Tuesday).Reading,
            Assert.Single(office.Steps, step => step.Kind == OfficeStepKind.Reading).Reference?.Citation);
    }

    [Theory]
    [InlineData(2027, 8, 6, DayOfWeek.Friday)]
    [InlineData(2026, 11, 11, DayOfWeek.Wednesday)]
    public void Weekday_feasts_and_memorials_do_not_force_the_Sunday_schema(int year, int month, int day, DayOfWeek weekday)
    {
        var office = Hours("general").Compose(new(year, month, day), Hour.NightPrayer);

        Assert.False(office.IsFirstVespers);
        Assert.Equal(Psalter.NightPsalmody(weekday).Psalms, Psalms(office));
        Assert.Equal(Psalter.NightPsalmody(weekday).Reading,
            Assert.Single(office.Steps, step => step.Kind == OfficeStepKind.Reading).Reference?.Citation);
    }

    [Theory]
    [MemberData(nameof(ComplineResponsoryTests.EasterDates), MemberType = typeof(ComplineResponsoryTests))]
    public void Either_complete_form_is_selectable_without_changing_Calendar_ownership(int year, int month, int day, string profile)
    {
        ILiturgyOfTheHours hours = new HoursEngine([Hours(profile)]);
        var easter = new DateOnly(year, month, day);
        for (var offset = 0; offset < 8; offset++)
        {
            var date = easter.AddDays(offset);
            var normal = hours.Compose(date, Hour.NightPrayer);
            foreach (var form in new[] { ComplineSundayForm.SundayI, ComplineSundayForm.SundayII })
            {
                var office = hours.Compose(date, Hour.NightPrayer, complineForm: form);
                var first = form == ComplineSundayForm.SundayI;

                Assert.Equal(form, office.SelectedComplineForm);
                Assert.Equal(first ? new[] { "Psalm 4", "Psalm 134" } : new[] { "Psalm 91" }, Psalms(office));
                Assert.Equal(first ? "Deuteronomy 6:4-7" : "Revelation 22:4-5",
                    Assert.Single(office.Steps, step => step.Kind == OfficeStepKind.Reading).Reference?.Citation);
                Assert.Equal(OfficeData.ResolveText(OfficeData.Texts.NightConcludingPrayers[first ? "saturday" : "sunday"]),
                    Assert.Single(office.Steps, step => step.Kind == OfficeStepKind.Prayer).Text);
                Assert.Equal(normal.Date, office.Date);
                Assert.Equal(normal.Day, office.Day);
                Assert.Equal(normal.Title, office.Title);
                Assert.Equal(normal.PsalterWeek, office.PsalterWeek);
                Assert.Equal(normal.IsFirstVespers, office.IsFirstVespers);
                Assert.Equal(normal.OfficialSources, office.OfficialSources);
                Assert.Equal(normal.Steps.Where(step => step.Kind is OfficeStepKind.Responsory or OfficeStepKind.MarianAntiphon),
                    office.Steps.Where(step => step.Kind is OfficeStepKind.Responsory or OfficeStepKind.MarianAntiphon));
            }
            Assert.Equal(normal.SelectedComplineForm, hours.Compose(date, Hour.NightPrayer).SelectedComplineForm);
        }
    }

    [Theory]
    [InlineData(2027, 3, 27, Hour.NightPrayer)]
    [InlineData(2027, 4, 5, Hour.NightPrayer)]
    [InlineData(2027, 4, 11, Hour.NightPrayer)]
    [InlineData(2026, 11, 11, Hour.NightPrayer)]
    [InlineData(2027, 3, 29, Hour.MorningPrayer)]
    [InlineData(2027, 3, 29, Hour.EveningPrayer)]
    public void Form_choice_is_not_offered_or_accepted_outside_octave_Compline(int year, int month, int day, Hour hour)
    {
        ILiturgyOfTheHours hours = new HoursEngine([Hours("general")]);
        var date = new DateOnly(year, month, day);
        Assert.Null(hours.Compose(date, hour).SelectedComplineForm);
        foreach (var form in new[] { ComplineSundayForm.SundayI, ComplineSundayForm.SundayII })
        {
            var error = Assert.Throws<ArgumentException>(() => hours.Compose(date, hour, complineForm: form));
            Assert.Equal("complineForm", error.ParamName);
        }
    }

    [Fact]
    public void Unknown_Compline_form_is_rejected()
    {
        ILiturgyOfTheHours hours = new HoursEngine([Hours("general")]);
        var error = Assert.Throws<ArgumentOutOfRangeException>(() =>
            hours.Compose(new(2027, 3, 29), Hour.NightPrayer, complineForm: (ComplineSundayForm)999));

        Assert.Equal("complineForm", error.ParamName);
    }

    [Theory]
    [InlineData(29, ComplineSundayForm.SundayI, "saturday", "Psalm 4")]
    [InlineData(29, ComplineSundayForm.SundayII, "sunday", "Psalm 91")]
    [InlineData(3, ComplineSundayForm.SundayI, "saturday", "Psalm 4")]
    [InlineData(3, ComplineSundayForm.SundayII, "sunday", "Psalm 91")]
    public void Chosen_form_uses_only_its_citation_bound_psalmody_texts(int day, ComplineSundayForm form, string weekday, string citation)
    {
        var date = day == 29 ? new DateOnly(2027, 3, day) : new DateOnly(2027, 4, day);
        ILiturgicalCalendar calendar = LiturgicalCalendars.Create(PreferredLiturgicalCalendar.Deserialize("general"));
        var week = calendar.ForWeek(calendar.ForEvening(date).OfficeDay.Date).PsalterWeek;
        var key = $"easter/w{week}/{weekday}/night/psalm-antiphon-1";
        var hours = new OrdinaryFormOffice(calendar, new SundayTextProvider(key, citation));

        var office = hours.Compose(date, Hour.NightPrayer, complineForm: form);
        var psalm = office.Steps.First(step => step.Kind == OfficeStepKind.Psalm);

        Assert.Equal(citation, psalm.Reference?.Citation);
        Assert.Equal("Synthetic selected schema body", psalm.Text);
        Assert.Equal("Synthetic selected schema antiphon", psalm.Antiphon);
        Assert.True(psalm.IsLicensedText);
        Assert.False(psalm.UseOfficialText);
    }

    private sealed class SundayTextProvider(string selectedKey, string citation) : IHoursTextProvider
    {
        public bool IsSample => false;

        public string Acknowledgment => string.Empty;

        public LicensedText? Find(IEnumerable<string> keys) => keys.Contains(selectedKey)
            ? new("Synthetic selected schema body", "Synthetic selected schema antiphon", citation)
            : null;
    }

    private static IEnumerable<string?> Psalms(Office office) =>
        office.Steps.Where(step => step.Kind == OfficeStepKind.Psalm).Select(step => step.Reference?.Citation);

    private static OrdinaryFormOffice Hours(string profile) =>
        new(LiturgicalCalendars.Create(PreferredLiturgicalCalendar.Deserialize(profile)), new NoTextProvider());

    private sealed class NoTextProvider : IHoursTextProvider
    {
        public bool IsSample => false;

        public string Acknowledgment => string.Empty;

        public LicensedText? Find(IEnumerable<string> keys) => null;
    }
}
