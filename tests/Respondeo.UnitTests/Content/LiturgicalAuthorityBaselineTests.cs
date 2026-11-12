using System.Globalization;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Respondeo.LiturgicalCalendar;

namespace Respondeo.UnitTests.Content;

public class LiturgicalAuthorityBaselineTests
{
    [Theory]
    [InlineData("GeneralRomanCalendar.txt")]
    [InlineData("UnitedStatesProper.txt")]
    public void Catalog_snapshot_matches_current_resource_without_claiming_authority(string resource)
    {
        using var baseline = ReadBaseline();
        var snapshot = baseline.RootElement.GetProperty("catalogSnapshots").EnumerateArray()
            .Single(row => row.GetProperty("resource").GetString() == resource);
        var rows = ReadRows(resource);
        var normalized = string.Join('\n', rows.Select(row => string.Join('|', row)));
        var digest = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(normalized)));

        Assert.Equal("repository-consistency-only", snapshot.GetProperty("evidenceKind").GetString());
        Assert.Equal("pending", snapshot.GetProperty("assignmentReview").GetString());
        Assert.Equal(snapshot.GetProperty("rows").GetInt32(), rows.Length);
        Assert.Equal(snapshot.GetProperty("normalizedSha256").GetString(), digest);

        if (resource == "GeneralRomanCalendar.txt")
        {
            var monthly = Enumerable.Range(1, 12).Select(month => rows.Count(row => MonthDay(row[0]).Month == month));
            Assert.Equal(snapshot.GetProperty("monthlyRows").EnumerateArray().Select(value => value.GetInt32()), monthly);
            var ranks = rows.GroupBy(row => row[1]).ToDictionary(group => group.Key, group => group.Count());
            Assert.Equal(snapshot.GetProperty("rankRows").EnumerateObject().Count(), ranks.Count);
            foreach (var rank in snapshot.GetProperty("rankRows").EnumerateObject())
            {
                Assert.Equal(rank.Value.GetInt32(), ranks[rank.Name]);
            }
        }
    }

    [Fact]
    public void Every_US_operation_has_a_unique_identity_and_every_removal_matches_one_baseline_entry()
    {
        using var baseline = ReadBaseline();
        var snapshot = baseline.RootElement.GetProperty("catalogSnapshots")[1];
        var general = ReadRows("GeneralRomanCalendar.txt");
        var overlay = ReadRows("UnitedStatesProper.txt");
        var identities = new HashSet<(string Date, string Name)>();
        var additions = 0;
        var replacements = 0;
        var removals = 0;

        foreach (var row in overlay)
        {
            var removal = row[0].StartsWith('-');
            var date = removal ? row[0][1..] : row[0];
            var name = removal ? row[1] : row[3];
            _ = MonthDay(date);
            Assert.True(identities.Add((date, name)), $"Duplicate US overlay operation: {date} | {name}");
            var matches = general.Where(candidate => candidate[0] == date && candidate[3] == name).ToArray();
            if (removal)
            {
                Assert.Single(matches);
                removals++;
            }
            else if (matches.Length > 0)
            {
                Assert.Single(matches);
                replacements++;
            }
            else
            {
                additions++;
            }
        }

        Assert.Equal(snapshot.GetProperty("additions").GetInt32(), additions);
        Assert.Equal(snapshot.GetProperty("replacements").GetInt32(), replacements);
        Assert.Equal(snapshot.GetProperty("removals").GetInt32(), removals);
        Assert.Equal(overlay.Length, additions + replacements + removals);
    }

    [Theory]
    [InlineData(CalendarRegion.GeneralRoman)]
    [InlineData(CalendarRegion.UnitedStates)]
    public void Embedded_lookup_preserves_every_catalog_entry_and_its_metadata(CalendarRegion region)
    {
        var expected = ReadRows("GeneralRomanCalendar.txt").ToDictionary(Key, Celebration);
        if (region == CalendarRegion.UnitedStates)
        {
            foreach (var row in ReadRows("UnitedStatesProper.txt"))
            {
                if (row[0].StartsWith('-'))
                {
                    Assert.True(expected.Remove((row[0][1..], row[1])), $"Unmatched US removal: {string.Join('|', row)}");
                    continue;
                }

                var key = Key(row);
                var replacement = Celebration(row);
                if (expected.TryGetValue(key, out var previous))
                {
                    replacement = replacement with { Id = previous.Id, Summary = replacement.Summary ?? previous.Summary };
                }

                expected[key] = replacement;
            }
        }

        for (var date = new DateOnly(2000, 1, 1); date.Year == 2000; date = date.AddDays(1))
        {
            var monthDay = date.ToString("MM-dd", CultureInfo.InvariantCulture);
            var rows = expected.Where(pair => pair.Key.Date == monthDay).Select(pair => pair.Value).OrderBy(value => value.Name, StringComparer.Ordinal);
            var actual = GeneralRomanCalendarData.ForMonthDay(date.Month, date.Day, region).OrderBy(value => value.Name, StringComparer.Ordinal);
            Assert.Equal(rows, actual);
        }
    }

    public static IEnumerable<object[]> CalendarFixtures()
    {
        using var baseline = ReadBaseline();
        foreach (var fixture in baseline.RootElement.GetProperty("calendarFixtures").EnumerateArray())
        {
            yield return [fixture.GetRawText()];
        }
    }

    [Fact]
    public void Calendar_matches_transcribed_annual_Psalter_week_assignments()
    {
        using var baseline = ReadBaseline();
        var fixtures = baseline.RootElement.GetProperty("weekFixtures").EnumerateArray().ToArray();
        Assert.Equal(6, fixtures.Length);
        Assert.Equal(new[] { "2026-11-29", "2027-02-10", "2027-02-14", "2027-05-17", "2028-06-05", "2028-06-12" },
            fixtures.Select(row => row.GetProperty("date").GetString()));
        foreach (var fixture in fixtures)
        {
            Assert.Equal("annual-calendar-explicit-psalter-annotation", fixture.GetProperty("evidenceKind").GetString());
            var source = fixture.GetProperty("source").GetString();
            Assert.Contains(source, new[] { "S6", "S7" });
            Assert.Contains(baseline.RootElement.GetProperty("sources").EnumerateArray(), row => row.GetProperty("id").GetString() == source);
            Assert.False(string.IsNullOrWhiteSpace(fixture.GetProperty("locator").GetString()));
            var date = DateOnly.ParseExact(fixture.GetProperty("date").GetString()!, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            foreach (var profile in new[] { "us", "us-thursday" })
            {
                var actual = LiturgicalCalendars.Create(PreferredLiturgicalCalendar.Deserialize(profile)).ForWeek(date);
                var temporal = fixture.GetProperty("temporalWeek");
                Assert.Equal(temporal.ValueKind == JsonValueKind.Null ? (int?)null : temporal.GetInt32(), actual.TemporalWeek);
                Assert.Equal(fixture.GetProperty("psalterWeek").GetInt32(), actual.PsalterWeek);
            }
        }
    }

    public static IEnumerable<object[]> EveningFixtures()
    {
        using var baseline = ReadBaseline();
        foreach (var fixture in baseline.RootElement.GetProperty("eveningFixtures").EnumerateArray())
        {
            yield return [fixture.GetRawText()];
        }
    }

    [Theory]
    [MemberData(nameof(EveningFixtures))]
    public void Calendar_matches_rule_derived_evening_cases(string fixtureJson)
    {
        using var document = JsonDocument.Parse(fixtureJson);
        using var baseline = ReadBaseline();
        var fixture = document.RootElement;
        Assert.Equal("rule-derived-not-ordo-transcribed", fixture.GetProperty("evidenceKind").GetString());
        Assert.False(string.IsNullOrWhiteSpace(fixture.GetProperty("locator").GetString()));
        var sources = fixture.GetProperty("sources").EnumerateArray().Select(value => value.GetString()).ToArray();
        Assert.Contains("S1", sources);
        Assert.Contains("S2", sources);
        Assert.All(sources, id => Assert.Contains(baseline.RootElement.GetProperty("sources").EnumerateArray(),
            source => source.GetProperty("id").GetString() == id));
        var profile = fixture.GetProperty("profile").GetString()!;
        var calendar = LiturgicalCalendars.Create(PreferredLiturgicalCalendar.Deserialize(profile));
        var date = DateOnly.ParseExact(fixture.GetProperty("date").GetString()!, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        var evening = calendar.ForEvening(date);

        Assert.Equal(date, evening.Today.Date);
        Assert.Equal(fixture.GetProperty("officeDate").GetString(), evening.OfficeDay.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        Assert.Equal(fixture.GetProperty("phase").GetString(), evening.Phase.ToString());
        Assert.Equal(fixture.GetProperty("celebration").GetString(), evening.OfficeDay.Celebration?.Name);
        Assert.Equal(fixture.GetProperty("ferialName").GetString(), evening.OfficeDay.FerialName);
    }

    [Theory]
    [MemberData(nameof(CalendarFixtures))]
    public void Calendar_matches_independently_transcribed_USCCB_cases(string fixtureJson)
    {
        using var document = JsonDocument.Parse(fixtureJson);
        using var baseline = ReadBaseline();
        var fixture = document.RootElement;
        var sourceId = fixture.GetProperty("source").GetString();
        Assert.Contains(sourceId, new[] { "S5", "S6", "S7" });
        Assert.False(string.IsNullOrWhiteSpace(fixture.GetProperty("locator").GetString()));
        Assert.Contains(baseline.RootElement.GetProperty("sources").EnumerateArray(), source => source.GetProperty("id").GetString() == sourceId);
        var profile = fixture.GetProperty("profile").GetString();
        Assert.Contains(profile, new[] { "us", "us-thursday" });
        var date = DateOnly.ParseExact(fixture.GetProperty("date").GetString()!, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        var calendar = new RomanCalendar(new LiturgicalCalendarOptions(CalendarRegion.UnitedStates, AscensionOnThursday: profile == "us-thursday"));
        var actual = calendar.ForDate(date);

        Assert.Equal(date, actual.Date);
        Assert.Equal(fixture.GetProperty("celebration").GetString(), actual.Celebration?.Name);
        Assert.Equal(fixture.GetProperty("ferialName").GetString(), actual.FerialName);
        Assert.Equal(fixture.GetProperty("rank").GetString(), actual.Celebration?.Rank.ToString());
        Assert.Equal(fixture.GetProperty("color").GetString(), actual.Color.ToString());
        Assert.Equal(fixture.GetProperty("season").GetString(), actual.Season.ToString());
        Assert.Empty(actual.OptionalMemorials);
    }

    [Fact]
    public void Lauds_Invitatory_omission_keeps_the_verse_without_inferring_prior_prayer()
    {
        using var baseline = ReadBaseline();
        var rule = baseline.RootElement.GetProperty("ruleReviews").EnumerateArray().Single(row => row.GetProperty("id").GetString() == "HOUR-01");
        var review = rule.GetProperty("invitatoryOmissionReview");

        Assert.Equal("S2", review.GetProperty("authority").GetString());
        Assert.Equal("S2 nn. 34-35, PDF p. 14", Assert.Single(review.GetProperty("locators").EnumerateArray()).GetString());
        Assert.Equal("normative-rule-implemented-not-full-proper-certification", review.GetProperty("status").GetString());
        Assert.Equal("invitatory-before-lauds", review.GetProperty("scope").GetString());
        Assert.Equal("OmitPsalm", review.GetProperty("mode").GetString());
        Assert.Equal(new[] { "invitatory-psalm", "invitatory-antiphon" }, review.GetProperty("omittedElements").EnumerateArray().Select(value => value.GetString()));
        Assert.True(review.GetProperty("openingVerseRetained").GetBoolean());
        Assert.False(review.GetProperty("priorPrayerInferred").GetBoolean());
        Assert.False(review.GetProperty("defaultChanged").GetBoolean());
        Assert.Equal("pending-reconciliation", rule.GetProperty("status").GetString());
    }

    [Fact]
    public void Marian_choice_retains_the_Easter_requirement_and_existing_inventory_boundary()
    {
        using var baseline = ReadBaseline();
        var rule = baseline.RootElement.GetProperty("ruleReviews").EnumerateArray().Single(row => row.GetProperty("id").GetString() == "HOUR-05");
        var review = rule.GetProperty("marianChoiceReview");

        Assert.Equal("S2", review.GetProperty("authority").GetString());
        Assert.Equal("S2 n. 92, PDF p. 20", Assert.Single(review.GetProperty("locators").EnumerateArray()).GetString());
        Assert.Equal("normative-rule-implemented-not-full-proper-certification", review.GetProperty("status").GetString());
        Assert.Equal("night-prayer-outside-easter", review.GetProperty("scope").GetString());
        Assert.Equal("four-distinct-existing-embedded-antiphons", review.GetProperty("inventory").GetString());
        Assert.Equal("Regina Caeli", review.GetProperty("easterChoice").GetString());
        Assert.False(review.GetProperty("easterAlternativesOffered").GetBoolean());
        Assert.False(review.GetProperty("seasonalDefaultChanged").GetBoolean());
        Assert.False(review.GetProperty("invitatoryPreferenceApplied").GetBoolean());
        Assert.False(review.GetProperty("choicePersisted").GetBoolean());
        Assert.Equal("pending-reconciliation", rule.GetProperty("status").GetString());
    }

    [Fact]
    public void Weekday_Compline_psalm_option_is_normative_and_does_not_replace_the_reading_or_prayer()
    {
        using var baseline = ReadBaseline();
        var rule = baseline.RootElement.GetProperty("ruleReviews").EnumerateArray().Single(row => row.GetProperty("id").GetString() == "HOUR-05");
        var review = rule.GetProperty("weekdayPsalmSubstitutionReview");

        Assert.Equal("S2", review.GetProperty("authority").GetString());
        Assert.Equal("S2 nn. 88-90, PDF p. 19", Assert.Single(review.GetProperty("locators").EnumerateArray()).GetString());
        Assert.Equal("normative-rule-implemented-not-full-proper-certification", review.GetProperty("status").GetString());
        Assert.Equal("weekday-cycle-night-prayer", review.GetProperty("scope").GetString());
        Assert.Equal(new[] { "Weekday", "SundayI", "SundayII" }, review.GetProperty("choices").EnumerateArray().Select(value => value.GetString()));
        Assert.Equal(new[] { "psalmody", "psalmody-text-bindings" }, review.GetProperty("changedElements").EnumerateArray().Select(value => value.GetString()));
        Assert.False(review.GetProperty("readingChanged").GetBoolean());
        Assert.False(review.GetProperty("concludingPrayerChanged").GetBoolean());
        Assert.False(review.GetProperty("calendarOwnershipChanged").GetBoolean());
        Assert.False(review.GetProperty("defaultChanged").GetBoolean());
        Assert.False(review.GetProperty("choicePersisted").GetBoolean());
        Assert.Equal("pending-reconciliation", rule.GetProperty("status").GetString());
    }

    [Fact]
    public void Participation_omissions_are_normative_rules_not_secondary_digital_inferences()
    {
        using var baseline = ReadBaseline();
        var rule = baseline.RootElement.GetProperty("ruleReviews").EnumerateArray().Single(row => row.GetProperty("id").GetString() == "HOUR-05");
        var review = rule.GetProperty("participationOmissionReview");

        Assert.Equal("S2", review.GetProperty("authority").GetString());
        Assert.Equal("normative-rule-implemented-not-full-proper-certification", review.GetProperty("status").GetString());
        Assert.True(review.GetProperty("serviceDateBased").GetBoolean());
        Assert.False(review.GetProperty("attendanceInferred").GetBoolean());
        Assert.Equal(new[]
        {
            "HolyThursdayEveningMass|holy-thursday|EveningPrayer|209",
            "GoodFridayPassion|good-friday|EveningPrayer|209",
            "EasterVigil|holy-saturday|NightPrayer|211",
            "ChristmasOfficeOfReadingsVigil|december-24|NightPrayer|215",
        }, review.GetProperty("conditions").EnumerateArray().Select(row =>
            $"{row.GetProperty("participation").GetString()}|{row.GetProperty("day").GetString()}|{row.GetProperty("hour").GetString()}|{row.GetProperty("paragraph").GetInt32()}"));
        Assert.Equal("pending-reconciliation", rule.GetProperty("status").GetString());
    }

    [Fact]
    public void Compline_digital_observations_do_not_claim_approved_book_authority()
    {
        using var baseline = ReadBaseline();
        var evidence = Assert.Single(baseline.RootElement.GetProperty("supplementaryHoursEvidence").EnumerateArray(),
            row => row.GetProperty("id").GetString() == "OBS-COMPLINE-01");

        Assert.Equal("OBS-COMPLINE-01", evidence.GetProperty("id").GetString());
        Assert.Equal("secondary-digital-observation-not-approved-book-verification", evidence.GetProperty("evidenceKind").GetString());
        Assert.False(evidence.GetProperty("normativeVerification").GetBoolean());
        Assert.Equal("approved-book-check-pending", evidence.GetProperty("reviewStatus").GetString());
        Assert.Equal("POST", evidence.GetProperty("settingsMethod").GetString());
        Assert.Equal("en", evidence.GetProperty("language").GetString());
        Assert.Equal(new[]
        {
            "2027-03-28|haec-dies", "2027-04-03|haec-dies",
            "2027-04-04|in-manus-with-easter-alleluias", "2027-04-05|in-manus-with-easter-alleluias", "2027-04-06|in-manus-with-easter-alleluias",
        }, evidence.GetProperty("observations").EnumerateArray().Select(row => $"{row.GetProperty("date").GetString()}|{row.GetProperty("form").GetString()}"));
        Assert.All(evidence.GetProperty("observations").EnumerateArray(), row => Assert.Matches("^[0-9a-f]{64}$", row.GetProperty("sha256").GetString()!));
        var rule = baseline.RootElement.GetProperty("ruleReviews").EnumerateArray().Single(row => row.GetProperty("id").GetString() == "HOUR-05");
        Assert.Equal("OBS-COMPLINE-01", Assert.Single(rule.GetProperty("supplementaryEvidence").EnumerateArray()).GetString());
        Assert.Equal(new[] { "S1", "S2", "S10" }, rule.GetProperty("sources").EnumerateArray().Select(value => value.GetString()));
        Assert.Equal("pending-reconciliation", rule.GetProperty("status").GetString());

        var octave = evidence.GetProperty("octavePsalmodyObservation");
        Assert.Equal("2027-03-29", octave.GetProperty("requestedDate").GetString());
        Assert.Equal("9f69092c63d35362fa02511a84cf33b98df4c6ceb76a5f5d694d70aa39a721e8", octave.GetProperty("sha256").GetString());
        var schemas = octave.GetProperty("schemas").EnumerateArray().ToArray();
        Assert.Equal(new[] { "sunday-i", "sunday-ii" }, schemas.Select(row => row.GetProperty("form").GetString()));
        Assert.Equal(new[] { "Psalm 4", "Psalm 134" }, schemas[0].GetProperty("psalmody").EnumerateArray().Select(value => value.GetString()));
        Assert.Equal(new[] { "Psalm 91" }, schemas[1].GetProperty("psalmody").EnumerateArray().Select(value => value.GetString()));
        Assert.Equal(new[] { "Deuteronomy 6:4-7", "Revelation 22:4-5" }, schemas.Select(row => row.GetProperty("reading").GetString()));

        var choice = rule.GetProperty("octaveFormChoiceReview");
        Assert.Equal("secondary-rubric-implemented-approved-book-check-pending", choice.GetProperty("status").GetString());
        Assert.Equal("OBS-COMPLINE-01", choice.GetProperty("evidence").GetString());
        Assert.Equal("civil-easter-octave-days-1-8-night-prayer", choice.GetProperty("scope").GetString());
        Assert.Equal(new[] { "SundayI", "SundayII" }, choice.GetProperty("choices").EnumerateArray().Select(value => value.GetString()));
        Assert.Equal(new[] { "psalmody", "reading", "concluding-prayer", "psalmody-text-bindings" },
            choice.GetProperty("coordinatedElements").EnumerateArray().Select(value => value.GetString()));
        Assert.False(choice.GetProperty("calendarOwnershipChanged").GetBoolean());
        Assert.False(choice.GetProperty("defaultChanged").GetBoolean());
        Assert.False(choice.GetProperty("choicePersisted").GetBoolean());
    }

    [Fact]
    public void Holy_Thursday_observations_preserve_references_without_claiming_approved_book_authority()
    {
        using var baseline = ReadBaseline();
        var evidence = Assert.Single(baseline.RootElement.GetProperty("supplementaryHoursEvidence").EnumerateArray(),
            row => row.GetProperty("id").GetString() == "OBS-LAUDS-01");

        Assert.Equal("secondary-digital-observation-not-approved-book-verification", evidence.GetProperty("evidenceKind").GetString());
        Assert.False(evidence.GetProperty("normativeVerification").GetBoolean());
        Assert.Equal("approved-book-check-pending", evidence.GetProperty("reviewStatus").GetString());
        Assert.Equal("POST", evidence.GetProperty("settingsMethod").GetString());
        Assert.Equal("en", evidence.GetProperty("language").GetString());
        var observations = evidence.GetProperty("observations").EnumerateArray().ToArray();
        Assert.Equal(new[] { "2027-03-25", "2028-04-13" }, observations.Select(row => row.GetProperty("date").GetString()));
        foreach (var row in observations)
        {
            Assert.Equal(new[] { "Psalm 80", "Isaiah 12:1-6", "Psalm 81" }, row.GetProperty("psalmody").EnumerateArray().Select(value => value.GetString()));
            Assert.Matches("^[0-9a-f]{64}$", row.GetProperty("sha256").GetString()!);
        }

        var comparison = evidence.GetProperty("weekdayComparison");
        Assert.Equal("2027-02-25", comparison.GetProperty("date").GetString());
        Assert.True(comparison.GetProperty("differentFirstAntiphon").GetBoolean());
        Assert.Matches("^[0-9a-f]{64}$", comparison.GetProperty("sha256").GetString()!);
        var rule = baseline.RootElement.GetProperty("ruleReviews").EnumerateArray().Single(row => row.GetProperty("id").GetString() == "HOUR-02");
        Assert.Equal(new[] { "OBS-LAUDS-01", "OBS-OCTAVE-01", "OBS-LENT-01" },
            rule.GetProperty("supplementaryEvidence").EnumerateArray().Select(value => value.GetString()));
    }

    [Fact]
    public void Octave_observations_keep_exact_references_and_secondary_authority()
    {
        using var baseline = ReadBaseline();
        var evidence = Assert.Single(baseline.RootElement.GetProperty("supplementaryHoursEvidence").EnumerateArray(),
            row => row.GetProperty("id").GetString() == "OBS-OCTAVE-01");

        Assert.Equal("secondary-digital-observation-not-approved-book-verification", evidence.GetProperty("evidenceKind").GetString());
        Assert.False(evidence.GetProperty("normativeVerification").GetBoolean());
        Assert.Equal("approved-book-check-pending", evidence.GetProperty("reviewStatus").GetString());
        Assert.Equal("POST", evidence.GetProperty("settingsMethod").GetString());
        Assert.Equal("en", evidence.GetProperty("language").GetString());
        var observations = evidence.GetProperty("observations").EnumerateArray().ToArray();
        Assert.Equal(new[] { "2027-03-29", "2027-03-29", "2027-04-03", "2027-04-03", "2027-04-04", "2027-04-04" },
            observations.Select(row => row.GetProperty("date").GetString()));
        Assert.Equal(new[] { "lauds", "vespers", "lauds", "vespers", "lauds", "vespers" }, observations.Select(row => row.GetProperty("hour").GetString()));
        foreach (var row in observations)
        {
            var expected = row.GetProperty("hour").GetString() == "lauds"
                ? new[] { "Psalm 63:2-9", "Daniel 3:57-88, 56", "Psalm 149" }
                : new[] { "Psalm 110:1-5, 7", "Psalm 114", "Revelation 19:1-7" };
            Assert.Equal(expected, row.GetProperty("psalmody").EnumerateArray().Select(value => value.GetString()));
            Assert.Matches("^[0-9a-f]{64}$", row.GetProperty("sha256").GetString()!);
        }
    }

    [Fact]
    public void Lenten_observations_distinguish_First_and_Second_Vespers_without_claiming_approved_book_authority()
    {
        using var baseline = ReadBaseline();
        var evidence = Assert.Single(baseline.RootElement.GetProperty("supplementaryHoursEvidence").EnumerateArray(),
            row => row.GetProperty("id").GetString() == "OBS-LENT-01");

        Assert.Equal("secondary-digital-observation-not-approved-book-verification", evidence.GetProperty("evidenceKind").GetString());
        Assert.False(evidence.GetProperty("normativeVerification").GetBoolean());
        Assert.Equal("approved-book-check-pending", evidence.GetProperty("reviewStatus").GetString());
        Assert.Equal("POST", evidence.GetProperty("settingsMethod").GetString());
        Assert.Equal("en", evidence.GetProperty("language").GetString());
        var rows = evidence.GetProperty("observations").EnumerateArray().ToArray();
        Assert.Equal(new[] { "2027-02-14", "2027-03-20", "2027-03-21" }, rows.Select(row => row.GetProperty("date").GetString()));
        Assert.Equal(new[] { "second-vespers", "first-vespers", "second-vespers" }, rows.Select(row => row.GetProperty("phase").GetString()));
        var expected = new[]
        {
            new[] { "Psalm 110:1-5, 7", "Psalm 114", "1 Peter 2:21-24" },
            new[] { "Psalm 119:105-112", "Psalm 16", "Philippians 2:6-11" },
            new[] { "Psalm 110:1-5, 7", "Psalm 115", "1 Peter 2:21-24" },
        };
        for (var index = 0; index < rows.Length; index++)
        {
            Assert.Equal(expected[index], rows[index].GetProperty("psalmody").EnumerateArray().Select(value => value.GetString()));
            Assert.Matches("^[0-9a-f]{64}$", rows[index].GetProperty("sha256").GetString()!);
        }
    }

    [Fact]
    public void Conflicting_feast_responses_remain_rejected_and_full_First_Vespers_review_stays_pending()
    {
        using var baseline = ReadBaseline();
        var rule = baseline.RootElement.GetProperty("ruleReviews").EnumerateArray().Single(row => row.GetProperty("id").GetString() == "HOUR-03");
        Assert.Equal("pending-reconciliation", rule.GetProperty("status").GetString());
        Assert.Contains("S2 n. 137, PDF p. 26", rule.GetProperty("locators").EnumerateArray().Select(value => value.GetString()));
        var issue = baseline.RootElement.GetProperty("openIssues").EnumerateArray().Single(row => row.GetProperty("id").GetString() == "AUTH-08");
        Assert.Equal("open", issue.GetProperty("status").GetString());
        Assert.Equal("rejected-as-reference-fixtures; cause unresolved; not used to override normative rules", issue.GetProperty("authority").GetString());
        var checks = issue.GetProperty("rejectedChecks").EnumerateArray().ToArray();
        Assert.Equal(new[] { "2027-01-03", "2028-08-06" }, checks.Select(row => row.GetProperty("requestedDate").GetString()));
        Assert.Equal(new[] { "Revelation 15:3-4", "Revelation 19:1-7" }, checks.Select(row => row.GetProperty("returnedCanticle").GetString()));
        Assert.All(checks, row => Assert.Matches("^[0-9a-f]{64}$", row.GetProperty("sha256").GetString()!));
    }

    [Fact]
    public void Evidence_register_keeps_unreviewed_scope_and_sources_explicit()
    {
        using var baseline = ReadBaseline();
        var root = baseline.RootElement;
        Assert.Equal(1, root.GetProperty("schemaVersion").GetInt32());
        Assert.Equal("in-progress", root.GetProperty("status").GetString());
        var scope = root.GetProperty("scope");
        Assert.Equal("current-rules-forward", scope.GetProperty("applicationMode").GetString());
        Assert.Equal("2026-11-11", scope.GetProperty("forwardFrom").GetString());
        Assert.Equal(new[] { 2026, 2027, 2028 }, scope.GetProperty("candidateYears").EnumerateArray().Select(value => value.GetInt32()));
        Assert.Empty(scope.GetProperty("certifiedYears").EnumerateArray());
        Assert.Equal(new[] { "general", "us", "us-thursday" }, scope.GetProperty("profiles").EnumerateArray().Select(value => value.GetString()));
        Assert.Equal(new[] { "lauds", "vespers", "compline" }, scope.GetProperty("hours").EnumerateArray().Select(value => value.GetString()));
        Assert.Equal("public-domain-bibles-including-biblical-canticles", scope.GetProperty("scripture").GetString());
        Assert.Equal("labeled-placeholders", scope.GetProperty("protectedBodies").GetString());
        Assert.Equal("optional-separate-layer-rights-review-required", scope.GetProperty("latin").GetString());
        Assert.False(scope.GetProperty("historicalReconstruction").GetBoolean());
        Assert.False(scope.GetProperty("completeDiocesanPropers").GetBoolean());
        var sources = root.GetProperty("sources").EnumerateArray().ToDictionary(source => source.GetProperty("id").GetString()!);
        Assert.Equal(new[] { "S1", "S10", "S11", "S2", "S5", "S6", "S7" }, sources.Keys.Order(StringComparer.Ordinal));
        foreach (var (id, source) in sources)
        {
            Assert.False(string.IsNullOrWhiteSpace(source.GetProperty("title").GetString()));
            Assert.False(string.IsNullOrWhiteSpace(source.GetProperty("limitation").GetString()));
            if (id == "S10")
            {
                Assert.Equal("not-acquired", source.GetProperty("reviewStatus").GetString());
                Assert.False(source.TryGetProperty("sha256", out _));
                continue;
            }

            Assert.Equal("partial", source.GetProperty("reviewStatus").GetString());
            Assert.Equal("https", new Uri(source.GetProperty("url").GetString()!).Scheme);
            Assert.Matches("^[0-9a-f]{64}$", source.GetProperty("sha256").GetString()!);
            Assert.Equal(root.GetProperty("recordedOn").GetString(), source.GetProperty("retrievedOn").GetString());
        }

        var rules = root.GetProperty("ruleReviews").EnumerateArray().ToDictionary(rule => rule.GetProperty("id").GetString()!);
        var expectedRules = Enumerable.Range(1, 6).Select(number => $"CAL-{number:00}")
            .Concat(Enumerable.Range(1, 6).Select(number => $"HOUR-{number:00}"));
        Assert.Equal(expectedRules.Order(StringComparer.Ordinal), rules.Keys.Order(StringComparer.Ordinal));
        foreach (var rule in rules.Values)
        {
            Assert.Equal("pending-reconciliation", rule.GetProperty("status").GetString());
            var references = rule.GetProperty("sources").EnumerateArray().Select(value => value.GetString()!).ToArray();
            Assert.NotEmpty(references);
            Assert.All(references, reference => Assert.True(sources.ContainsKey(reference), $"Unknown source: {reference}"));
        }

        var fixtureKeys = root.GetProperty("calendarFixtures").EnumerateArray()
            .Select(fixture => $"{fixture.GetProperty("date").GetString()}|{fixture.GetProperty("profile").GetString()}");
        Assert.Equal(new[]
        {
            "2026-05-14|us", "2026-05-14|us-thursday", "2026-05-17|us", "2026-05-17|us-thursday",
            "2027-04-05|us", "2027-04-05|us-thursday", "2028-03-20|us", "2028-03-20|us-thursday",
            "2028-06-05|us", "2028-06-05|us-thursday",
        }, fixtureKeys.Order(StringComparer.Ordinal));

        var eveningKeys = root.GetProperty("eveningFixtures").EnumerateArray()
            .Select(fixture => $"{fixture.GetProperty("date").GetString()}|{fixture.GetProperty("profile").GetString()}");
        Assert.Equal(new[]
        {
            "2027-04-04|general", "2027-04-04|us", "2027-04-04|us-thursday",
            "2028-01-01|general", "2028-01-01|us", "2028-01-01|us-thursday",
        }, eveningKeys.Order(StringComparer.Ordinal));

        var issues = root.GetProperty("openIssues").EnumerateArray().ToDictionary(issue => issue.GetProperty("id").GetString()!);
        Assert.Equal(Enumerable.Range(1, 9).Select(number => $"AUTH-{number:00}"), issues.Keys.Order(StringComparer.Ordinal));
        Assert.All(issues.Values, issue =>
        {
            Assert.Equal("open", issue.GetProperty("status").GetString());
            Assert.False(string.IsNullOrWhiteSpace(issue.GetProperty("detail").GetString()));
        });
    }

    [Fact]
    public void Exceptional_assignment_review_tracks_the_actual_data_without_claiming_approved_book_verification()
    {
        using var baseline = ReadBaseline();
        var root = baseline.RootElement;
        var review = root.GetProperty("ruleReviews").EnumerateArray()
            .Single(row => row.GetProperty("id").GetString() == "HOUR-02").GetProperty("assignmentReview");
        var sources = root.GetProperty("sources").EnumerateArray().Select(row => row.GetProperty("id").GetString()).ToArray();
        Assert.Equal("approved-book-check-pending", review.GetProperty("status").GetString());
        Assert.Equal("behavior-preserving-extraction-not-new-authority", review.GetProperty("evidenceKind").GetString());
        Assert.False(review.GetProperty("normativeVerification").GetBoolean());
        Assert.Equal(new[] { "S2", "S10" }, review.GetProperty("sources").EnumerateArray().Select(row => row.GetString()));
        Assert.All(review.GetProperty("sources").EnumerateArray(), source => Assert.Contains(source.GetString(), sources));

        var resource = review.GetProperty("resource").GetString()!;
        using var stream = typeof(Respondeo.LiturgyOfTheHours.OrdinaryFormOffice).Assembly.GetManifestResourceStream(resource);
        Assert.NotNull(stream);
        var data = Respondeo.LiturgyOfTheHours.OfficeData.TemporalPsalmody;
        var assignments = data.Triduum.OrderBy(row => row.Key).SelectMany(row =>
            row.Value.Morning is null ? new[] { $"{row.Key}/evening" } : new[] { $"{row.Key}/morning", $"{row.Key}/evening" });
        Assert.Equal(assignments, review.GetProperty("triduumAssignments").EnumerateArray().Select(row => row.GetString()));
        Assert.Equal(data.LentenSundayEveningCanticle, review.GetProperty("lentenSundayEveningCanticle").GetString());
        Assert.Equal("holy-week-psalter-not-an-override", review.GetProperty("holyThursdayMorning").GetString());
        Assert.Equal("reuse-offset-zero", review.GetProperty("easterOctave").GetString());
    }

    [Fact]
    public void US_obligation_policy_remains_provisional_and_linked_to_an_open_decree_review()
    {
        using var baseline = ReadBaseline();
        var root = baseline.RootElement;
        var review = root.GetProperty("ruleReviews").EnumerateArray().Single(row => row.GetProperty("id").GetString() == "CAL-06");
        Assert.Equal("pending-reconciliation", review.GetProperty("status").GetString());
        Assert.Equal("provisional-national-profile", review.GetProperty("policyStatus").GetString());
        Assert.Equal("pending-direct-source-review", review.GetProperty("decreeReview").GetString());
        var rules = new[]
        {
            LiturgicalObligationRule.UnitedStatesNationalPolicy,
            LiturgicalObligationRule.UnitedStatesSaturdayMondayException,
            LiturgicalObligationRule.TransferEffectNotModeled,
        }.Select(rule => new LiturgicalObligation(LiturgicalObligationStatus.Unknown, rule).RuleId);
        Assert.Equal(rules, review.GetProperty("ruleIds").EnumerateArray().Select(row => row.GetString()));
        var issue = root.GetProperty("openIssues").EnumerateArray()
            .Single(row => row.GetProperty("id").GetString() == review.GetProperty("openIssue").GetString());
        Assert.Equal("open", issue.GetProperty("status").GetString());
        Assert.Equal(review.GetProperty("id").GetString(), issue.GetProperty("ruleReview").GetString());
    }

    private static JsonDocument ReadBaseline([CallerFilePath] string file = "")
    {
        var root = Directory.GetParent(file)!.Parent!.Parent!.Parent!.FullName;
        return JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "docs", "liturgical-authority-baseline.json")));
    }

    private static string[][] ReadRows(string resource)
    {
        using var stream = typeof(LiturgicalCalendars).Assembly.GetManifestResourceStream($"Respondeo.LiturgicalCalendar.{resource}")
            ?? throw new InvalidOperationException($"Calendar resource not found: {resource}");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd().Split('\n').Select(line => line.Trim())
            .Where(line => line.Length > 0 && !line.StartsWith('#'))
            .Select(line => line.Split('|', StringSplitOptions.TrimEntries)).ToArray();
    }

    private static (string Date, string Name) Key(string[] row) => (row[0], row[3]);

    private static LiturgicalCelebration Celebration(string[] row)
    {
        Assert.Contains(row.Length, new[] { 4, 5, 6, 8 });
        _ = MonthDay(row[0]);
        return new LiturgicalCelebration(row[3], Enum.Parse<CelebrationRank>(row[1]), Enum.Parse<LiturgicalColor>(row[2]))
        {
            Id = row.Length >= 5 && row[4].Length > 0 ? row[4] : null,
            Summary = row.Length >= 6 && row[5].Length > 0 ? row[5] : null,
            CalendarKey = row.Length == 8 ? row[6] : null,
            Precedence = row.Length == 8 ? Enum.Parse<LiturgicalPrecedenceClass>(row[7]) : null,
        };
    }

    private static DateOnly MonthDay(string value) => DateOnly.ParseExact($"2000-{value}", "yyyy-MM-dd", CultureInfo.InvariantCulture);
}
