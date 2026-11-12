using Respondeo.LiturgicalCalendar;
using Respondeo.LiturgyOfTheHours;

namespace Respondeo.UnitTests.LiturgyOfTheHours;

public class CelebrationContentBindingTests
{
    [Fact]
    public void Authored_bindings_preserve_every_migrated_calendars_existing_content_ID()
    {
        var observed = new HashSet<string>();
        foreach (var profile in new[] { "general", "us", "us-thursday" })
        {
            var calendar = LiturgicalCalendars.Create(PreferredLiturgicalCalendar.Deserialize(profile));
            for (var date = new DateOnly(2027, 1, 1); date.Year <= 2040; date = date.AddDays(1))
            {
                var day = calendar.ForDate(date);
                var memorials = day.OptionalMemorials.Concat(day.MemorialCommemorations);
                var celebrations = day.Celebration is { } principal ? memorials.Prepend(principal) : memorials;
                foreach (var celebration in celebrations.Where(celebration => celebration.CalendarKey is not null))
                {
                    var key = celebration.CalendarKey!;
                    observed.Add(key);
                    Assert.Matches("^[a-z0-9]+(-[a-z0-9]+)*$", key);
                    Assert.Equal(OrdinaryFormOffice.CelebrationId(celebration.Name), OrdinaryFormOffice.CelebrationId(celebration));
                    Assert.Equal(OrdinaryFormOffice.CelebrationId(celebration),
                        OrdinaryFormOffice.CelebrationId(celebration with { Name = "Renamed display label", Id = "unrelated-profile" }));
                }
            }
        }

        Assert.Equal(254, observed.Count);
        Assert.Subset(observed, OfficeData.Sanctoral.CalendarBindings.Keys.ToHashSet());
        Assert.All(OfficeData.Sanctoral.CalendarBindings, binding =>
        {
            Assert.Matches("^[a-z0-9]+(-[a-z0-9]+)*$", binding.Key);
            Assert.Matches("^[a-z0-9]+(-[a-z0-9]+)*$", binding.Value);
        });
    }

    [Theory]
    [InlineData("general")]
    [InlineData("us")]
    [InlineData("us-thursday")]
    public void Renaming_migrated_celebrations_preserves_assignments_and_ordered_provider_requests(string profile)
    {
        var calendar = LiturgicalCalendars.Create(PreferredLiturgicalCalendar.Deserialize(profile));
        var originalProvider = new RecordingProvider();
        var renamedProvider = new RecordingProvider();
        var original = new OrdinaryFormOffice(calendar, originalProvider);
        var renamed = new OrdinaryFormOffice(new RenamedCalendar(calendar), renamedProvider);

        for (var date = new DateOnly(2027, 1, 1); date.Year <= 2028; date = date.AddDays(1))
        {
            if (calendar.ForDate(date).Celebration?.CalendarKey is null)
            {
                continue;
            }

            foreach (var hour in Enum.GetValues<Hour>())
            {
                Compare(date, hour);
            }
            Compare(date.AddDays(-1), Hour.EveningPrayer);
            Compare(date.AddDays(-1), Hour.NightPrayer);
        }

        void Compare(DateOnly date, Hour hour)
        {
            originalProvider.Requests.Clear();
            renamedProvider.Requests.Clear();
            var expected = original.Compose(date, hour);
            var actual = renamed.Compose(date, hour);

            Assert.Equal(expected.IsFirstVespers, actual.IsFirstVespers);
            Assert.Equal(expected.Day.Date, actual.Day.Date);
            Assert.Equal(expected.PsalterWeek, actual.PsalterWeek);
            Assert.Equivalent(expected.Steps.Select(step => step with { Explanation = null }),
                actual.Steps.Select(step => step with { Explanation = null }), strict: true);
            Assert.Equal(originalProvider.Requests, renamedProvider.Requests);
        }
    }

    [Theory]
    [InlineData(2027, 1, 5, Hour.EveningPrayer, "the-epiphany-of-the-lord", "evening-i", "Titus 3:4-5", "1 Timothy 3:16")]
    [InlineData(2027, 8, 6, Hour.EveningPrayer, "the-transfiguration-of-the-lord", "evening", "Romans 8:16-17", "1 Timothy 3:16")]
    [InlineData(2027, 11, 9, Hour.EveningPrayer, "the-dedication-of-the-lateran-basilica", "evening", "Revelation 21:2-3, 22, 27", "Revelation 19:1-7")]
    public void Renamed_celebration_uses_existing_proper_bodies_antiphons_and_reference_bindings(
        int year, int month, int day, Hour hour, string contentId, string hourKey, string reading, string canticle)
    {
        var calendar = new RenamedCalendar(LiturgicalCalendars.Create(LiturgicalCalendarOptions.Default));
        var provider = new RecordingProvider(new()
        {
            [$"proper/{contentId}/{hourKey}/hymn"] = new("Synthetic proper hymn"),
            [$"proper/{contentId}/{hourKey}/psalm-antiphon-3"] = new("", "Synthetic proper antiphon", canticle),
        });
        var office = new OrdinaryFormOffice(calendar, provider).Compose(new(year, month, day), hour);

        Assert.Equal("Renamed display label", office.Day.Celebration!.Name);
        Assert.Equal(reading, Assert.Single(office.Steps, step => step.Kind == OfficeStepKind.Reading).Reference!.Citation);
        Assert.Equal("Synthetic proper hymn", Assert.Single(office.Steps, step => step.Kind == OfficeStepKind.Hymn).Text);
        var actualCanticle = Assert.Single(office.Steps, step => step.Kind == OfficeStepKind.Canticle);
        Assert.Equal(canticle, actualCanticle.Reference!.Citation);
        Assert.Equal("Synthetic proper antiphon", actualCanticle.Antiphon);
        Assert.True(actualCanticle.IsLicensedText);

        provider.Requests.Clear();
        var disabled = new OrdinaryFormOffice(calendar, provider).Compose(new(year, month, day), hour, preferOfficialText: false);
        Assert.Empty(provider.Requests);
        Assert.Equal(reading, Assert.Single(disabled.Steps, step => step.Kind == OfficeStepKind.Reading).Reference!.Citation);
        Assert.Equal(canticle, Assert.Single(disabled.Steps, step => step.Kind == OfficeStepKind.Canticle).Reference!.Citation);
        Assert.Null(Assert.Single(disabled.Steps, step => step.Kind == OfficeStepKind.Hymn).Text);
        Assert.Null(Assert.Single(disabled.Steps, step => step.Kind == OfficeStepKind.Canticle).Antiphon);
    }

    [Fact]
    public void Unmapped_stable_keys_are_content_IDs_and_never_borrow_a_matching_display_names_proper()
    {
        var misleading = new LiturgicalCelebration("The Epiphany of the Lord", CelebrationRank.Feast, LiturgicalColor.White)
        {
            CalendarKey = "a-distinct-celebration", Id = "the-epiphany-of-the-lord",
        };
        Assert.Equal("a-distinct-celebration", OrdinaryFormOffice.CelebrationId(misleading));
        Assert.Null(Sanctoral.Celebration(OrdinaryFormOffice.CelebrationId(misleading)));
        Assert.Equal("the-epiphany-of-the-lord", OrdinaryFormOffice.CelebrationId(misleading with { CalendarKey = null }));
    }

    [Fact]
    public void Provider_only_Common_metadata_uses_the_stable_ID_without_borrowing_display_name_metadata()
    {
        var celebration = new LiturgicalCelebration("The Epiphany of the Lord", CelebrationRank.Memorial, LiturgicalColor.White)
        {
            CalendarKey = "provider-only-memorial",
        };
        var calendar = new FixedCalendar(new(new(2027, 7, 13), LiturgicalSeason.OrdinaryTime, LiturgicalColor.White, celebration));
        var provider = new RecordingProvider(new()
        {
            ["common/test-common/morning/hymn"] = new("Synthetic Common hymn"),
            ["common/test-common/morning/psalm-antiphon-1"] = new("", "Wrong Common psalm antiphon"),
        }, new() { ["provider-only-memorial"] = new("test-common") });
        var office = new OrdinaryFormOffice(calendar, provider).Compose(new(2027, 7, 13), Hour.MorningPrayer);

        Assert.Contains("celebration:provider-only-memorial", provider.Requests);
        Assert.Equal("Synthetic Common hymn", Assert.Single(office.Steps, step => step.Kind == OfficeStepKind.Hymn).Text);
        Assert.NotEqual("Isaiah 52:7-10", Assert.Single(office.Steps, step => step.Kind == OfficeStepKind.Reading).Reference!.Citation);
        Assert.All(office.Steps.Where(step => step.Kind is OfficeStepKind.Psalm or OfficeStepKind.Canticle), step => Assert.Null(step.Antiphon));
    }

    [Fact]
    public void Direct_content_identity_keeps_memorial_proper_Morning_psalmody_and_reading()
    {
        var celebration = new LiturgicalCelebration("Renamed memorial", CelebrationRank.Memorial, LiturgicalColor.White)
        {
            CalendarKey = "saint-martin-of-tours-bishop",
        };
        var calendar = new FixedCalendar(new(new(2026, 11, 11), LiturgicalSeason.OrdinaryTime, LiturgicalColor.White, celebration));
        var office = new OrdinaryFormOffice(calendar, new RecordingProvider()).Compose(new(2026, 11, 11), Hour.MorningPrayer);

        Assert.Equal(new[] { "Psalm 63:2-9", "Daniel 3:57-88, 56", "Psalm 149" },
            office.Steps.Where(step => step.Kind is OfficeStepKind.Psalm or OfficeStepKind.Canticle).Select(step => step.Reference!.Citation));
        Assert.Equal("Hebrews 13:7-9a", Assert.Single(office.Steps, step => step.Kind == OfficeStepKind.Reading).Reference!.Citation);
    }

    [Theory]
    [InlineData(2028, 3, 20, "saint-joseph", "saint-joseph-spouse-of-the-blessed-virgin-mary", "2 Samuel 7:28-29")]
    [InlineData(2027, 4, 5, "annunciation", "the-annunciation-of-the-lord", "Isaiah 11:1-2")]
    [InlineData(2022, 6, 23, "nativity-of-saint-john-the-baptist", "the-nativity-of-saint-john-the-baptist", "Malachi 3:23-24")]
    [InlineData(2030, 12, 9, "immaculate-conception", "the-immaculate-conception-of-the-blessed-virgin-mary", "Romans 5:20b-21")]
    public void Transferred_solemnities_keep_Hours_bindings_independent_of_the_display_name(
        int year, int month, int day, string key, string contentId, string reading)
    {
        foreach (var profile in new[] { "general", "us", "us-thursday" })
        {
            var calendar = new RenamedCalendar(LiturgicalCalendars.Create(PreferredLiturgicalCalendar.Deserialize(profile)));
            var provider = new RecordingProvider(new() { [$"proper/{contentId}/morning/hymn"] = new("Synthetic transferred hymn") });
            var office = new OrdinaryFormOffice(calendar, provider).Compose(new(year, month, day), Hour.MorningPrayer);

            Assert.Equal(key, office.Day.Celebration!.CalendarKey);
            Assert.Equal("Renamed display label", office.Day.Celebration.Name);
            Assert.Equal(reading, Assert.Single(office.Steps, step => step.Kind == OfficeStepKind.Reading).Reference!.Citation);
            Assert.Equal("Synthetic transferred hymn", Assert.Single(office.Steps, step => step.Kind == OfficeStepKind.Hymn).Text);
        }
    }

    [Theory]
    [InlineData(1, 1, "mary-the-holy-mother-of-god", "Micah 5:2-4a")]
    [InlineData(8, 15, "the-assumption-of-the-blessed-virgin-mary", "Isaiah 61:10")]
    [InlineData(11, 1, "all-saints", "Ephesians 1:17-18")]
    public void Remaining_fixed_solemnities_use_direct_Hours_IDs_after_renaming(int month, int day, string key, string reading)
    {
        foreach (var profile in new[] { "general", "us", "us-thursday" })
        {
            var calendar = new RenamedCalendar(LiturgicalCalendars.Create(PreferredLiturgicalCalendar.Deserialize(profile)));
            var provider = new RecordingProvider(new() { [$"proper/{key}/morning/hymn"] = new("Synthetic fixed hymn") });
            var office = new OrdinaryFormOffice(calendar, provider).Compose(new(2027, month, day), Hour.MorningPrayer);

            Assert.Equal(key, office.Day.Celebration!.CalendarKey);
            Assert.Equal("Renamed display label", office.Day.Celebration.Name);
            Assert.Equal(reading, Assert.Single(office.Steps, step => step.Kind == OfficeStepKind.Reading).Reference!.Citation);
            Assert.Equal("Synthetic fixed hymn", Assert.Single(office.Steps, step => step.Kind == OfficeStepKind.Hymn).Text);
            Assert.DoesNotContain(key, OfficeData.Sanctoral.CalendarBindings.Keys);
        }
    }

    private sealed class RenamedCalendar(ILiturgicalCalendar inner) : ILiturgicalCalendar
    {
        public LiturgicalCalendarOptions Options => inner.Options;
        public LiturgicalWeek ForWeek(DateOnly date) => inner.ForWeek(date);
        public LiturgicalDay ForDate(DateOnly date)
        {
            var day = inner.ForDate(date);
            return day.Celebration is { CalendarKey: not null } celebration
                ? day with { Celebration = celebration with { Name = "Renamed display label", Id = "unrelated-profile" } }
                : day;
        }
    }

    private sealed class FixedCalendar(LiturgicalDay day) : ILiturgicalCalendar
    {
        public LiturgicalDay ForDate(DateOnly date) => day with { Date = date };
    }

    private sealed class RecordingProvider(
        Dictionary<string, LicensedText>? entries = null, Dictionary<string, HoursCelebration>? celebrations = null) : IHoursTextProvider
    {
        public bool IsSample => false;
        public string Acknowledgment => "Synthetic test acknowledgment";
        public List<string> Requests { get; } = [];
        public LicensedText? Find(IEnumerable<string> keys)
        {
            var ordered = keys.ToArray();
            Requests.Add(string.Join('|', ordered));
            return ordered.Select(key => entries?.GetValueOrDefault(key)).FirstOrDefault(text => text is not null);
        }
        public HoursCelebration? FindCelebration(string id)
        {
            Requests.Add($"celebration:{id}");
            return celebrations?.GetValueOrDefault(id);
        }
    }
}
