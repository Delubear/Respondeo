using Respondeo.LiturgicalCalendar;
using Respondeo.LiturgyOfTheHours;

namespace Respondeo.UnitTests.LiturgyOfTheHours;

public class ParticipationOmissionTests
{
    public static TheoryData<int, int, int, Hour, LiturgicalParticipation, string> Cases => new()
    {
        { 2027, 3, 25, Hour.EveningPrayer, LiturgicalParticipation.HolyThursdayEveningMass, "209" },
        { 2027, 3, 26, Hour.EveningPrayer, LiturgicalParticipation.GoodFridayPassion, "209" },
        { 2027, 3, 27, Hour.NightPrayer, LiturgicalParticipation.EasterVigil, "211" },
        { 2028, 4, 13, Hour.EveningPrayer, LiturgicalParticipation.HolyThursdayEveningMass, "209" },
        { 2028, 4, 14, Hour.EveningPrayer, LiturgicalParticipation.GoodFridayPassion, "209" },
        { 2028, 4, 15, Hour.NightPrayer, LiturgicalParticipation.EasterVigil, "211" },
        { 2026, 12, 24, Hour.NightPrayer, LiturgicalParticipation.ChristmasOfficeOfReadingsVigil, "215" },
        { 2027, 12, 24, Hour.NightPrayer, LiturgicalParticipation.ChristmasOfficeOfReadingsVigil, "215" },
        { 2028, 12, 24, Hour.NightPrayer, LiturgicalParticipation.ChristmasOfficeOfReadingsVigil, "215" },
    };

    [Theory]
    [MemberData(nameof(Cases))]
    public void Participation_omits_only_the_relevant_Hour_and_preserves_its_identity(
        int year, int month, int day, Hour hour, LiturgicalParticipation participation, string paragraph)
    {
        var date = new DateOnly(year, month, day);
        foreach (var profile in new[] { "general", "us", "us-thursday" })
        {
            var hours = Hours(profile);
            var normal = hours.Compose(date, hour);
            var omitted = hours.Compose(date, hour, participation: participation);

            Assert.False(normal.IsOmitted);
            Assert.NotEmpty(normal.Steps);
            var rule = Assert.IsType<OfficeOmissionRule>(normal.ConditionalOmission);
            Assert.Equal(participation, rule.Participation);
            Assert.Equal($"General Instruction of the Liturgy of the Hours, no. {paragraph}", rule.SourceLocator);
            Assert.True(omitted.IsOmitted);
            Assert.Empty(omitted.Steps);
            Assert.Equal(rule, omitted.ConditionalOmission);
            Assert.Equal(normal.Date, omitted.Date);
            Assert.Equal(normal.Hour, omitted.Hour);
            Assert.Equal(normal.Title, omitted.Title);
            Assert.Equal(normal.Day, omitted.Day);
            Assert.Equal(normal.PsalterWeek, omitted.PsalterWeek);
            Assert.Equal(normal.IsFirstVespers, omitted.IsFirstVespers);
            Assert.Equal(normal.OfficialSources, omitted.OfficialSources);

            foreach (var otherHour in Enum.GetValues<Hour>().Where(value => value != hour))
            {
                var other = hours.Compose(date, otherHour, participation: participation);
                Assert.False(other.IsOmitted);
                Assert.NotEmpty(other.Steps);
            }
            foreach (var otherParticipation in Enum.GetValues<LiturgicalParticipation>().Where(value => value != participation))
            {
                var other = hours.Compose(date, hour, participation: otherParticipation);
                Assert.False(other.IsOmitted);
                Assert.Equal(normal.Steps.Count, other.Steps.Count);
            }
            foreach (var adjacent in new[] { date.AddDays(-1), date.AddDays(1) })
            {
                var other = hours.Compose(adjacent, hour, participation: participation);
                Assert.False(other.IsOmitted);
                Assert.NotEmpty(other.Steps);
            }

            Assert.False(hours.Compose(date, hour).IsOmitted);
        }
    }

    [Fact]
    public void Christmas_condition_is_the_Office_of_Readings_vigil_not_Mass_alone()
    {
        var office = Hours("general").Compose(new(2026, 12, 24), Hour.NightPrayer);
        var rule = Assert.IsType<OfficeOmissionRule>(office.ConditionalOmission);

        Assert.Equal(LiturgicalParticipation.ChristmasOfficeOfReadingsVigil, rule.Participation);
        Assert.Contains("solemn Office of Readings vigil", rule.AttendanceLabel);
        Assert.Contains("Midnight Mass alone is not this condition", rule.Explanation);
        Assert.False(office.IsOmitted);
        Assert.True(office.IsFirstVespers);
    }

    [Fact]
    public void Ordinary_dates_do_not_offer_an_omission_or_assume_attendance()
    {
        var hours = Hours("general");
        foreach (var hour in Enum.GetValues<Hour>())
        {
            foreach (var participation in Enum.GetValues<LiturgicalParticipation>())
            {
                var office = hours.Compose(new(2026, 11, 11), hour, participation: participation);
                Assert.Null(office.ConditionalOmission);
                Assert.False(office.IsOmitted);
                Assert.NotEmpty(office.Steps);
            }
        }
    }

    [Fact]
    public void Unknown_participation_is_rejected()
    {
        var error = Assert.Throws<ArgumentOutOfRangeException>(() =>
            Hours("general").Compose(new(2027, 3, 25), Hour.EveningPrayer, participation: (LiturgicalParticipation)999));

        Assert.Equal("participation", error.ParamName);
    }

    private static ILiturgyOfTheHours Hours(string profile) => new HoursEngine(
        [new OrdinaryFormOffice(LiturgicalCalendars.Create(PreferredLiturgicalCalendar.Deserialize(profile)), new EmbeddedHoursTextProvider())]);
}
