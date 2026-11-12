using Respondeo.LiturgicalCalendar;

namespace Respondeo.UnitTests.Content;

public class ComputedCelebrationDataTests
{
    private static readonly string[] ExpectedKeys =
    [
        "ash-wednesday",
        "palm-sunday",
        "holy-thursday",
        "good-friday",
        "holy-saturday",
        "easter-sunday",
        "ascension",
        "pentecost",
        "trinity",
        "corpus-christi",
        "mother-of-the-church",
        "sacred-heart",
        "immaculate-heart",
        "thanksgiving",
        "holy-family",
        "epiphany",
        "baptism-of-the-lord",
        "christ-the-king",
    ];

    [Fact]
    public void Embedded_catalog_defines_each_computed_calendar_identity_once()
    {
        var definitions = ComputedCelebrationData.All.ToArray();

        Assert.Equal(ExpectedKeys.OrderBy(key => key, StringComparer.Ordinal),
            definitions.Select(definition => definition.CalendarKey).OrderBy(key => key, StringComparer.Ordinal));
        Assert.All(definitions, definition =>
            Assert.Equal(definition, ComputedCelebrationData.Get(definition.CalendarKey!)));
    }

    [Theory]
    [InlineData("Bad-Key | Feast | White | Example | FeastOfTheLord", "lowercase kebab-case")]
    [InlineData("example | Unknown | White | Example | FeastOfTheLord", "Invalid CelebrationRank")]
    [InlineData("example | Feast | White | Example | Unknown", "Invalid LiturgicalPrecedenceClass")]
    [InlineData("example | 0 | White | Example | FeastOfTheLord", "Invalid CelebrationRank")]
    [InlineData("example | Feast | 0 | Example | FeastOfTheLord", "Invalid LiturgicalColor")]
    [InlineData("example | Feast | White | Example | 5", "Invalid LiturgicalPrecedenceClass")]
    [InlineData("example | Memorial, Feast | White | Example | FeastOfTheLord", "Invalid CelebrationRank")]
    [InlineData("bad--key | Feast | White | Example | FeastOfTheLord", "lowercase kebab-case")]
    [InlineData("example | Memorial | White | Example | FeastOfTheLord", "incompatible with rank")]
    [InlineData("example | Feast | White |  | FeastOfTheLord", "must not be blank")]
    [InlineData("example | Feast | White | Example", "Expected five fields")]
    public void Invalid_definition_reports_resource_and_line(string row, string reason)
    {
        using var reader = new StringReader($"# Header\n{row}");

        var error = Assert.Throws<InvalidOperationException>(() => ComputedCelebrationData.Load(reader, "Computed.txt"));

        Assert.Contains("'Computed.txt' at line 2:", error.Message);
        Assert.Contains(reason, error.Message);
    }

    [Fact]
    public void Duplicate_computed_identity_is_rejected_with_its_source_line()
    {
        const string row = "example | Feast | White | Example | FeastOfTheLord";
        using var reader = new StringReader($"{row}\n{row}");

        var error = Assert.Throws<InvalidOperationException>(() => ComputedCelebrationData.Load(reader, "Computed.txt"));

        Assert.Contains("'Computed.txt' at line 2:", error.Message);
        Assert.Contains("Duplicate calendar key 'example'", error.Message);
    }

    [Theory]
    [InlineData("solemnity", "propersolemnity", CelebrationRank.Solemnity, LiturgicalPrecedenceClass.ProperSolemnity)]
    [InlineData("feast", "feastofthelord", CelebrationRank.Feast, LiturgicalPrecedenceClass.FeastOfTheLord)]
    [InlineData("memorial", "generalmemorial", CelebrationRank.Memorial, LiturgicalPrecedenceClass.GeneralMemorial)]
    [InlineData("optionalmemorial", "optionalmemorial", CelebrationRank.OptionalMemorial, LiturgicalPrecedenceClass.OptionalMemorial)]
    public void Fixed_and_computed_parsers_preserve_the_same_named_metadata(
        string rank, string precedence, CelebrationRank expectedRank, LiturgicalPrecedenceClass expectedPrecedence)
    {
        using var computed = new StringReader($"example-2 | {rank} | white | Example | {precedence}");
        using var fixedDate = new StringReader($"01-01 | {rank} | white | Example | | | example-2 | {precedence}");
        var expected = new LiturgicalCelebration("Example", expectedRank, LiturgicalColor.White)
        {
            CalendarKey = "example-2",
            Precedence = expectedPrecedence,
        };

        Assert.Equal(expected, ComputedCelebrationData.Load(computed, "Computed.txt")["example-2"]);
        Assert.Equal(expected, Assert.Single(GeneralRomanCalendarData.Load(fixedDate, "Fixed.txt")[(1, 1)]));
    }
}
