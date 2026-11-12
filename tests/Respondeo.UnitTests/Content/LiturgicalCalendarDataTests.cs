using Respondeo.LiturgicalCalendar;

namespace Respondeo.UnitTests.Content;

public class LiturgicalCalendarDataTests
{
    [Theory]
    [InlineData("02-30 | Memorial | White | Example", "Invalid date")]
    [InlineData("04-31 | Memorial | White | Example", "Invalid date")]
    [InlineData("00-01 | Memorial | White | Example", "Invalid date")]
    [InlineData("13-01 | Memorial | White | Example", "Invalid date")]
    [InlineData("01-00 | Memorial | White | Example", "Invalid date")]
    [InlineData("01-32 | Memorial | White | Example", "Invalid date")]
    [InlineData("1-01 | Memorial | White | Example", "Invalid date")]
    [InlineData("01-1 | Memorial | White | Example", "Invalid date")]
    [InlineData("01/01 | Memorial | White | Example", "Invalid date")]
    [InlineData(" | Memorial | White | Example", "Invalid date")]
    [InlineData("01-01 | 0 | White | Example", "Invalid CelebrationRank")]
    [InlineData("01-01 | 99 | White | Example", "Invalid CelebrationRank")]
    [InlineData("01-01 | Unknown | White | Example", "Invalid CelebrationRank")]
    [InlineData("01-01 | Memorial, Feast | White | Example", "Invalid CelebrationRank")]
    [InlineData("01-01 | Memorial | 2 | Example", "Invalid LiturgicalColor")]
    [InlineData("01-01 | Memorial | 99 | Example", "Invalid LiturgicalColor")]
    [InlineData("01-01 | Memorial | Unknown | Example", "Invalid LiturgicalColor")]
    [InlineData("01-01 | Memorial | White, Red | Example", "Invalid LiturgicalColor")]
    [InlineData("01-01 | Memorial | White |   ", "Celebration name must not be blank")]
    [InlineData("01-01 | Memorial | White", "Expected 4 to 6")]
    [InlineData("01-01 | Memorial | White | Example | id | summary | extra", "Expected 4 to 6")]
    [InlineData("-01-01 | Example", "Removals are permitted only in a regional proper")]
    public void Invalid_general_rows_report_source_and_physical_line(string row, string reason)
    {
        using var reader = new StringReader($"# Comment\n\n{row}");

        var error = Assert.Throws<InvalidOperationException>(() => GeneralRomanCalendarData.Load(reader, "General.txt"));

        Assert.Contains("'General.txt' at line 3:", error.Message);
        Assert.Contains(reason, error.Message);
    }

    [Theory]
    [InlineData("02-29 | Memorial | White | Example", null, null)]
    [InlineData("02-29 | Memorial | White | Example | profile", "profile", null)]
    [InlineData("02-29 | Memorial | White | Example | profile | Description", "profile", "Description")]
    [InlineData(" 02-29 | memorial | white | Example | | ", null, null)]
    public void Valid_rows_preserve_trimmed_fields_and_allow_leap_day(string row, string? id, string? summary)
    {
        using var reader = new StringReader($"# Comment\n\n{row}\n");

        var map = GeneralRomanCalendarData.Load(reader, "General.txt");

        var entry = Assert.Single(map);
        Assert.Equal((2, 29), entry.Key);
        Assert.Equal(new LiturgicalCelebration("Example", CelebrationRank.Memorial, LiturgicalColor.White)
        {
            Id = id,
            Summary = summary,
        }, Assert.Single(entry.Value));
    }

    [Fact]
    public void Duplicate_general_identity_is_rejected_even_when_rank_differs()
    {
        using var reader = new StringReader("01-01 | Memorial | White | Example\n01-01 | Feast | Red | Example");

        var error = Assert.Throws<InvalidOperationException>(() => GeneralRomanCalendarData.Load(reader, "General.txt"));

        Assert.Contains("'General.txt' at line 2:", error.Message);
        Assert.Contains("Duplicate or conflicting operation for 'Example' on 01-01", error.Message);
    }

    [Fact]
    public void Distinct_celebrations_on_one_date_and_same_name_on_different_dates_are_retained()
    {
        using var reader = new StringReader("01-01 | OptionalMemorial | White | One\n01-01 | OptionalMemorial | Red | Two\n01-02 | Feast | White | One");

        var map = GeneralRomanCalendarData.Load(reader, "General.txt");

        Assert.Equal(2, map.Count);
        Assert.Equal(new[] { "One", "Two" }, map[(1, 1)].Select(celebration => celebration.Name));
        Assert.Equal(new LiturgicalCelebration("One", CelebrationRank.Feast, LiturgicalColor.White), Assert.Single(map[(1, 2)]));
    }

    [Theory]
    [InlineData("-01-02 | Example", "must match exactly one General entry")]
    [InlineData("-01-01 | Missing", "must match exactly one General entry")]
    [InlineData("-01-01 | example", "must match exactly one General entry")]
    [InlineData("-02-30 | Example", "Invalid date")]
    [InlineData("-01-01 |   ", "Celebration name must not be blank")]
    [InlineData("-01-01", "Removals are permitted only in a regional proper")]
    [InlineData("-01-01 | Example | extra", "Removals are permitted only in a regional proper")]
    [InlineData("01-02 | 2 | White | Another", "Invalid CelebrationRank")]
    public void Invalid_regional_operations_report_the_proper_source(string row, string reason)
    {
        using var general = new StringReader("01-01 | Memorial | White | Example");
        using var proper = new StringReader($"# Comment\n{row}");

        var error = Assert.Throws<InvalidOperationException>(() => GeneralRomanCalendarData.Load(general, "General.txt", proper, "Proper.txt"));

        Assert.Contains("'Proper.txt' at line 2:", error.Message);
        Assert.Contains(reason, error.Message);
    }

    [Theory]
    [InlineData("-01-01 | Example\n-01-01 | Example")]
    [InlineData("01-01 | Feast | White | Example\n01-01 | Memorial | White | Example")]
    [InlineData("-01-01 | Example\n01-01 | Feast | White | Example")]
    [InlineData("01-01 | Feast | White | Example\n-01-01 | Example")]
    public void Duplicate_or_conflicting_regional_operations_are_rejected(string rows)
    {
        using var general = new StringReader("01-01 | Memorial | White | Example");
        using var proper = new StringReader(rows);

        var error = Assert.Throws<InvalidOperationException>(() => GeneralRomanCalendarData.Load(general, "General.txt", proper, "Proper.txt"));

        Assert.Contains("'Proper.txt' at line 2:", error.Message);
        Assert.Contains("Duplicate or conflicting operation for 'Example' on 01-01", error.Message);
    }

    [Theory]
    [InlineData("", "General summary")]
    [InlineData("Regional summary", "Regional summary")]
    public void Regional_merge_preserves_metadata_removes_exact_targets_and_orders_additions(string summary, string expectedSummary)
    {
        using var general = new StringReader("""
            01-01 | OptionalMemorial | White | Example | profile | General summary
            01-01 | OptionalMemorial | White | Removed
            01-01 | OptionalMemorial | Red | Retained
            01-02 | Memorial | White | Unchanged
            """);
        using var proper = new StringReader($"""
            01-01 | Memorial | Red | Example | | {summary}
            -01-01 | Removed
            01-01 | OptionalMemorial | White | Added | added-profile | Added summary
            01-03 | Feast | White | New date
            """);

        var map = GeneralRomanCalendarData.Load(general, "General.txt", proper, "Proper.txt");

        Assert.Equal(3, map.Count);
        Assert.Equal(new[]
        {
            new LiturgicalCelebration("Example", CelebrationRank.Memorial, LiturgicalColor.Red) { Id = "profile", Summary = expectedSummary },
            new LiturgicalCelebration("Retained", CelebrationRank.OptionalMemorial, LiturgicalColor.Red),
            new LiturgicalCelebration("Added", CelebrationRank.OptionalMemorial, LiturgicalColor.White) { Id = "added-profile", Summary = "Added summary" },
        }, map[(1, 1)]);
        Assert.Equal(new LiturgicalCelebration("Unchanged", CelebrationRank.Memorial, LiturgicalColor.White), Assert.Single(map[(1, 2)]));
        Assert.Equal(new LiturgicalCelebration("New date", CelebrationRank.Feast, LiturgicalColor.White), Assert.Single(map[(1, 3)]));
    }

    [Theory]
    [InlineData("01-01 | Feast | White | Example | | | | FeastOfTheLord", "Calendar key")]
    [InlineData("01-01 | Feast | White | Example | | | Bad-Key | FeastOfTheLord", "Calendar key")]
    [InlineData("01-01 | Feast | White | Example | | | bad--key | FeastOfTheLord", "Calendar key")]
    [InlineData("01-01 | Feast | White | Example | | | example | 5", "Invalid LiturgicalPrecedenceClass")]
    [InlineData("01-01 | Feast | White | Example | | | example | Unknown", "Invalid LiturgicalPrecedenceClass")]
    [InlineData("01-01 | Memorial | White | Example | | | example | FeastOfTheLord", "incompatible with rank")]
    [InlineData("01-01 | Feast | White | Example | | | example | OptionalMemorial", "incompatible with rank")]
    public void Invalid_rule_metadata_reports_source_and_line(string row, string reason)
    {
        using var reader = new StringReader($"# Comment\n{row}");

        var error = Assert.Throws<InvalidOperationException>(() => GeneralRomanCalendarData.Load(reader, "General.txt"));

        Assert.Contains("'General.txt' at line 2:", error.Message);
        Assert.Contains(reason, error.Message);
    }

    [Fact]
    public void Extended_rows_preserve_all_metadata_and_regional_overrides_inherit_identity()
    {
        using var general = new StringReader("01-01 | Feast | White | Example | profile | Description | example | FeastOfTheLord");
        using var proper = new StringReader("01-01 | Feast | Red | Example");

        var map = GeneralRomanCalendarData.Load(general, "General.txt", proper, "Proper.txt");

        Assert.Equal(new LiturgicalCelebration("Example", CelebrationRank.Feast, LiturgicalColor.Red)
        {
            Id = "profile", Summary = "Description", CalendarKey = "example", Precedence = LiturgicalPrecedenceClass.FeastOfTheLord,
        }, Assert.Single(map[(1, 1)]));
    }

    [Fact]
    public void Rank_changes_do_not_inherit_an_incompatible_precedence_class()
    {
        using var general = new StringReader("01-01 | Feast | White | Example | | | example | FeastOfTheLord");
        using var proper = new StringReader("01-01 | Memorial | White | Example");

        var map = GeneralRomanCalendarData.Load(general, "General.txt", proper, "Proper.txt");
        var actual = Assert.Single(map[(1, 1)]);

        Assert.Equal("example", actual.CalendarKey);
        Assert.Null(actual.Precedence);
        Assert.Equal(LiturgicalPrecedenceClass.GeneralMemorial, LiturgicalPrecedence.ForCelebration(actual));
    }

    [Fact]
    public void Duplicate_authored_keys_are_rejected_even_on_different_dates()
    {
        using var reader = new StringReader("01-01 | Feast | White | First | | | example | FeastOfTheLord\n01-02 | Feast | White | Second | | | example | GeneralFeast");

        var error = Assert.Throws<InvalidOperationException>(() => GeneralRomanCalendarData.Load(reader, "General.txt"));

        Assert.Contains("'General.txt' at line 2:", error.Message);
        Assert.Contains("Duplicate calendar key 'example'", error.Message);
    }

    [Theory]
    [InlineData("01-01 | Feast | White | Example | | | changed | FeastOfTheLord", "changes the stable identity")]
    [InlineData("01-02 | Feast | White | Different name | | | example | GeneralFeast", "duplicate calendar key")]
    public void Regional_merging_rejects_rewritten_or_duplicated_keys(string row, string reason)
    {
        using var general = new StringReader("01-01 | Feast | White | Example | | | example | FeastOfTheLord");
        using var proper = new StringReader(row);

        var error = Assert.Throws<InvalidOperationException>(() => GeneralRomanCalendarData.Load(general, "General.txt", proper, "Proper.txt"));

        Assert.Contains("Proper.txt", error.Message);
        Assert.Contains(reason, error.Message);
    }
}
