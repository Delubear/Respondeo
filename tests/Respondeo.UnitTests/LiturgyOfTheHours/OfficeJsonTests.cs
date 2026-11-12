using System.Text;
using System.Text.Json;
using Respondeo.LiturgyOfTheHours;

namespace Respondeo.UnitTests.LiturgyOfTheHours;

public class OfficeJsonTests
{
    [Theory]
    [InlineData("{\"title\":\"A\",\"title\":\"B\"}", "Duplicate property")]
    [InlineData("{\"title\":\"A\",\"Title\":\"B\"}", "Duplicate property")]
    [InlineData("{\"text\":[\"A\"],\"typo\":true}", "typo")]
    [InlineData("{\"seasons\":{\"Lent\":{\"text\":[],\"text\":[]}}}", "Duplicate property")]
    [InlineData("{\"source\":999}", "could not be converted")]
    [InlineData("{\"source\":\"999\"}", "could not be converted")]
    [InlineData("{\"source\":\"typo\"}", "could not be converted")]
    [InlineData("{\"kind\":999}", "could not be converted")]
    [InlineData("{\"kind\":\"Typo\"}", "could not be converted")]
    [InlineData("{\"seasons\":{\"Typo\":{}}}", "could not be converted")]
    [InlineData("{\"seasons\":{\"999\":{}}}", "could not be converted")]
    [InlineData("null", "root value must not be null")]
    [InlineData("{", "Expected")]
    public void Authoring_errors_are_rejected_with_resource_and_original_JSON_error(string json, string reason)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

        var error = Assert.Throws<InvalidOperationException>(() => OfficeJson.Read<StepTemplate>(stream, "authored.json"));

        Assert.Contains("'authored.json'", error.Message);
        Assert.Contains(reason, error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.IsAssignableFrom<JsonException>(error.InnerException);
    }

    [Theory]
    [InlineData("fixed")]
    [InlineData("short-reading")]
    [InlineData("night-responsory")]
    public void Comments_trailing_commas_and_enum_member_names_remain_supported(string source)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(
            "// Authored comment\n{\"source\":\"" + source + "\",\"title\":\"Example\",}"));

        var step = OfficeJson.Read<StepTemplate>(stream, "authored.json");

        Assert.Equal(source switch
        {
            "fixed" => StepSource.Fixed,
            "short-reading" => StepSource.ShortReading,
            _ => StepSource.NightResponsory,
        }, step.Source);
        Assert.Equal("Example", step.Title);
    }

    [Fact]
    public void Duplicate_dictionary_keys_and_missing_required_constructor_fields_are_rejected()
    {
        using var duplicate = new MemoryStream(Encoding.UTF8.GetBytes("{\"morning\":{\"example\":\"A\",\"example\":\"B\"}}"));
        var error = Assert.Throws<InvalidOperationException>(() => OfficeJson.Read<SanctoralReadings>(duplicate, "duplicate.json"));
        Assert.Contains("Duplicate property 'example' at $.morning", error.Message);

        using var missing = new MemoryStream(Encoding.UTF8.GetBytes("{}"));
        var missingError = Assert.Throws<InvalidOperationException>(() => OfficeJson.Read<HourTemplate>(missing, "missing.json"));
        Assert.Contains("missing required properties", missingError.Message);
    }

    [Fact]
    public void Every_embedded_resource_still_loads_under_strict_parsing()
    {
        Assert.Equal(4, OfficeData.Psalter.Morning.Count);
        Assert.NotEmpty(OfficeData.Texts.Texts);
        Assert.Equal(3, OfficeData.Hours.Hours.Count);
        Assert.NotEmpty(OfficeData.Sanctoral.Commons);
        Assert.Equal(4, OfficeData.TemporalPsalmody.Triduum.Count);
    }
}
