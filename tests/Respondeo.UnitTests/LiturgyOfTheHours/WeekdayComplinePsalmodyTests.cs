using NSubstitute;
using Respondeo.LiturgicalCalendar;
using Respondeo.LiturgyOfTheHours;

namespace Respondeo.UnitTests.LiturgyOfTheHours;

public class WeekdayComplinePsalmodyTests
{
    [Theory]
    [InlineData(2026, 11, 11)]
    [InlineData(2027, 1, 4)]
    [InlineData(2027, 2, 15)]
    [InlineData(2027, 4, 6)]
    [InlineData(2027, 8, 6)]
    [InlineData(2027, 11, 4)]
    [InlineData(2027, 12, 20)]
    public void Sunday_psalm_substitution_preserves_every_other_step_and_Office_identity(int year, int month, int day)
    {
        var date = new DateOnly(year, month, day);
        foreach (var profile in new[] { "general", "us", "us-thursday" })
        {
            var hours = Hours(profile);
            var normal = hours.Compose(date, Hour.NightPrayer);
            Assert.Equal(ComplinePsalmodyChoice.Weekday, normal.SelectedComplinePsalmody);
            Assert.Null(normal.SelectedComplineForm);
            foreach (var choice in new[] { ComplinePsalmodyChoice.SundayI, ComplinePsalmodyChoice.SundayII })
            {
                var office = hours.Compose(date, Hour.NightPrayer, complinePsalmody: choice);

                Assert.Equal(choice, office.SelectedComplinePsalmody);
                Assert.Equal(choice == ComplinePsalmodyChoice.SundayI ? new[] { "Psalm 4", "Psalm 134" } : new[] { "Psalm 91" },
                    office.Steps.Where(step => step.Kind == OfficeStepKind.Psalm).Select(step => step.Reference?.Citation));
                Assert.Equivalent(normal.Steps.Where(step => step.Kind != OfficeStepKind.Psalm),
                    office.Steps.Where(step => step.Kind != OfficeStepKind.Psalm), strict: true);
                Assert.Equal(normal.Date, office.Date);
                Assert.Equal(normal.Day, office.Day);
                Assert.Equal(normal.Title, office.Title);
                Assert.Equal(normal.PsalterWeek, office.PsalterWeek);
                Assert.Equal(normal.IsFirstVespers, office.IsFirstVespers);
                Assert.Equal(normal.OfficialSources, office.OfficialSources);
                Assert.Null(office.SelectedComplineForm);
            }
            Assert.Equivalent(normal.Steps, hours.Compose(date, Hour.NightPrayer).Steps, strict: true);
        }
    }

    [Theory]
    [InlineData(2026, 11, 11, Hour.MorningPrayer)]
    [InlineData(2026, 11, 11, Hour.EveningPrayer)]
    [InlineData(2026, 12, 8, Hour.NightPrayer)]
    [InlineData(2026, 12, 24, Hour.NightPrayer)]
    [InlineData(2027, 3, 26, Hour.NightPrayer)]
    [InlineData(2027, 3, 27, Hour.NightPrayer)]
    [InlineData(2027, 3, 29, Hour.NightPrayer)]
    [InlineData(2027, 4, 3, Hour.NightPrayer)]
    [InlineData(2027, 4, 4, Hour.NightPrayer)]
    [InlineData(2027, 4, 10, Hour.NightPrayer)]
    [InlineData(2027, 4, 11, Hour.NightPrayer)]
    public void Substitution_is_not_offered_or_accepted_for_other_Hours_or_complete_Sunday_forms(int year, int month, int day, Hour hour)
    {
        var hours = Hours("general");
        var date = new DateOnly(year, month, day);
        Assert.Null(hours.Compose(date, hour).SelectedComplinePsalmody);
        foreach (var choice in new[] { ComplinePsalmodyChoice.SundayI, ComplinePsalmodyChoice.SundayII })
        {
            var error = Assert.Throws<ArgumentException>(() => hours.Compose(date, hour, complinePsalmody: choice));
            Assert.Equal("complinePsalmody", error.ParamName);
        }
    }

    [Fact]
    public void Invalid_or_conflicting_choices_are_rejected()
    {
        var hours = Hours("general");
        var invalid = Assert.Throws<ArgumentOutOfRangeException>(() =>
            hours.Compose(new(2026, 11, 11), Hour.NightPrayer, complinePsalmody: (ComplinePsalmodyChoice)999));
        Assert.Equal("complinePsalmody", invalid.ParamName);
        var conflict = Assert.Throws<ArgumentException>(() => hours.Compose(new(2027, 3, 29), Hour.NightPrayer,
            complineForm: ComplineSundayForm.SundayI, complinePsalmody: ComplinePsalmodyChoice.SundayII));
        Assert.Equal("complinePsalmody", conflict.ParamName);
    }

    [Theory]
    [InlineData(ComplinePsalmodyChoice.SundayI, "saturday", "Psalm 4")]
    [InlineData(ComplinePsalmodyChoice.SundayII, "sunday", "Psalm 91")]
    public void Substitution_binds_Sunday_psalms_but_keeps_weekday_non_psalmody_text_keys(
        ComplinePsalmodyChoice choice, string weekday, string citation)
    {
        var provider = Substitute.For<IHoursTextProvider>();
        var entries = new Dictionary<string, LicensedText>
        {
            [$"ordinarytime/w4/{weekday}/night/psalm-antiphon-1"] = new("Synthetic Sunday body", "Synthetic Sunday antiphon", citation),
            ["ordinarytime/w4/wednesday/night/psalm-antiphon-1"] = new("Wrong weekday body", "Wrong weekday antiphon", "Psalm 31:1-6"),
            [$"ordinarytime/w4/{weekday}/night/hymn"] = new("Wrong Sunday hymn"),
            ["ordinarytime/w4/wednesday/night/hymn"] = new("Synthetic weekday hymn"),
        };
        provider.Find(Arg.Any<IEnumerable<string>>()).Returns(call => call.Arg<IEnumerable<string>>()
            .Where(entries.ContainsKey).Select(key => entries[key]).FirstOrDefault());
        var hours = Hours("general", provider);

        var office = hours.Compose(new(2026, 11, 11), Hour.NightPrayer, complinePsalmody: choice);
        var psalm = office.Steps.First(step => step.Kind == OfficeStepKind.Psalm);

        Assert.Equal(citation, psalm.Reference?.Citation);
        Assert.Equal("Synthetic Sunday body", psalm.Text);
        Assert.Equal("Synthetic Sunday antiphon", psalm.Antiphon);
        Assert.True(psalm.IsLicensedText);
        Assert.False(psalm.UseOfficialText);
        Assert.Equal("Synthetic weekday hymn", Assert.Single(office.Steps, step => step.Kind == OfficeStepKind.Hymn).Text);
        Assert.Equal("Ephesians 4:26-27", Assert.Single(office.Steps, step => step.Kind == OfficeStepKind.Reading).Reference?.Citation);
        Assert.Equal(OfficeData.ResolveText(OfficeData.Texts.NightConcludingPrayers["wednesday"]),
            Assert.Single(office.Steps, step => step.Kind == OfficeStepKind.Prayer).Text);

        entries[$"ordinarytime/w4/{weekday}/night/psalm-antiphon-1"] = new("Wrong citation body", "Wrong antiphon", "Psalm 143:1-11");
        Assert.Throws<InvalidDataException>(() => hours.Compose(new(2026, 11, 11), Hour.NightPrayer, complinePsalmody: choice));
    }

    private static ILiturgyOfTheHours Hours(string profile, IHoursTextProvider? provider = null) => new HoursEngine(
        [new OrdinaryFormOffice(LiturgicalCalendars.Create(PreferredLiturgicalCalendar.Deserialize(profile)), provider ?? Substitute.For<IHoursTextProvider>())]);
}
