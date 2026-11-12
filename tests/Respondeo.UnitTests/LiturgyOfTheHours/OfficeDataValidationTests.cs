using Respondeo.LiturgicalCalendar;
using Respondeo.LiturgyOfTheHours;

namespace Respondeo.UnitTests.LiturgyOfTheHours;

public class OfficeDataValidationTests
{
    [Fact]
    public void Embedded_data_and_calendar_binding_sources_validate_together()
    {
        OfficeDataValidation.Validate(OfficeData.Texts, OfficeData.Hours, OfficeData.Sanctoral);
        var keys = Enum.GetValues<CalendarRegion>()
            .SelectMany(region => GeneralRomanCalendarData.ForRegion(region).Values.SelectMany(entries => entries))
            .Concat(ComputedCelebrationData.All).Select(entry => entry.CalendarKey!);

        OfficeDataValidation.ValidateCalendarBindings(OfficeData.Sanctoral, keys);
    }

    [Theory]
    [InlineData("self")]
    [InlineData("indirect")]
    public void Cyclic_text_references_fail_before_expansion(string cycle)
    {
        var entries = new Dictionary<string, string[]>(OfficeData.Texts.Texts)
        {
            ["cycle-a"] = [cycle == "self" ? "{cycle-a}" : "{cycle-b}"],
            ["cycle-b"] = ["{cycle-a}"],
        };

        var error = Assert.Throws<InvalidOperationException>(() => OfficeDataValidation.Validate(
            OfficeData.Texts with { Texts = entries }, OfficeData.Hours, OfficeData.Sanctoral));

        Assert.Contains(OfficeData.TextsResource, error.Message);
        Assert.Contains("Cyclic text reference", error.Message);
        Assert.Contains("cycle-a", error.Message);
    }

    [Theory]
    [InlineData("{missing-text}", "Unknown ordinary text")]
    [InlineData("{{unknown}}", "Unknown ordinary text")]
    [InlineData("{Bad-Key}", "Unrecognized text reference")]
    public void Bad_text_links_identify_the_resource_and_owner(string line, string reason)
    {
        var entries = new Dictionary<string, string[]>(OfficeData.Texts.Texts) { ["broken-text"] = [line] };

        var error = Assert.Throws<InvalidOperationException>(() => OfficeDataValidation.Validate(
            OfficeData.Texts with { Texts = entries }, OfficeData.Hours, OfficeData.Sanctoral));

        Assert.Contains(OfficeData.TextsResource, error.Message);
        Assert.Contains("broken-text", error.Message);
        Assert.Contains(reason, error.Message);
    }

    [Theory]
    [InlineData("text")]
    [InlineData("intro")]
    [InlineData("antiphon")]
    [InlineData("season-text")]
    [InlineData("season-intro")]
    public void Template_references_are_validated_in_every_body_surface(string surface)
    {
        var step = new StepTemplate
        {
            Text = surface == "text" ? ["{missing-text}"] : null,
            IntroText = surface == "intro" ? ["{missing-text}"] : null,
            Antiphon = surface == "antiphon" ? "{missing-text}" : null,
            Seasons = new()
            {
                [LiturgicalSeason.Lent] = new(surface == "season-text" ? ["{missing-text}"] : null, null,
                    surface == "season-intro" ? ["{missing-text}"] : null),
            },
        };
        var hours = OfficeData.Hours with { Hours = new() { [Hour.MorningPrayer] = new("test", [step]) } };

        var error = Assert.Throws<InvalidOperationException>(() =>
            OfficeDataValidation.Validate(OfficeData.Texts, hours, OfficeData.Sanctoral));

        Assert.Contains(OfficeData.HoursResource, error.Message);
        Assert.Contains("hours.MorningPrayer.steps[0]", error.Message);
        Assert.Contains("missing-text", error.Message);
    }

    [Fact]
    public void Unknown_Common_is_an_error_but_absent_optional_proper_content_is_not()
    {
        var unknown = OfficeData.Sanctoral with { Celebrations = new() { ["example"] = new(Common: "missing-common") } };
        var error = Assert.Throws<InvalidOperationException>(() =>
            OfficeDataValidation.Validate(OfficeData.Texts, OfficeData.Hours, unknown));
        Assert.Contains(OfficeData.SanctoralResource, error.Message);
        Assert.Contains("'example' references unknown Common 'missing-common'", error.Message);

        var optional = OfficeData.Sanctoral with
        {
            Celebrations = new(),
            CalendarBindings = new() { ["example"] = "provider-only-example" },
        };
        OfficeDataValidation.Validate(OfficeData.Texts, OfficeData.Hours, optional);
        OfficeDataValidation.ValidateCalendarBindings(optional, ["example"]);
        Assert.False(optional.Celebrations.ContainsKey("provider-only-example"));
    }

    [Fact]
    public void Unknown_calendar_binding_source_is_rejected_without_sampling_calendar_dates()
    {
        var data = OfficeData.Sanctoral with { CalendarBindings = new() { ["unknown-calendar-key"] = "provider-only" } };
        var error = Assert.Throws<InvalidOperationException>(() => OfficeDataValidation.ValidateCalendarBindings(data, ["known-key"]));

        Assert.Contains("unknown calendar identity 'unknown-calendar-key'", error.Message);
    }

    [Fact]
    public void Repeated_acyclic_text_references_and_breviary_tokens_are_valid()
    {
        var data = OfficeData.Texts with
        {
            Texts = new(OfficeData.Texts.Texts)
            {
                ["test-leaf"] = ["{{V}} Test"],
                ["test-root"] = ["{test-leaf}", "{test-leaf}", "{{cross}}"],
            },
        };

        OfficeDataValidation.Validate(data, OfficeData.Hours, OfficeData.Sanctoral);
    }
}
