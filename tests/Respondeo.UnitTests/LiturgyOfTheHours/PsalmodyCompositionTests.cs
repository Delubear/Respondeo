using Respondeo.LiturgicalCalendar;
using Respondeo.LiturgyOfTheHours;

namespace Respondeo.UnitTests.LiturgyOfTheHours;

public class PsalmodyCompositionTests
{
    private const string Martin = "proper/saint-martin-of-tours-bishop";

    [Theory]
    [InlineData(Hour.MorningPrayer, "morning", "Psalm 63:2-9")]
    [InlineData(Hour.EveningPrayer, "evening", "Psalm 15")]
    public void ProperPsalmody_DoesNotBorrowCommonWeekdayOrGenericAntiphons(Hour hour, string key, string citation)
    {
        var provider = new TextProvider(new()
        {
            [$"common/pastors/{key}/psalm-antiphon-1"] = new("", "Wrong Common"),
            [$"ordinarytime/w4/wednesday/{key}/psalm-antiphon-1"] = new("", "Wrong weekday"),
            [$"{key}/psalm-antiphon-1"] = new("", "Wrong generic"),
            ["psalm-antiphon-1"] = new("", "Wrong generic"),
        });

        var step = Psalmody(Compose(new(2026, 11, 11), hour, provider))[0];

        Assert.Equal(citation, step.Reference?.Citation);
        Assert.Null(step.Antiphon);
        Assert.Null(step.Text);
        Assert.True(step.UseOfficialText);
        Assert.False(step.IsLicensedText);
    }

    [Theory]
    [InlineData(Hour.MorningPrayer, "morning", "Psalm 63:2-9")]
    [InlineData(Hour.EveningPrayer, "evening", "Psalm 15")]
    public void ProperText_BindsToItsCitationAndAntiphon(Hour hour, string key, string citation)
    {
        var provider = new TextProvider(new()
        {
            [$"{Martin}/{key}/psalm-antiphon-1"] = new("Synthetic complete body", "Synthetic proper antiphon", citation),
        });

        var step = Psalmody(Compose(new(2026, 11, 11), hour, provider))[0];

        Assert.Equal(citation, step.Reference?.Citation);
        Assert.Equal("Synthetic complete body", step.Text);
        Assert.Equal("Synthetic proper antiphon", step.Antiphon);
        Assert.True(step.IsLicensedText);
        Assert.False(step.UseOfficialText);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void MissingOrBlankProperAntiphon_RemainsExplicitlyUnavailable(string? antiphon)
    {
        var provider = new TextProvider(new()
        {
            [$"{Martin}/morning/psalm-antiphon-1"] = new("Synthetic complete body", antiphon, "Psalm 63:2-9"),
        });

        var step = Psalmody(Compose(new(2026, 11, 11), Hour.MorningPrayer, provider))[0];

        Assert.Equal("Synthetic complete body", step.Text);
        Assert.Null(step.Antiphon);
        Assert.True(step.UseOfficialText);
    }

    [Theory]
    [InlineData("Psalm 108")]
    [InlineData(null)]
    public void ProperBody_WithWrongOrAbsentCitation_IsRejected(string? citation)
    {
        var provider = new TextProvider(new()
        {
            [$"{Martin}/morning/psalm-antiphon-1"] = new("Synthetic wrong body", "Synthetic antiphon", citation),
        });

        var error = Assert.Throws<InvalidDataException>(() => Compose(new(2026, 11, 11), Hour.MorningPrayer, provider));

        Assert.Contains("Psalm 63:2-9", error.Message);
    }

    [Fact]
    public void AntiphonWithWrongCitation_IsRejectedEvenWithoutABody()
    {
        var provider = new TextProvider(new()
        {
            [$"{Martin}/morning/psalm-antiphon-1"] = new("", "Synthetic antiphon", "Psalm 108"),
        });

        Assert.Throws<InvalidDataException>(() => Compose(new(2026, 11, 11), Hour.MorningPrayer, provider));
    }

    [Fact]
    public void OrdinaryMemorial_UsesWeekdayPsalmAntiphonButMayUseCommonGospelAntiphon()
    {
        var provider = new TextProvider(new()
        {
            ["common/doctors/morning/psalm-antiphon-1"] = new("", "Wrong Common psalm antiphon"),
            ["ordinarytime/w4/tuesday/morning/psalm-antiphon-1"] = new("", "Synthetic weekday antiphon"),
            ["common/doctors/morning/gospel-canticle"] = new("", "Synthetic Common Gospel antiphon"),
        });

        var office = Compose(new(2026, 11, 10), Hour.MorningPrayer, provider);

        Assert.Equal("Psalm 101", Psalmody(office)[0].Reference?.Citation);
        Assert.Equal("Synthetic weekday antiphon", Psalmody(office)[0].Antiphon);
        Assert.Equal("Synthetic Common Gospel antiphon", Assert.Single(office.Steps, s => s.Kind == OfficeStepKind.GospelCanticle).Antiphon);
    }

    [Fact]
    public void FeastUsingCommon_UsesTheSameCommonForPsalmodyAndAntiphons()
    {
        var provider = new TextProvider(new()
        {
            ["common/apostles/evening/psalm-antiphon-1"] = new("", "Synthetic apostle antiphon", "Psalm 116:10-19"),
            ["ordinarytime/w1/monday/evening/psalm-antiphon-1"] = new("", "Wrong weekday antiphon"),
        });

        var step = Psalmody(Compose(new(2026, 11, 30), Hour.EveningPrayer, provider))[0];

        Assert.Equal("Psalm 116:10-19", step.Reference?.Citation);
        Assert.Equal("Synthetic apostle antiphon", step.Antiphon);
    }

    [Fact]
    public void SundayFirstVespers_UsesTheIncomingWeekAndSaturdayPsalmodyKeys()
    {
        var provider = new TextProvider(new()
        {
            ["ordinarytime/w4/saturday/evening/psalm-antiphon-1"] = new("", "Wrong outgoing week"),
            ["ordinarytime/w1/saturday/evening/psalm-antiphon-1"] = new("", "Synthetic incoming week", "Psalm 141:1-9"),
            ["ordinarytime/w1/sunday/evening/psalm-antiphon-1"] = new("", "Wrong Sunday evening"),
        });

        var office = Compose(new(2026, 11, 14), Hour.EveningPrayer, provider);

        Assert.True(office.IsFirstVespers);
        Assert.Equal("Synthetic incoming week", Psalmody(office)[0].Antiphon);
    }

    [Fact]
    public void SolemnityFirstVespers_DoesNotUseSecondVespersTexts()
    {
        var provider = new TextProvider(new()
        {
            ["proper/all-saints/evening/hymn"] = new("Wrong second Vespers hymn"),
            ["proper/all-saints/hymn"] = new("Ambiguous hymn"),
            ["proper/all-saints/evening-i/hymn"] = new("Synthetic first Vespers hymn"),
        });

        var office = Compose(new(2026, 10, 31), Hour.EveningPrayer, provider);

        Assert.True(office.IsFirstVespers);
        Assert.Equal("Synthetic first Vespers hymn", Assert.Single(office.Steps, s => s.Kind == OfficeStepKind.Hymn).Text);
    }

    [Fact]
    public void NightPrayer_DoesNotUseMemorialPropersOrCommons()
    {
        var provider = new TextProvider(new()
        {
            [$"{Martin}/night/psalm-antiphon-1"] = new("", "Wrong saint antiphon"),
            [$"{Martin}/night/hymn"] = new("Wrong saint hymn"),
            ["common/pastors/night/hymn"] = new("Wrong Common hymn"),
            ["ordinarytime/w4/wednesday/night/psalm-antiphon-1"] = new("", "Synthetic ferial antiphon"),
            ["night/hymn"] = new("Synthetic night hymn"),
        });

        var office = Compose(new(2026, 11, 11), Hour.NightPrayer, provider);

        Assert.Equal("Synthetic ferial antiphon", Psalmody(office)[0].Antiphon);
        Assert.Equal("Synthetic night hymn", Assert.Single(office.Steps, s => s.Kind == OfficeStepKind.Hymn).Text);
    }

    [Theory]
    [InlineData(CalendarRegion.GeneralRoman)]
    [InlineData(CalendarRegion.UnitedStates)]
    public void MartinAcrossFourHundredYears_RespectsSundaySuppressionAndEveningConcurrence(CalendarRegion region)
    {
        var calendar = LiturgicalCalendars.Create(new LiturgicalCalendarOptions(region));
        var engine = new OrdinaryFormOffice(calendar, new TextProvider(new()));
        for (var year = 2000; year < 2400; year++)
        {
            var date = new DateOnly(year, 11, 11);
            var morning = engine.Compose(date, Hour.MorningPrayer);
            var evening = engine.Compose(date, Hour.EveningPrayer);
            if (date.DayOfWeek == DayOfWeek.Sunday)
            {
                Assert.Null(morning.Day.Celebration);
                Assert.DoesNotContain("saint-martin", morning.Title, StringComparison.OrdinalIgnoreCase);
                continue;
            }

            Assert.Equal("Saint Martin of Tours, Bishop", morning.Day.Celebration?.Name);
            Assert.Equal(new[] { "Psalm 63:2-9", "Daniel 3:57-88, 56", "Psalm 149" }, Psalmody(morning).Select(s => s.Reference!.Citation));
            if (date.DayOfWeek == DayOfWeek.Saturday)
            {
                Assert.True(evening.IsFirstVespers);
                Assert.Equal(date.AddDays(1), evening.Day.Date);
            }
            else
            {
                Assert.False(evening.IsFirstVespers);
                Assert.Equal(new[] { "Psalm 15", "Psalm 112", "Revelation 15:3-4" }, Psalmody(evening).Select(s => s.Reference!.Citation));
            }
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void ContentMerge_RejectsFullPsalmodyBodyWithoutCitation(string? citation)
    {
        var file = new HoursContentFile
        {
            Texts = new() { [$"{Martin}/morning/psalm-antiphon-1"] = new("Synthetic full body", Citation: citation) },
        };

        var error = Assert.Throws<InvalidDataException>(() => HoursContentFile.Merge([("synthetic.json", file)]));

        Assert.Contains("canonical citation", error.Message);
    }

    private static Office Compose(DateOnly date, Hour hour, IHoursTextProvider provider) =>
        new OrdinaryFormOffice(LiturgicalCalendars.Create(LiturgicalCalendarOptions.Default), provider).Compose(date, hour);

    private static OfficeStep[] Psalmody(Office office) =>
        office.Steps.Where(s => s.Kind is OfficeStepKind.Psalm or OfficeStepKind.Canticle).ToArray();

    private sealed class TextProvider(Dictionary<string, LicensedText> texts) : IHoursTextProvider
    {
        public bool IsSample => false;

        public string Acknowledgment => "Synthetic test content only";

        public LicensedText? Find(IEnumerable<string> keys) => keys.Select(k => texts.GetValueOrDefault(k)).FirstOrDefault(t => t is not null);
    }
}
