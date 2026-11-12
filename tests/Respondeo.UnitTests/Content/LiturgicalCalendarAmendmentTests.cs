using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Respondeo.LiturgicalCalendar;
using Respondeo.LiturgyOfTheHours;

namespace Respondeo.UnitTests.Content;

public class LiturgicalCalendarAmendmentTests
{
    [Fact]
    public void Amendment_evidence_retains_sources_and_forward_only_scope()
    {
        using var document = ReadRegister();
        var root = document.RootElement;
        Assert.Equal(1, root.GetProperty("schemaVersion").GetInt32());
        Assert.Equal("reviewed-nominal-assignments-reconciled", root.GetProperty("status").GetString());
        Assert.Equal("current-rules-forward", Text(root, "applicationMode"));
        Assert.Equal("2026-11-11", Text(root, "forwardFrom"));
        Assert.Equal(2026, root.GetProperty("reviewYear").GetInt32());
        var reviewedOn = Date(root.GetProperty("reviewedOn"));
        var sources = root.GetProperty("sources").EnumerateArray().ToArray();
        Assert.Equal(9, sources.Length);
        Assert.Equal(sources.Length, sources.Select(source => Text(source, "id")).Distinct(StringComparer.Ordinal).Count());
        foreach (var source in sources)
        {
            var url = new Uri(Text(source, "url"));
            Assert.Equal("https", url.Scheme);
            Assert.Contains(url.Host, new[] { "www.vatican.va", "press.vatican.va" });
            Assert.Matches("^[0-9a-f]{64}$", Text(source, "sha256"));
            Assert.True(Date(source.GetProperty("decreeDate")) <= reviewedOn);
            if (source.GetProperty("publishedOn").ValueKind != JsonValueKind.Null)
            {
                var publishedOn = Date(source.GetProperty("publishedOn"));
                Assert.InRange(publishedOn, Date(source.GetProperty("decreeDate")), reviewedOn);
            }

            Assert.False(string.IsNullOrWhiteSpace(Text(source, "locator")));
            Assert.EndsWith("not-full-proper", Text(source, "reviewStatus"));
        }

        var assignments = root.GetProperty("assignments").EnumerateArray().ToArray();
        Assert.Equal(11, assignments.Length);
        Assert.Equal(assignments.Length, assignments.Select(assignment => Text(assignment, "id")).Distinct(StringComparer.Ordinal).Count());
        foreach (var assignment in assignments)
        {
            Assert.Single(sources, source => Text(source, "id") == Text(assignment, "source"));
            Assert.False(string.IsNullOrWhiteSpace(Text(assignment, "name")));
            Assert.Equal(JsonValueKind.Null, assignment.GetProperty("effectiveFrom").ValueKind);
            Assert.Contains(Text(assignment, "rank"), new[] { "Feast", "Memorial", "OptionalMemorial" });
            var fixedDate = assignment.GetProperty("monthDay");
            var offset = assignment.GetProperty("easterOffset");
            Assert.NotEqual(fixedDate.ValueKind == JsonValueKind.Null, offset.ValueKind == JsonValueKind.Null);
            if (fixedDate.ValueKind != JsonValueKind.Null)
            {
                _ = DateOnly.ParseExact($"2000-{fixedDate.GetString()}", "yyyy-MM-dd", CultureInfo.InvariantCulture);
            }
            else
            {
                Assert.Equal("mary-mother-of-the-church", Text(assignment, "id"));
                Assert.Equal(50, offset.GetInt32());
            }
        }

        Assert.Empty(root.GetProperty("openCalendarDiscrepancies").EnumerateArray());
        Assert.False(root.GetProperty("implementation").GetProperty("fullHoursReviewComplete").GetBoolean());
    }

    [Theory]
    [InlineData(CalendarRegion.GeneralRoman)]
    [InlineData(CalendarRegion.UnitedStates)]
    public void Reviewed_nominal_assignments_match_both_current_catalogs(CalendarRegion region)
    {
        using var document = ReadRegister();
        foreach (var assignment in document.RootElement.GetProperty("assignments").EnumerateArray())
        {
            var expectedName = assignment.TryGetProperty("repositoryName", out var alias) ? alias.GetString()! : Text(assignment, "name");
            IReadOnlyList<LiturgicalCelebration> candidates;
            if (assignment.GetProperty("monthDay").ValueKind != JsonValueKind.Null)
            {
                var date = DateOnly.ParseExact($"2026-{Text(assignment, "monthDay")}", "yyyy-MM-dd", CultureInfo.InvariantCulture);
                candidates = GeneralRomanCalendarData.ForMonthDay(date.Month, date.Day, region);
            }
            else
            {
                var date = LiturgicalDates.Easter(2026).AddDays(assignment.GetProperty("easterOffset").GetInt32());
                Assert.Equal(new DateOnly(2026, 5, 25), date);
                var day = new RomanCalendar(new LiturgicalCalendarOptions(region)).ForDate(date);
                Assert.NotNull(day.Celebration);
                candidates = [day.Celebration];
            }

            var actual = Assert.Single(candidates, candidate => candidate.Name == expectedName);
            Assert.Equal(Text(assignment, "rank"), actual.Rank.ToString());
        }
    }

    [Theory]
    [InlineData("teresa-of-calcutta", "2025-09-05", "virgins", "holy-women:works-of-mercy")]
    [InlineData("john-henry-newman", "2026-10-09", "pastors:priests", "doctors")]
    public void Recent_decrees_keep_Common_alternatives_and_publication_distinct_from_effective_dates(
        string id, string firstAnnualDate, string firstCommon, string secondCommon)
    {
        using var document = ReadRegister();
        var assignment = document.RootElement.GetProperty("assignments").EnumerateArray().Single(row => Text(row, "id") == id);
        var source = document.RootElement.GetProperty("sources").EnumerateArray().Single(row => Text(row, "id") == Text(assignment, "source"));
        Assert.Equal(firstAnnualDate, Text(assignment, "firstAnnualDateAfterPublication"));
        Assert.Equal(new[] { firstCommon, secondCommon }, assignment.GetProperty("hoursCommonChoices").EnumerateArray().Select(value => value.GetString()));
        Assert.Equal(JsonValueKind.Null, assignment.GetProperty("effectiveFrom").ValueKind);
        var publishedOn = Date(source.GetProperty("publishedOn"));
        var annualDate = Date(assignment.GetProperty("firstAnnualDateAfterPublication"));
        Assert.True(publishedOn <= annualDate);
        Assert.True(annualDate.AddYears(-1) < publishedOn);
        Assert.Equal(Text(assignment, "monthDay"), annualDate.ToString("MM-dd", CultureInfo.InvariantCulture));
    }

    [Fact]
    public void Amendment_register_is_linked_from_the_authority_baseline()
    {
        using var baseline = ReadRegister("liturgical-authority-baseline.json");
        Assert.Equal("liturgical-calendar-amendments.json", Text(baseline.RootElement, "amendmentRegister"));
        Assert.Contains(baseline.RootElement.GetProperty("openIssues").EnumerateArray(), issue => Text(issue, "id") == "AUTH-02" && Text(issue, "status") == "open");
    }

    [Theory]
    [InlineData(CalendarRegion.GeneralRoman, false)]
    [InlineData(CalendarRegion.UnitedStates, false)]
    [InlineData(CalendarRegion.UnitedStates, true)]
    public void Universal_additions_remain_optional_and_obey_future_Sunday_suppression(CalendarRegion region, bool ascensionOnThursday)
    {
        var calendar = new RomanCalendar(new LiturgicalCalendarOptions(region, AscensionOnThursday: ascensionOnThursday));
        foreach (var year in Enumerable.Range(2027, 14))
        {
            Check(new DateOnly(year, 9, 5), "Saint Teresa of Calcutta, Virgin");
            Check(new DateOnly(year, 10, 9), "Saint John Henry Newman, Priest and Doctor of the Church");
        }

        void Check(DateOnly date, string name)
        {
            var nominal = Assert.Single(GeneralRomanCalendarData.ForMonthDay(date.Month, date.Day, region), candidate => candidate.Name == name);
            Assert.Equal(CelebrationRank.OptionalMemorial, nominal.Rank);
            Assert.Equal(LiturgicalColor.White, nominal.Color);
            var day = calendar.ForDate(date);
            Assert.Null(day.Celebration);
            if (date.DayOfWeek == DayOfWeek.Sunday)
            {
                Assert.Empty(day.OptionalMemorials);
            }
            else
            {
                var actual = Assert.Single(day.OptionalMemorials, candidate => candidate.Name == name);
                Assert.Equal(nominal, actual);
            }

            Assert.DoesNotContain(day.OptionalMemorials, candidate => candidate.Name == "Saint John Henry Newman, Cardinal");
        }
    }

    [Theory]
    [InlineData("Saint Teresa of Calcutta, Virgin", "virgins", "Song of Songs 8:7", "1 Corinthians 7:32b, 34a")]
    [InlineData("Saint John Henry Newman, Priest and Doctor of the Church", "pastors", "Hebrews 13:7-9a", "1 Peter 5:1-4")]
    public void Universal_additions_keep_Hours_bindings_without_forcing_Common_psalmody(string name, string common, string morning, string evening)
    {
        Assert.Equal(common, Sanctoral.Celebration(name)?.Common);
        Assert.Equal(morning, Sanctoral.ShortReading(Hour.MorningPrayer, name));
        Assert.Equal(evening, Sanctoral.ShortReading(Hour.EveningPrayer, name));
        Assert.Null(Sanctoral.MorningPsalmody(name));
        Assert.Null(Sanctoral.EveningPsalmody(name, includeCommon: false));
        Assert.DoesNotContain("saint-john-henry-newman-cardinal", OfficeData.Sanctoral.Celebrations.Keys);
    }

    private static string Text(JsonElement element, string property) => element.GetProperty(property).GetString()!;

    private static DateOnly Date(JsonElement value) => DateOnly.ParseExact(value.GetString()!, "yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static JsonDocument ReadRegister(string name = "liturgical-calendar-amendments.json", [CallerFilePath] string file = "")
    {
        var root = Directory.GetParent(file)!.Parent!.Parent!.Parent!.FullName;
        return JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "docs", name)));
    }
}
