using Respondeo.LiturgicalCalendar;
using Respondeo.LiturgyOfTheHours;

namespace Respondeo.UnitTests.LiturgyOfTheHours;

public class TemporalPsalmodyDataTests
{
    [Theory]
    [InlineData(-3, Hour.EveningPrayer, "Psalm 72:1-11", "Psalm 72:12-19", "Revelation 11:17-18; 12:10b-12a")]
    [InlineData(-2, Hour.MorningPrayer, "Psalm 51", "Habakkuk 3:2-4, 13a, 15-19", "Psalm 147:12-20")]
    [InlineData(-2, Hour.EveningPrayer, "Psalm 116:10-19", "Psalm 143:1-11", "Philippians 2:6-11")]
    [InlineData(-1, Hour.MorningPrayer, "Psalm 64", "Isaiah 38:10-14, 17-20", "Psalm 150")]
    [InlineData(-1, Hour.EveningPrayer, "Psalm 116:10-19", "Psalm 143:1-11", "Philippians 2:6-11")]
    [InlineData(0, Hour.MorningPrayer, "Psalm 63:2-9", "Daniel 3:57-88, 56", "Psalm 149")]
    [InlineData(0, Hour.EveningPrayer, "Psalm 110:1-5, 7", "Psalm 114", "Revelation 19:1-7")]
    public void Embedded_assignments_preserve_composed_psalmody(int offset, Hour hour, string first, string second, string third)
    {
        foreach (var region in Enum.GetValues<CalendarRegion>())
        {
            var calendar = LiturgicalCalendars.Create(new(region));
            var office = new OrdinaryFormOffice(calendar, new NoTextProvider());
            foreach (var year in new[] { 2027, 2028, 2038 })
            {
                var result = office.Compose(LiturgicalDates.Easter(year).AddDays(offset), hour, preferOfficialText: false);
                Assert.Equal(new[] { first, second, third }, result.Steps
                    .Where(step => step.Kind is OfficeStepKind.Psalm or OfficeStepKind.Canticle)
                    .Select(step => step.Reference!.Citation));
            }
        }
    }

    [Theory]
    [InlineData(-3)]
    [InlineData(-2)]
    [InlineData(-1)]
    [InlineData(0)]
    public void Missing_offset_is_rejected(int offset)
    {
        var entries = OfficeData.TemporalPsalmody.Triduum.Where(pair => pair.Key != offset).ToDictionary();
        var error = Assert.Throws<InvalidOperationException>(() =>
            (OfficeData.TemporalPsalmody with { Triduum = entries }).Validate("temporal.json"));

        Assert.Contains("'temporal.json'", error.Message);
        Assert.Contains("exactly the Easter offsets", error.Message);
    }

    [Fact]
    public void Unexpected_offset_is_rejected()
    {
        var entries = OfficeData.TemporalPsalmody.Triduum.ToDictionary();
        entries.Add(1, entries[0]);

        var error = Assert.Throws<InvalidOperationException>(() =>
            (OfficeData.TemporalPsalmody with { Triduum = entries }).Validate("temporal.json"));

        Assert.Contains("exactly the Easter offsets", error.Message);
    }

    [Fact]
    public void Holy_Thursday_cannot_override_the_morning_psalter()
    {
        var entries = OfficeData.TemporalPsalmody.Triduum.ToDictionary();
        entries[-3] = entries[-3] with { Morning = entries[0].Morning };

        var error = Assert.Throws<InvalidOperationException>(() =>
            (OfficeData.TemporalPsalmody with { Triduum = entries }).Validate("temporal.json"));

        Assert.Contains("Holy Thursday morning must retain", error.Message);
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("short")]
    [InlineData("blank")]
    [InlineData("misplaced-canticle")]
    public void Malformed_assignment_has_resource_offset_and_hour_context(string problem)
    {
        var entries = OfficeData.TemporalPsalmody.Triduum.ToDictionary();
        entries[-2] = entries[-2] with
        {
            Evening = problem switch
            {
                "missing" => null,
                "short" => ["Psalm 51"],
                "blank" => ["Psalm 51", " ", "Philippians 2:6-11"],
                _ => ["Philippians 2:6-11", "Psalm 51", "Psalm 150"],
            },
        };

        var error = Assert.Throws<InvalidOperationException>(() =>
            (OfficeData.TemporalPsalmody with { Triduum = entries }).Validate("temporal.json"));

        Assert.Contains("'temporal.json'", error.Message);
        Assert.Contains("offset -2 evening", error.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Psalm 51")]
    public void Lenten_Sunday_requires_a_canticle(string citation)
    {
        var error = Assert.Throws<InvalidOperationException>(() =>
            (OfficeData.TemporalPsalmody with { LentenSundayEveningCanticle = citation }).Validate("temporal.json"));

        Assert.Contains("Lenten Sunday Evening Prayer requires a canticle", error.Message);
    }

    [Fact]
    public void Validation_copies_assignments_to_read_only_collections()
    {
        var morning = new[] { "Psalm 51", "Habakkuk 3:2-4, 13a, 15-19", "Psalm 147:12-20" };
        var entries = OfficeData.TemporalPsalmody.Triduum.ToDictionary();
        entries[-2] = entries[-2] with { Morning = morning };
        var validated = (OfficeData.TemporalPsalmody with { Triduum = entries }).Validate("temporal.json");

        morning[0] = "Changed";
        entries.Clear();

        Assert.Equal("Psalm 51", validated.Triduum[-2].Morning![0]);
        Assert.Equal(4, validated.Triduum.Count);
        var frozenMorning = Assert.IsAssignableFrom<IList<string>>(validated.Triduum[-2].Morning);
        Assert.Throws<NotSupportedException>(() => frozenMorning.Clear());
    }

    private sealed class NoTextProvider : IHoursTextProvider
    {
        public bool IsSample => false;
        public string Acknowledgment => string.Empty;
        public LicensedText? Find(IEnumerable<string> keys) => null;
    }
}
