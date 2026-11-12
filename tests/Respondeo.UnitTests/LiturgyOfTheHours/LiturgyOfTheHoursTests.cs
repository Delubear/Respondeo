using Microsoft.Extensions.DependencyInjection;
using Respondeo.LiturgicalCalendar;
using Respondeo.LiturgyOfTheHours;

namespace Respondeo.UnitTests.LiturgyOfTheHours;

public class LiturgyOfTheHoursTests
{
    [Theory]
    [InlineData("Excerpts from the English translation of The Roman Missal © 2010, International Commission on English in the Liturgy Corporation. All rights reserved.", true)]
    [InlineData("The English translation of The Liturgy of the Hours © 1974, ICEL. All rights reserved.", true)]
    [InlineData("The English translation of The Liturgy of the Hours 1974, ICEL. All rights reserved.", false)]
    [InlineData("The English translation of The Liturgy of the Hours © 1974. All rights reserved.", false)]
    [InlineData("The English translation of The Liturgy of the Hours © 1974, ICEL.", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void HoursAcknowledgment_RequiresIcelElements(string? acknowledgment, bool expected) =>
        Assert.Equal(expected, HoursAcknowledgment.IsValid(acknowledgment));

    [Fact]
    public void MorningPrayer_InvitatoryOptions_HaveCanonicalStanzas()
    {
        var office = Hours().Compose(new DateOnly(2025, 1, 15), Hour.MorningPrayer);
        var options = office.Steps.Where(s => s.Options is not null).SelectMany(s => s.Options!).Where(o => o.Reference is not null).ToList();

        Assert.NotEmpty(options);
        foreach (var option in options)
        {
            Assert.NotNull(option.Stanzas);
            Assert.NotEmpty(option.Stanzas);
            Assert.All(option.Stanzas, s => Assert.StartsWith(option.Reference!.Citation + ":", s.Citation));
        }
    }

    [Fact]
    public void Hours_UseTheSelectedCelebrationWithoutResolvingTheCalendarTie()
    {
        var date = new DateOnly(2038, 6, 24);
        var calendarDay = LiturgicalCalendars.Create(LiturgicalCalendarOptions.Default).ForDate(date);
        var john = Assert.Single(calendarDay.Occurrence!.ContendingCelebrations,
            contender => contender.CalendarKey == "nativity-of-saint-john-the-baptist");
        var hours = Hours();

        var defaultOffice = hours.Compose(date, Hour.MorningPrayer);
        var selectedOffice = hours.Compose(date, Hour.MorningPrayer, celebrationCalendarKey: john.CalendarKey);
        var selectedVespers = hours.Compose(date, Hour.EveningPrayer, celebrationCalendarKey: john.CalendarKey);
        var defaultVespers = hours.Compose(date, Hour.EveningPrayer);
        var vespersBefore = hours.Compose(date.AddDays(-1), Hour.EveningPrayer, celebrationCalendarKey: john.CalendarKey);

        Assert.Equal(calendarDay.Celebration?.CalendarKey, defaultOffice.Day.Celebration?.CalendarKey);
        Assert.Null(defaultOffice.SelectedCelebrationCalendarKey);
        Assert.Equal(john.CalendarKey, selectedOffice.SelectedCelebrationCalendarKey);
        Assert.Equal(john.CalendarKey, selectedOffice.Day.Celebration?.CalendarKey);
        Assert.Equal(john.Color, selectedOffice.Day.Color);
        Assert.Equal(LiturgicalOccurrenceRule.UnresolvedEqualPrecedence, selectedOffice.Day.Occurrence?.Rule);
        Assert.Equal(2, selectedOffice.Day.Occurrence?.ContendingCelebrations.Count);
        Assert.Equal(john.CalendarKey, selectedVespers.Day.Celebration?.CalendarKey);
        Assert.Equal(["Psalm 16", "Psalm 21:2-8, 14", "Revelation 15:3-4"], selectedVespers.Steps
            .Where(step => step.Kind is OfficeStepKind.Psalm or OfficeStepKind.Canticle)
            .Select(step => step.Reference?.Citation));
        Assert.Equal(["Psalm 110:1-5, 7", "Psalm 111", "Revelation 19:1-7"], defaultVespers.Steps
            .Where(step => step.Kind is OfficeStepKind.Psalm or OfficeStepKind.Canticle)
            .Select(step => step.Reference?.Citation));
        Assert.True(vespersBefore.IsFirstVespers);
        Assert.Equal(john.CalendarKey, vespersBefore.Day.Celebration?.CalendarKey);
        Assert.Throws<ArgumentException>(() => hours.Compose(date.AddDays(1), Hour.MorningPrayer,
            celebrationCalendarKey: john.CalendarKey));
    }

    private static LiturgicalWeek Week(DateOnly date, bool unitedStates = false) =>
        LiturgicalCalendars.Create(new(unitedStates ? CalendarRegion.UnitedStates : CalendarRegion.GeneralRoman)).ForWeek(date);

    [Fact]
    public void PsalterWeek_FirstSundayOfAdvent_IsWeekOne() =>
        Assert.Equal(1, Week(new(2024, 12, 1)).PsalterWeek);

    [Fact]
    public void PsalterWeek_AshWednesday_IsWeekFour() =>
        Assert.Equal(4, Week(new(2025, 3, 5)).PsalterWeek);

    [Fact]
    public void PsalterWeek_FirstSundayOfLent_IsWeekOne() =>
        Assert.Equal(1, Week(new(2025, 3, 9)).PsalterWeek);

    [Fact]
    public void PsalterWeek_EasterSunday_IsWeekOne() =>
        Assert.Equal(1, Week(new(2025, 4, 20)).PsalterWeek);

    [Theory]
    [InlineData(2025, 5, 25, 2)]
    [InlineData(2025, 5, 31, 2)]
    [InlineData(2025, 6, 1, 3)]
    [InlineData(2025, 6, 7, 3)]
    public void PsalterWeek_EasterWeeksSixAndSeven_Cycle(int year, int month, int day, int expected) =>
        Assert.Equal(expected, Week(new(year, month, day)).PsalterWeek);

    [Theory]
    [InlineData(2023, 12, 29, 1)]
    [InlineData(2023, 12, 31, 1)]
    [InlineData(2024, 1, 2, 1)]
    [InlineData(2024, 1, 7, 2)]
    [InlineData(2025, 12, 29, 1)]
    [InlineData(2026, 1, 4, 2)]
    [InlineData(2022, 12, 30, 1)]
    [InlineData(2023, 1, 1, 1)]
    public void PsalterWeek_ChristmasTime_PreservesProvisionalBackgroundCycle(int year, int month, int day, int expected) =>
        Assert.Equal(expected, Week(new(year, month, day)).PsalterWeek);

    [Fact]
    public void OrdinaryTimeWeek_DayAfterBaptism_IsWeekOne() =>
        Assert.Equal(1, Week(new(2025, 1, 13)).TemporalWeek);

    [Fact]
    public void OrdinaryTimeWeek_UsMondayBaptismRule_IsWeekOne() =>
        Assert.Equal(1, Week(new(2024, 1, 9), unitedStates: true).TemporalWeek);

    [Fact]
    public void OrdinaryTimeWeek_LastSaturdayBeforeAdvent_IsWeekThirtyFour() =>
        Assert.Equal(34, Week(new(2025, 11, 29)).TemporalWeek);

    [Fact]
    public void Easter_2025_IsApril20() =>
        Assert.Equal(new DateOnly(2025, 4, 20), LiturgicalDates.Easter(2025));

    [Fact]
    public void FirstSundayOfAdvent_2025_IsNovember30() =>
        Assert.Equal(new DateOnly(2025, 11, 30), LiturgicalDates.FirstSundayOfAdvent(2025));

    [Fact]
    public void BaptismOfTheLord_2024_UsRule_IsJanuary8() =>
        Assert.Equal(new DateOnly(2024, 1, 8), LiturgicalDates.BaptismOfTheLord(2024, true));

    private static ILiturgyOfTheHours Hours() =>
        new Microsoft.Extensions.DependencyInjection.ServiceCollection()
            .AddRespondeoLiturgy()
            .AddRespondeoHours()
            .BuildServiceProvider()
            .CreateScope()
            .ServiceProvider
            .GetRequiredService<ILiturgyOfTheHours>();

    private static OfficeStep NightStep(DateOnly date, OfficeStepKind kind) =>
        Hours().Compose(date, Hour.NightPrayer).Steps.First(s => s.Kind == kind);

    [Fact]
    public void MorningPrayer_MemorialOfMartyr_ShowsCommonShortReading()
    {
        var reading = Hours().Compose(new DateOnly(2026, 1, 21), Hour.MorningPrayer).Steps.First(s => s.Kind == OfficeStepKind.Reading);

        Assert.False(reading.UseOfficialText);
        Assert.Equal("2 Corinthians 1:3-5", reading.Reference?.Citation);
    }

    [Fact]
    public void EveningPrayer_MemorialOfBishop_ShowsCommonOfPastorsReading()
    {
        var reading = Hours().Compose(new DateOnly(2026, 11, 4), Hour.EveningPrayer).Steps.First(s => s.Kind == OfficeStepKind.Reading);

        Assert.Equal("1 Peter 5:1-4", reading.Reference?.Citation);
    }

    [Fact]
    public void EveningPrayer_Advent_FallsBackToProperReading()
    {
        var reading = Hours().Compose(new DateOnly(2025, 12, 2), Hour.EveningPrayer).Steps.First(s => s.Kind == OfficeStepKind.Reading);

        Assert.Null(reading.Reference);
    }

    [Fact]
    public void NightPrayer_OrdinaryMonday_UsesPublicDomainConcludingPrayer()
    {
        var prayer = NightStep(new DateOnly(2025, 7, 14), OfficeStepKind.Prayer);

        Assert.False(prayer.UseOfficialText);
        Assert.Contains("Corporibus nostris", prayer.Text);
    }

    [Fact]
    public void NightPrayer_OrdinaryThursday_UsesPublicDomainConcludingPrayer()
    {
        var prayer = NightStep(new DateOnly(2025, 7, 17), OfficeStepKind.Prayer);

        Assert.False(prayer.UseOfficialText);
        Assert.Contains("diurno labore fatigatos", prayer.Text);
    }

    [Fact]
    public void NightPrayer_SaturdayBeforeSunday_UsesFirstVespersPrayer() =>
        Assert.Contains("Visita", NightStep(new DateOnly(2025, 7, 19), OfficeStepKind.Prayer).Text);

    [Fact]
    public void NightPrayer_EasterWeek_UsesHaecDiesResponsory() =>
        Assert.Contains("Haec dies", NightStep(new DateOnly(2027, 3, 30), OfficeStepKind.Responsory).Text);

    [Fact]
    public void MorningPrayer_DefaultBuild_UsesMarkedSampleContent()
    {
        var office = Hours().Compose(new DateOnly(2025, 7, 14), Hour.MorningPrayer);

        Assert.True(office.IsSampleContent);
        Assert.False(string.IsNullOrWhiteSpace(office.Acknowledgment));
        Assert.Contains("Sample", office.Steps.First(s => s.Kind == OfficeStepKind.Hymn).Text);
        Assert.All(office.Steps.Where(s => s.Kind is OfficeStepKind.Psalm or OfficeStepKind.Canticle), s => Assert.NotNull(s.Antiphon));
    }
}
