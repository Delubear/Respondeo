using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Respondeo.Bible.Contracts;
using Respondeo.LiturgicalCalendar;
using Respondeo.LiturgyOfTheHours;
using Respondeo.Services;

namespace Respondeo.UnitTests.LiturgyOfTheHours;

public class OfficialTextPreferenceTests
{
    private static readonly DateOnly OrdinaryMonday = new(2027, 1, 11);
    private const string Monday = "ordinarytime/w1/monday";

    [Theory]
    [InlineData(Hour.MorningPrayer)]
    [InlineData(Hour.EveningPrayer)]
    [InlineData(Hour.NightPrayer)]
    public void PublicEngine_DefaultsToSampleProviderAndOverrideDoesNotLeak(Hour hour)
    {
        using var services = new ServiceCollection().AddRespondeoLiturgy().AddRespondeoHours().BuildServiceProvider();
        using var scope = services.CreateScope();
        var hours = scope.ServiceProvider.GetRequiredService<ILiturgyOfTheHours>();

        var enabled = hours.Compose(OrdinaryMonday, hour);
        var disabled = hours.Compose(OrdinaryMonday, hour, preferOfficialText: false);
        var enabledAgain = hours.Compose(OrdinaryMonday, hour);

        Assert.True(enabled.IsSampleContent);
        Assert.Contains("Sample", enabled.Steps.Single(step => step.Kind == OfficeStepKind.Hymn).Text);
        Assert.True(enabled.Steps.Single(step => step.Kind == OfficeStepKind.Hymn).IsSampleText);
        Assert.All(Flatten(enabled.Steps), step => Assert.False(step.IsLicensedText));
        Assert.False(disabled.IsSampleContent);
        Assert.Null(disabled.Acknowledgment);
        Assert.All(Flatten(disabled.Steps), step => Assert.False(step.HasProviderText));
        Assert.Null(disabled.Steps.Single(step => step.Kind == OfficeStepKind.Hymn).Text);
        Assert.True(disabled.Steps.Single(step => step.Kind == OfficeStepKind.Hymn).UseOfficialText);
        Assert.DoesNotContain(Flatten(disabled.Steps), step => step.Antiphon?.Contains("Sample", StringComparison.OrdinalIgnoreCase) == true);
        Assert.Equivalent(Assignment(enabled), Assignment(disabled), strict: true);
        Assert.Equivalent(enabled, enabledAgain, strict: true);
    }

    [Theory]
    [InlineData(Hour.MorningPrayer)]
    [InlineData(Hour.EveningPrayer)]
    [InlineData(Hour.NightPrayer)]
    public void DisabledPreference_DoesNotConsultTheProvider(Hour hour)
    {
        var provider = Substitute.For<IHoursTextProvider>();
        var hours = Engine(provider);

        var office = hours.Compose(OrdinaryMonday, hour, preferOfficialText: false);

        Assert.NotEmpty(office.Steps);
        Assert.Empty(provider.ReceivedCalls());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SuppliedReading_WinsByDefaultAndDisableRestoresBibleFallback(bool sample)
    {
        var provider = new TextProvider(new()
        {
            [$"{Monday}/morning/reading"] = new("Synthetic provider reading", Citation: "2 Thessalonians 3:10-13"),
        }, sample);
        var hours = Engine(provider);
        var enabled = hours.Compose(OrdinaryMonday, Hour.MorningPrayer);
        var disabled = hours.Compose(OrdinaryMonday, Hour.MorningPrayer, preferOfficialText: false);
        var reading = enabled.Steps.Single(step => step.Kind == OfficeStepKind.Reading);
        var fallback = disabled.Steps.Single(step => step.Kind == OfficeStepKind.Reading);
        var bible = Substitute.For<IBibleText>();
        bible.GetPassageAsync("2 Thessalonians 3:10-13").Returns("Synthetic complete Bible verses");

        var suppliedPassages = await new HourPassageLoader(bible).LoadAsync(enabled with { Steps = [reading] });
        var fallbackPassages = await new HourPassageLoader(bible).LoadAsync(disabled with { Steps = [fallback] });

        Assert.Equal("2 Thessalonians 3:10-13", reading.Reference?.Citation);
        Assert.Equal("Synthetic provider reading", reading.Text);
        Assert.Equal(sample, reading.IsSampleText);
        Assert.Equal(!sample, reading.IsLicensedText);
        Assert.False(reading.UseOfficialText);
        Assert.Empty(suppliedPassages.Passages);
        Assert.Equal("Synthetic complete Bible verses", Assert.Single(fallbackPassages.Passages).Value);
        await bible.Received(1).GetPassageAsync("2 Thessalonians 3:10-13");
        Assert.Equivalent(Assignment(enabled), Assignment(disabled), strict: true);
    }

    [Theory]
    [InlineData(Hour.MorningPrayer, OfficeStepKind.Versicle, "morning/invitatory-verse")]
    [InlineData(Hour.MorningPrayer, OfficeStepKind.LordsPrayer, "morning/lords-prayer")]
    [InlineData(Hour.EveningPrayer, OfficeStepKind.Versicle, "evening/opening")]
    [InlineData(Hour.EveningPrayer, OfficeStepKind.Conclusion, "evening/conclusion")]
    [InlineData(Hour.NightPrayer, OfficeStepKind.Examen, "night/examen")]
    [InlineData(Hour.NightPrayer, OfficeStepKind.Responsory, "night/responsory")]
    [InlineData(Hour.NightPrayer, OfficeStepKind.GospelCanticle, "night/gospel-canticle")]
    [InlineData(Hour.NightPrayer, OfficeStepKind.Prayer, "night/collect")]
    [InlineData(Hour.NightPrayer, OfficeStepKind.Conclusion, "night/conclusion")]
    public void ScopedProviderText_ReplacesFixedElementsOnlyWhenEnabled(Hour hour, OfficeStepKind kind, string element)
    {
        var provider = new TextProvider(new() { [$"{Monday}/{element}"] = new("Synthetic scoped body") });
        var hours = Engine(provider);
        var enabled = hours.Compose(OrdinaryMonday, hour);
        var disabled = hours.Compose(OrdinaryMonday, hour, preferOfficialText: false);

        var supplied = enabled.Steps.Single(step => step.Kind == kind);
        Assert.Equal("Synthetic scoped body", supplied.Text);
        Assert.True(supplied.IsLicensedText);
        Assert.NotEqual(supplied.Text, disabled.Steps.Single(step => step.Kind == kind).Text);
        Assert.Equivalent(Assignment(enabled), Assignment(disabled), strict: true);
    }

    [Fact]
    public void AlternativeOpeningAndOptions_UseTheSamePreferenceAndKeepPsalmSlotBindings()
    {
        var provider = new TextProvider(new()
        {
            [$"{Monday}/morning/opening"] = new("Synthetic alternative opening"),
            [$"{Monday}/morning/invitatory/psalm-95"] = new("Synthetic complete Invitatory", "Synthetic Invitatory antiphon", "Psalm 95"),
            [$"{Monday}/morning/psalm-antiphon-1"] = new("", "Synthetic first psalm antiphon", "Psalm 5:2-10, 12-13"),
        });
        var hours = Engine(provider);
        var enabled = hours.Compose(OrdinaryMonday, Hour.MorningPrayer);
        var disabled = hours.Compose(OrdinaryMonday, Hour.MorningPrayer, preferOfficialText: false);
        var opening = enabled.Steps.Single(step => step.Kind == OfficeStepKind.Versicle);
        var option = opening.Options!.Single(step => step.Reference?.Citation == "Psalm 95");

        Assert.Equal("Synthetic alternative opening", opening.IntroText);
        Assert.Equal("Synthetic complete Invitatory", option.Text);
        Assert.Equal("Synthetic Invitatory antiphon", option.Antiphon);
        Assert.True(option.IsLicensedText);
        Assert.Equal("Synthetic first psalm antiphon", enabled.Steps.First(step => step.Kind == OfficeStepKind.Psalm).Antiphon);
        Assert.Null(disabled.Steps.Single(step => step.Kind == OfficeStepKind.Versicle).Options![0].Antiphon);
        Assert.Equivalent(Assignment(enabled), Assignment(disabled), strict: true);
    }

    [Fact]
    public void MarianOptions_ResolveByStableTextKeyWithoutChangingTheChoices()
    {
        var provider = new TextProvider(new()
        {
            [$"{Monday}/night/marian-antiphon/salve-regina"] = new("Synthetic Marian body"),
        });
        var hours = Engine(provider);
        var enabled = hours.Compose(OrdinaryMonday, Hour.NightPrayer);
        var disabled = hours.Compose(OrdinaryMonday, Hour.NightPrayer, preferOfficialText: false);
        var ending = enabled.Steps.Single(step => step.Kind == OfficeStepKind.MarianAntiphon);

        Assert.Equal("Synthetic Marian body", ending.Options!.Single(step => step.ContentElement == "marian-antiphon/salve-regina").Text);
        Assert.True(ending.HasProviderText);
        Assert.NotNull(enabled.Acknowledgment);
        Assert.Equivalent(Assignment(enabled), Assignment(disabled), strict: true);
    }

    [Theory]
    [InlineData("reading", "Wrong 1:1", "Synthetic body")]
    [InlineData("psalm-antiphon-1", "Psalm 149", "Synthetic body")]
    [InlineData("invitatory/psalm-95", "Psalm 100", "Synthetic body")]
    [InlineData("invitatory/psalm-95", null, "Synthetic unbound body")]
    public void InvalidBindings_FailWhenEnabledButCannotAffectDisabledComposition(string element, string? citation, string body)
    {
        var hours = Engine(new TextProvider(new() { [$"{Monday}/morning/{element}"] = new(body, Citation: citation) }));

        Assert.Throws<InvalidDataException>(() => hours.Compose(OrdinaryMonday, Hour.MorningPrayer));
        Assert.NotEmpty(hours.Compose(OrdinaryMonday, Hour.MorningPrayer, preferOfficialText: false).Steps);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" \r\n\t")]
    public void MissingOrEmptyProviderContent_DoesNotClaimSuccessfulResolution(string? body)
    {
        var entries = new Dictionary<string, LicensedText>();
        if (body is not null)
        {
            entries[$"{Monday}/morning/hymn"] = new(body);
        }
        var office = Engine(new TextProvider(entries, sample: true)).Compose(OrdinaryMonday, Hour.MorningPrayer);
        var hymn = office.Steps.Single(step => step.Kind == OfficeStepKind.Hymn);

        Assert.Null(hymn.Text);
        Assert.True(hymn.UseOfficialText);
        Assert.False(hymn.HasProviderText);
        Assert.False(office.IsSampleContent);
        Assert.Null(office.Acknowledgment);
    }

    [Fact]
    public void FirstVespers_FixedTextDoesNotBorrowTheSecondVespersKey()
    {
        var provider = new TextProvider(new()
        {
            ["ordinarytime/w2/saturday/evening-i/opening"] = new("Synthetic First Vespers opening"),
            ["ordinarytime/w2/saturday/evening/opening"] = new("Wrong Second Vespers opening"),
        });
        var office = Engine(provider).Compose(new(2027, 1, 16), Hour.EveningPrayer);

        Assert.True(office.IsFirstVespers);
        Assert.Equal("Synthetic First Vespers opening", office.Steps.Single(step => step.Kind == OfficeStepKind.Versicle).Text);
    }

    [Theory]
    [InlineData("general")]
    [InlineData("us")]
    [InlineData("us-thursday")]
    public void PreferenceModes_PreserveAssignmentsAcrossProfilesAndSeasonalBoundaries(string profile)
    {
        var hours = new OrdinaryFormOffice(LiturgicalCalendars.Create(PreferredLiturgicalCalendar.Deserialize(profile)), new EmbeddedHoursTextProvider());
        DateOnly[] dates = [new(2026, 11, 11), new(2026, 11, 28), new(2026, 12, 24), new(2027, 1, 1), new(2027, 2, 10),
            new(2027, 3, 25), new(2027, 3, 27), new(2027, 3, 28), new(2027, 4, 3), new(2027, 4, 4), new(2027, 5, 6), new(2027, 5, 9)];
        foreach (var date in dates)
        {
            foreach (var hour in Enum.GetValues<Hour>())
            {
                var enabled = hours.Compose(date, hour);
                var disabled = hours.Compose(date, hour, preferOfficialText: false);
                Assert.Equivalent(Assignment(enabled), Assignment(disabled), strict: true);
                Assert.All(Flatten(disabled.Steps), step => Assert.False(step.HasProviderText));
            }
        }
    }

    private static OrdinaryFormOffice Engine(IHoursTextProvider provider) =>
        new(LiturgicalCalendars.Create(LiturgicalCalendarOptions.Default), provider);

    private static Office Assignment(Office office) => office with
    {
        Acknowledgment = null,
        IsSampleContent = false,
        Steps = office.Steps.Select(AssignmentStep).ToArray(),
    };

    private static OfficeStep AssignmentStep(OfficeStep step) => step with
    {
        Text = null, Antiphon = null, IntroText = null, UseOfficialText = false, IsLicensedText = false, IsSampleText = false,
        Options = step.Options?.Select(AssignmentStep).ToArray(),
    };

    private static IEnumerable<OfficeStep> Flatten(IEnumerable<OfficeStep> steps) =>
        steps.SelectMany(step => new[] { step }.Concat(Flatten(step.Options ?? [])));

    private sealed class TextProvider(Dictionary<string, LicensedText> entries, bool sample = false) : IHoursTextProvider
    {
        public bool IsSample => sample;
        public string Acknowledgment => sample ? "Invented sample text; not approved." : "Synthetic test acknowledgment";
        public LicensedText? Find(IEnumerable<string> keys) => keys.Select(key => entries.GetValueOrDefault(key)).FirstOrDefault(text => text is not null);
    }
}
