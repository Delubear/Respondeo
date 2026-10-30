using Microsoft.Extensions.DependencyInjection;
using Respondeo.LiturgicalCalendar;
using Respondeo.LiturgyOfTheHours;

namespace Respondeo.UnitTests.LiturgyOfTheHours;

public class LiturgyOfTheHoursTests
{
    [Fact]
    public void PsalterWeek_FirstSundayOfAdvent_IsWeekOne() =>
        Assert.Equal(1, PsalterWeek.For(new DateOnly(2024, 12, 1), LiturgicalSeason.Advent));

    [Fact]
    public void PsalterWeek_AshWednesday_IsWeekFour() =>
        Assert.Equal(4, PsalterWeek.For(new DateOnly(2025, 3, 5), LiturgicalSeason.Lent));

    [Fact]
    public void PsalterWeek_FirstSundayOfLent_IsWeekOne() =>
        Assert.Equal(1, PsalterWeek.For(new DateOnly(2025, 3, 9), LiturgicalSeason.Lent));

    [Fact]
    public void PsalterWeek_EasterSunday_IsWeekOne() =>
        Assert.Equal(1, PsalterWeek.For(new DateOnly(2025, 4, 20), LiturgicalSeason.Easter));

    [Fact]
    public void OrdinaryTimeWeek_DayAfterBaptism_IsWeekOne() =>
        Assert.Equal(1, PsalterWeek.OrdinaryTimeWeek(new DateOnly(2025, 1, 13), LiturgicalDates.Easter(2025)));

    [Fact]
    public void OrdinaryTimeWeek_UsMondayBaptismRule_IsWeekOne() =>
        Assert.Equal(1, PsalterWeek.OrdinaryTimeWeek(new DateOnly(2024, 1, 9), LiturgicalDates.Easter(2024), epiphanyTransferredToSunday: true));

    [Fact]
    public void OrdinaryTimeWeek_LastSaturdayBeforeAdvent_IsWeekThirtyFour() =>
        Assert.Equal(34, PsalterWeek.OrdinaryTimeWeek(new DateOnly(2025, 11, 29), LiturgicalDates.Easter(2025)));

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
    public void NightPrayer_OrdinaryMonday_UsesPublicDomainConcludingPrayer()
    {
        var prayer = NightStep(new DateOnly(2025, 7, 14), OfficeStepKind.Prayer);

        Assert.False(prayer.UseOfficialText);
        Assert.Contains("Corporibus nostris", prayer.Text);
    }

    [Fact]
    public void NightPrayer_OrdinaryThursday_FallsBackToOfficialSource() =>
        Assert.True(NightStep(new DateOnly(2025, 7, 17), OfficeStepKind.Prayer).UseOfficialText);

    [Fact]
    public void NightPrayer_SaturdayBeforeSunday_UsesFirstVespersPrayer() =>
        Assert.Contains("Visita", NightStep(new DateOnly(2025, 7, 19), OfficeStepKind.Prayer).Text);

    [Fact]
    public void NightPrayer_EasterTime_UsesHaecDiesResponsory() =>
        Assert.Contains("Haec dies", NightStep(new DateOnly(2025, 5, 6), OfficeStepKind.Responsory).Text);

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
