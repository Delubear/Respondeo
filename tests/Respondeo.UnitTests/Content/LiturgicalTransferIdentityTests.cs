using Respondeo.LiturgicalCalendar;

namespace Respondeo.UnitTests.Content;

public class LiturgicalTransferIdentityTests
{
    public static TheoryData<string, int, int, int, int, int, string, LiturgicalTransferRule> TransferCases
    {
        get
        {
            var cases = new TheoryData<string, int, int, int, int, int, string, LiturgicalTransferRule>();
            foreach (var profile in new[] { "general", "us", "us-thursday" })
            {
                cases.Add(profile, 2028, 3, 19, 3, 20, "saint-joseph", LiturgicalTransferRule.PrivilegedSunday);
                cases.Add(profile, 2035, 3, 19, 3, 17, "saint-joseph", LiturgicalTransferRule.JosephAnticipatedBeforePalmSunday);
                cases.Add(profile, 2027, 3, 25, 4, 5, "annunciation", LiturgicalTransferRule.HolyWeekOrEasterOctave);
                cases.Add(profile, 2030, 12, 8, 12, 9, "immaculate-conception", LiturgicalTransferRule.PrivilegedSunday);
                cases.Add(profile, 2022, 6, 24, 6, 23, "nativity-of-saint-john-the-baptist", LiturgicalTransferRule.JohnTheBaptistSacredHeartCollision);
                cases.Add(profile, 2033, 6, 24, 6, 23, "nativity-of-saint-john-the-baptist", LiturgicalTransferRule.JohnTheBaptistSacredHeartCollision);
                cases.Add(profile, 1973, 6, 29, 6, 30, "saints-peter-and-paul", LiturgicalTransferRule.PeterAndPaulSacredHeartCollision);
                cases.Add(profile, 2057, 6, 29, 6, 30, "saints-peter-and-paul", LiturgicalTransferRule.PeterAndPaulSacredHeartCollision);
            }
            return cases;
        }
    }

    [Theory]
    [MemberData(nameof(TransferCases))]
    public void Transfer_reuses_the_complete_regional_catalog_record_exactly_once(
        string profile, int year, int month, int day, int targetMonth, int targetDay, string key, LiturgicalTransferRule rule)
    {
        var calendar = LiturgicalCalendars.Create(PreferredLiturgicalCalendar.Deserialize(profile));
        var source = Assert.Single(GeneralRomanCalendarData.ForMonthDay(month, day, calendar.Options.Region), celebration => celebration.CalendarKey == key);
        var nominalDate = new DateOnly(year, month, day);
        var target = new DateOnly(year, targetMonth, targetDay);
        var nominal = calendar.ForDate(nominalDate);
        var transferred = calendar.ForDate(target);
        var transfer = Assert.Single(transferred.Transfers);

        Assert.Equal(new LiturgicalTransfer(key, nominalDate, target, rule), transfer);
        Assert.Equal(transfer, Assert.Single(nominal.Transfers));
        Assert.Equal(transferred, calendar.ForDate(target));
        Assert.Equal(transferred.GetHashCode(), calendar.ForDate(target).GetHashCode());
        Assert.Equal(nominal, calendar.ForDate(nominalDate));
        Assert.Equal(nominal.GetHashCode(), calendar.ForDate(nominalDate).GetHashCode());
        Assert.Equal($"CAL-03/{rule switch
        {
            LiturgicalTransferRule.JosephAnticipatedBeforePalmSunday => "joseph-anticipated-before-palm-sunday",
            LiturgicalTransferRule.HolyWeekOrEasterOctave => "holy-week-or-easter-octave",
            LiturgicalTransferRule.PrivilegedSunday => "privileged-sunday",
            LiturgicalTransferRule.JohnTheBaptistSacredHeartCollision => "john-the-baptist-sacred-heart-collision",
            LiturgicalTransferRule.PeterAndPaulSacredHeartCollision => "peter-and-paul-sacred-heart-collision",
            _ => throw new InvalidOperationException($"Unexpected transfer rule: {rule}."),
        }}", transfer.RuleId);
        Assert.Equal(source, transferred.Celebration);
        Assert.Equal(source.Color, transferred.Color);
        Assert.Equal(CelebrationRank.Solemnity, transferred.Celebration!.Rank);
        Assert.Equal(LiturgicalPrecedenceClass.GeneralSolemnityOrAllSouls, transferred.Precedence);
        Assert.Equal(LiturgicalOccurrenceRule.Solemnity, transferred.Occurrence!.Rule);
        if (key == "saints-peter-and-paul" && year == 1973)
        {
            Assert.Contains(transferred.Occurrence.SuppressedCelebrations, celebration =>
                celebration.CalendarKey == "the-first-martyrs-of-the-holy-roman-church");
        }

        Assert.NotEqual(key, nominal.Celebration?.CalendarKey);
        Assert.DoesNotContain(calendar.ForDate(nominalDate).OptionalMemorials, celebration => celebration.CalendarKey == key);

        var nearbyOccurrences = Enumerable.Range(-10, 31).Select(offset => calendar.ForDate(nominalDate.AddDays(offset)))
            .Where(resolved => resolved.Celebration?.CalendarKey == key).Select(resolved => resolved.Date);
        Assert.Equal(new[] { target }, nearbyOccurrences);
        if (key is "annunciation" or "immaculate-conception")
        {
            Assert.False(string.IsNullOrWhiteSpace(source.Summary));
            Assert.Equal(source.Summary, transferred.Celebration.Summary);
        }
    }

    [Theory]
    [InlineData("general")]
    [InlineData("us")]
    [InlineData("us-thursday")]
    public void Transferred_Annunciation_wins_over_a_memorial_on_the_Easter_transfer_target(string profile)
    {
        var calendar = LiturgicalCalendars.Create(PreferredLiturgicalCalendar.Deserialize(profile));
        var target = new DateOnly(2035, 4, 2);
        var francis = Assert.Single(GeneralRomanCalendarData.ForMonthDay(4, 2, calendar.Options.Region),
            celebration => celebration.CalendarKey == "saint-francis-of-paola-hermit");
        var transferred = calendar.ForDate(target);

        Assert.Equal("annunciation", transferred.Celebration?.CalendarKey);
        Assert.Equal(LiturgicalPrecedenceClass.GeneralSolemnityOrAllSouls, transferred.Precedence);
        Assert.Equal(new LiturgicalTransfer("annunciation", new DateOnly(2035, 3, 25), target,
            LiturgicalTransferRule.HolyWeekOrEasterOctave), Assert.Single(transferred.Transfers));
        Assert.Equal(LiturgicalOccurrenceRule.Solemnity, transferred.Occurrence?.Rule);
        Assert.Contains(francis, transferred.Occurrence!.SuppressedCelebrations);
        Assert.DoesNotContain(francis, transferred.OptionalMemorials);
    }

    [Theory]
    [InlineData("general")]
    [InlineData("us")]
    [InlineData("us-thursday")]
    public void Annunciation_transfers_suppress_fixed_memorials_on_occupied_targets_across_a_gregorian_cycle(
        string profile)
    {
        var calendar = LiturgicalCalendars.Create(PreferredLiturgicalCalendar.Deserialize(profile));
        var occupiedTargetCount = 0;

        for (var year = 2000; year < 2400; year++)
        {
            var nominal = calendar.ForDate(new DateOnly(year, 3, 25));
            var transfer = nominal.Transfers.SingleOrDefault(item => item.CelebrationKey == "annunciation");
            if (transfer is null)
            {
                continue;
            }

            var target = calendar.ForDate(transfer.ObservedDate);
            var fixedCelebrations = GeneralRomanCalendarData.ForMonthDay(
                transfer.ObservedDate.Month, transfer.ObservedDate.Day, calendar.Options.Region);
            if (fixedCelebrations.Count == 0)
            {
                continue;
            }

            occupiedTargetCount++;
            Assert.Equal("annunciation", target.Celebration?.CalendarKey);
            Assert.Equal(transfer, Assert.Single(target.Transfers));
            Assert.Equal(LiturgicalOccurrenceRule.Solemnity, target.Occurrence?.Rule);
            foreach (var fixedCelebration in fixedCelebrations)
            {
                Assert.True(fixedCelebration.Rank is CelebrationRank.Memorial or CelebrationRank.OptionalMemorial);
                Assert.Contains(fixedCelebration, target.Occurrence!.SuppressedCelebrations);
                Assert.DoesNotContain(fixedCelebration, target.OptionalMemorials);
            }
        }

        Assert.Equal(44, occupiedTargetCount);
    }

    [Theory]
    [InlineData("general")]
    [InlineData("us")]
    [InlineData("us-thursday")]
    public void Immaculate_Conception_transfers_suppress_Saint_Juan_Diego_on_occupied_targets_across_a_gregorian_cycle(
        string profile)
    {
        var calendar = LiturgicalCalendars.Create(PreferredLiturgicalCalendar.Deserialize(profile));
        var occupiedTargetCount = 0;

        for (var year = 2000; year < 2400; year++)
        {
            var nominal = calendar.ForDate(new DateOnly(year, 12, 8));
            var transfer = nominal.Transfers.SingleOrDefault(item => item.CelebrationKey == "immaculate-conception");
            if (transfer is null)
            {
                continue;
            }

            var target = calendar.ForDate(transfer.ObservedDate);
            var juanDiego = Assert.Single(GeneralRomanCalendarData.ForMonthDay(
                transfer.ObservedDate.Month, transfer.ObservedDate.Day, calendar.Options.Region),
                celebration => celebration.CalendarKey == "saint-juan-diego-cuauhtlatoatzin");

            occupiedTargetCount++;
            Assert.Equal(new DateOnly(year, 12, 9), transfer.ObservedDate);
            Assert.Equal(LiturgicalTransferRule.PrivilegedSunday, transfer.Rule);
            Assert.Equal("immaculate-conception", target.Celebration?.CalendarKey);
            Assert.Equal(transfer, Assert.Single(target.Transfers));
            Assert.Equal(LiturgicalOccurrenceRule.Solemnity, target.Occurrence?.Rule);
            Assert.Contains(juanDiego, target.Occurrence!.SuppressedCelebrations);
            Assert.DoesNotContain(juanDiego, target.OptionalMemorials);
        }

        Assert.Equal(57, occupiedTargetCount);
    }

    [Theory]
    [InlineData("general")]
    [InlineData("us")]
    [InlineData("us-thursday")]
    public void Saint_Joseph_anticipation_suppresses_fixed_memorials_on_occupied_targets_across_a_gregorian_cycle(
        string profile)
    {
        var calendar = LiturgicalCalendars.Create(PreferredLiturgicalCalendar.Deserialize(profile));
        var occupiedTargetCount = 0;

        for (var year = 2000; year < 2400; year++)
        {
            var nominal = calendar.ForDate(new DateOnly(year, 3, 19));
            var transfer = nominal.Transfers.SingleOrDefault(item => item.CelebrationKey == "saint-joseph");
            if (transfer is null)
            {
                continue;
            }

            var target = calendar.ForDate(transfer.ObservedDate);
            var fixedCelebrations = GeneralRomanCalendarData.ForMonthDay(
                transfer.ObservedDate.Month, transfer.ObservedDate.Day, calendar.Options.Region);
            if (fixedCelebrations.Count == 0)
            {
                continue;
            }

            occupiedTargetCount++;
            Assert.Equal(LiturgicalTransferRule.JosephAnticipatedBeforePalmSunday, transfer.Rule);
            Assert.Equal("saint-joseph", target.Celebration?.CalendarKey);
            Assert.Equal(transfer, Assert.Single(target.Transfers));
            Assert.Equal(LiturgicalOccurrenceRule.Solemnity, target.Occurrence?.Rule);
            foreach (var fixedCelebration in fixedCelebrations)
            {
                Assert.True(fixedCelebration.Rank is CelebrationRank.Memorial or CelebrationRank.OptionalMemorial);
                Assert.Contains(fixedCelebration, target.Occurrence!.SuppressedCelebrations);
                Assert.DoesNotContain(fixedCelebration, target.OptionalMemorials);
            }
        }

        Assert.Equal(22, occupiedTargetCount);
    }

    [Theory]
    [InlineData("general")]
    [InlineData("us")]
    [InlineData("us-thursday")]
    public void Privileged_Sunday_transfers_preserve_identity_across_a_gregorian_cycle(string profile)
    {
        var calendar = LiturgicalCalendars.Create(PreferredLiturgicalCalendar.Deserialize(profile));
        var josephCount = 0;
        var annunciationCount = 0;

        foreach (var year in Enumerable.Range(2000, 400))
        {
            foreach (var (month, day, key) in new[]
            {
                (3, 19, "saint-joseph"),
                (3, 25, "annunciation"),
            })
            {
                var nominalDate = new DateOnly(year, month, day);
                var transfer = calendar.ForDate(nominalDate).Transfers.SingleOrDefault(item =>
                    item.CelebrationKey == key && item.Rule == LiturgicalTransferRule.PrivilegedSunday);
                if (transfer is null)
                {
                    continue;
                }

                Assert.Equal(DayOfWeek.Sunday, nominalDate.DayOfWeek);
                Assert.Equal(nominalDate.AddDays(1), transfer.ObservedDate);
                var target = calendar.ForDate(transfer.ObservedDate);
                Assert.Equal(key, target.Celebration?.CalendarKey);
                Assert.Equal(transfer, Assert.Single(target.Transfers));
                Assert.Equal(LiturgicalOccurrenceRule.Solemnity, target.Occurrence?.Rule);

                if (key == "saint-joseph")
                {
                    josephCount++;
                }
                else
                {
                    annunciationCount++;
                }
            }
        }

        Assert.Equal(43, josephCount);
        Assert.Equal(36, annunciationCount);
    }

    [Theory]
    [InlineData("general")]
    [InlineData("us")]
    [InlineData("us-thursday")]
    public void Annunciation_transfers_follow_the_applicable_rule_across_a_gregorian_cycle(string profile)
    {
        var calendar = LiturgicalCalendars.Create(PreferredLiturgicalCalendar.Deserialize(profile));
        var easterOctaveCount = 0;
        var privilegedSundayCount = 0;

        for (var year = 2000; year < 2400; year++)
        {
            var nominalDate = new DateOnly(year, 3, 25);
            var nominal = calendar.ForDate(nominalDate);
            var easter = Computus.GregorianEaster(year);
            var easterOffset = nominalDate.DayNumber - easter.DayNumber;
            var expectedRule = easterOffset is >= -7 and <= 7
                ? LiturgicalTransferRule.HolyWeekOrEasterOctave
                : nominalDate.DayOfWeek == DayOfWeek.Sunday && nominal.Season == LiturgicalSeason.Lent
                    ? LiturgicalTransferRule.PrivilegedSunday
                    : (LiturgicalTransferRule?)null;
            var transfer = nominal.Transfers.SingleOrDefault(item => item.CelebrationKey == "annunciation");

            if (expectedRule is null)
            {
                Assert.Null(transfer);
                continue;
            }

            Assert.NotNull(transfer);
            Assert.Equal(expectedRule, transfer.Rule);
            Assert.Equal(expectedRule == LiturgicalTransferRule.HolyWeekOrEasterOctave
                ? easter.AddDays(8)
                : nominalDate.AddDays(1), transfer.ObservedDate);
            var target = calendar.ForDate(transfer.ObservedDate);
            Assert.Equal("annunciation", target.Celebration?.CalendarKey);
            Assert.Equal(transfer, Assert.Single(target.Transfers));
            Assert.Equal(LiturgicalOccurrenceRule.Solemnity, target.Occurrence?.Rule);

            if (expectedRule == LiturgicalTransferRule.HolyWeekOrEasterOctave)
            {
                easterOctaveCount++;
            }
            else
            {
                privilegedSundayCount++;
            }
        }

        Assert.Equal(107, easterOctaveCount);
        Assert.Equal(36, privilegedSundayCount);
    }

    [Fact]
    public void Liturgical_date_range_counts_both_endpoints_and_rejects_reversed_dates()
    {
        var sameDayRange = new LiturgicalDateRange(Start: new DateOnly(1973, 6, 30), End: new DateOnly(1973, 6, 30));
        var (start, end) = sameDayRange;

        Assert.Equal(new DateOnly(1973, 6, 30), start);
        Assert.Equal(new DateOnly(1973, 6, 30), end);
        Assert.Equal(1, sameDayRange.DayCount);
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new LiturgicalDateRange(new DateOnly(1973, 7, 1), new DateOnly(1973, 6, 30)));
    }

    [Fact]
    public void Skipped_dates_must_belong_to_the_forward_search_transfer_and_precede_its_target()
    {
        var nominalDate = new DateOnly(1973, 6, 29);
        var observedDate = new DateOnly(1973, 7, 2);
        var skippedDates = new LiturgicalDateRange(new DateOnly(1973, 6, 30), new DateOnly(1973, 7, 1));

        var transfer = new LiturgicalTransfer(
            CelebrationKey: "saints-peter-and-paul",
            NominalDate: nominalDate,
            ObservedDate: observedDate,
            Rule: LiturgicalTransferRule.PeterAndPaulSacredHeartCollision,
            SkippedDateRange: skippedDates);

        Assert.Equal(skippedDates, transfer.SkippedDateRange);
        var firstAvailable = new LiturgicalTransfer("saints-peter-and-paul", nominalDate, nominalDate.AddDays(1),
            LiturgicalTransferRule.PeterAndPaulSacredHeartCollision);
        Assert.Null(firstAvailable.SkippedDateRange);
        Assert.Throws<ArgumentException>(() => new LiturgicalTransfer("saints-peter-and-paul", nominalDate,
            observedDate, LiturgicalTransferRule.PeterAndPaulSacredHeartCollision));
        Assert.Throws<ArgumentException>(() => new LiturgicalTransfer("other-celebration", nominalDate,
            observedDate, LiturgicalTransferRule.PeterAndPaulSacredHeartCollision, skippedDates));
        Assert.Throws<ArgumentException>(() => new LiturgicalTransfer("saints-peter-and-paul", nominalDate.AddDays(1),
            observedDate, LiturgicalTransferRule.PeterAndPaulSacredHeartCollision, skippedDates));
        Assert.Throws<ArgumentException>(() => new LiturgicalTransfer("saints-peter-and-paul", nominalDate,
            nominalDate.AddDays(-1), LiturgicalTransferRule.PeterAndPaulSacredHeartCollision));
        Assert.Throws<ArgumentException>(() => new LiturgicalTransfer("saints-peter-and-paul", nominalDate,
            observedDate, LiturgicalTransferRule.HolyWeekOrEasterOctave, skippedDates));
        Assert.Throws<ArgumentException>(() => new LiturgicalTransfer("saints-peter-and-paul", nominalDate,
            observedDate, LiturgicalTransferRule.PeterAndPaulSacredHeartCollision,
            new LiturgicalDateRange(nominalDate, nominalDate.AddDays(1))));
        Assert.Throws<ArgumentException>(() => new LiturgicalTransfer("saints-peter-and-paul", nominalDate,
            observedDate, LiturgicalTransferRule.PeterAndPaulSacredHeartCollision,
            new LiturgicalDateRange(nominalDate.AddDays(2), observedDate.AddDays(-1))));
        Assert.Throws<ArgumentException>(() => new LiturgicalTransfer("saints-peter-and-paul", nominalDate,
            observedDate, LiturgicalTransferRule.PeterAndPaulSacredHeartCollision,
            new LiturgicalDateRange(nominalDate.AddDays(1), observedDate.AddDays(-2))));
        var (key, nominal, observed, rule) = transfer;
        Assert.Equal(("saints-peter-and-paul", nominalDate, observedDate,
            LiturgicalTransferRule.PeterAndPaulSacredHeartCollision), (key, nominal, observed, rule));
    }

    [Fact]
    public void Peter_and_Paul_transfer_skip_unavailable_days()
    {
        var original = new DateOnly(1973, 6, 29);
        var unavailableDates = new HashSet<DateOnly>
        {
            new(1973, 6, 30),
            new(1973, 7, 1),
        };

        var resolution = LiturgicalTransferResolver.Resolve(
            original,
            Computus.GregorianEaster(original.Year),
            LiturgicalSeason.OrdinaryTime,
            "saints-peter-and-paul",
            johnTheBaptistIsPatron: false,
            isAvailable: date => !unavailableDates.Contains(date));

        Assert.Equal(new DateOnly(1973, 7, 2), resolution?.Target);
        Assert.Equal(LiturgicalTransferRule.PeterAndPaulSacredHeartCollision, resolution?.Rule);
        Assert.Equal(new LiturgicalDateRange(new DateOnly(1973, 6, 30), new DateOnly(1973, 7, 1)),
            resolution?.SkippedDateRange);
        Assert.Equal(2, resolution?.SkippedDateRange?.DayCount);
    }

    [Theory]
    [InlineData("general")]
    [InlineData("us")]
    [InlineData("us-thursday")]
    public void Peter_and_Paul_transfer_to_the_next_available_day_when_the_Sacred_Heart_falls_on_June_29(
        string profile)
    {
        var calendar = LiturgicalCalendars.Create(PreferredLiturgicalCalendar.Deserialize(profile));
        var collisionCount = 0;

        for (var year = 2000; year < 2400; year++)
        {
            var date = new DateOnly(year, 6, 29);
            if (Computus.GregorianEaster(year).AddDays(68) != date)
            {
                continue;
            }

            collisionCount++;
            var nominal = calendar.ForDate(date);
            var transfer = Assert.Single(nominal.Transfers);
            Assert.Equal("saints-peter-and-paul", transfer.CelebrationKey);
            Assert.True(transfer.ObservedDate > date);
            Assert.Equal(LiturgicalTransferRule.PeterAndPaulSacredHeartCollision, transfer.Rule);
            Assert.Equal("sacred-heart", nominal.Celebration?.CalendarKey);
            Assert.Equal(LiturgicalOccurrenceRule.Solemnity, nominal.Occurrence?.Rule);

            var target = calendar.ForDate(transfer.ObservedDate);
            Assert.Equal("saints-peter-and-paul", target.Celebration?.CalendarKey);
            Assert.Equal(LiturgicalOccurrenceRule.Solemnity, target.Occurrence?.Rule);
            Assert.Equal(transfer, Assert.Single(target.Transfers));
        }

        Assert.Equal(10, collisionCount);
    }

    [Fact]
    public void U_S_Corpus_Christi_remains_on_its_observed_Sunday_without_transferring_John()
    {
        var calendar = LiturgicalCalendars.Create(LiturgicalCalendarOptions.Default with
        {
            Region = CalendarRegion.UnitedStates,
        });
        var corpusChristi = calendar.ForDate(new DateOnly(2038, 6, 27));
        var john = calendar.ForDate(new DateOnly(2038, 6, 24));

        Assert.Equal("corpus-christi", corpusChristi.Celebration?.CalendarKey);
        Assert.Equal("nativity-of-saint-john-the-baptist", john.Celebration?.CalendarKey);
        Assert.Empty(john.Transfers);
    }

    [Fact]
    public void Corpus_Christi_did_not_coincide_with_John_the_Baptist_in_2021()
    {
        var calendar = LiturgicalCalendars.Create(LiturgicalCalendarOptions.Default);
        var corpusChristi = calendar.ForDate(new DateOnly(2021, 6, 3));
        var john = calendar.ForDate(new DateOnly(2021, 6, 24));

        Assert.Equal("corpus-christi", corpusChristi.Celebration?.CalendarKey);
        Assert.Equal("nativity-of-saint-john-the-baptist", john.Celebration?.CalendarKey);
        Assert.Empty(john.Transfers);
    }

    [Theory]
    [InlineData("general")]
    [InlineData("us")]
    [InlineData("us-thursday")]
    public void John_patron_reverses_the_Sacred_Heart_collision_transfer(string profile)
    {
        var options = PreferredLiturgicalCalendar.Deserialize(profile) with { SaintJohnTheBaptistIsPatron = true };
        var calendar = LiturgicalCalendars.Create(options);
        var john = calendar.ForDate(new DateOnly(2022, 6, 24));
        var sacredHeart = calendar.ForDate(new DateOnly(2022, 6, 23));

        Assert.Equal("nativity-of-saint-john-the-baptist", john.Celebration?.CalendarKey);
        Assert.Equal("sacred-heart", sacredHeart.Celebration?.CalendarKey);
        Assert.Equal(new LiturgicalTransfer("sacred-heart", new DateOnly(2022, 6, 24), new DateOnly(2022, 6, 23),
            LiturgicalTransferRule.JohnTheBaptistSacredHeartCollision), Assert.Single(sacredHeart.Transfers));
        Assert.Equal(sacredHeart.Transfers, john.Transfers);
    }

    [Theory]
    [InlineData("general")]
    [InlineData("us")]
    [InlineData("us-thursday")]
    public void John_and_Sacred_Heart_collision_directions_are_consistent_across_a_gregorian_cycle(string profile)
    {
        var ordinaryCalendar = LiturgicalCalendars.Create(PreferredLiturgicalCalendar.Deserialize(profile));
        var patronOptions = PreferredLiturgicalCalendar.Deserialize(profile) with { SaintJohnTheBaptistIsPatron = true };
        var patronCalendar = LiturgicalCalendars.Create(patronOptions);
        var collisionCount = 0;

        for (var year = 2000; year < 2400; year++)
        {
            var nominal = new DateOnly(year, 6, 24);
            var ordinaryDay = ordinaryCalendar.ForDate(nominal);
            var transfer = ordinaryDay.Transfers.SingleOrDefault(item =>
                item.Rule == LiturgicalTransferRule.JohnTheBaptistSacredHeartCollision);
            if (transfer is null)
            {
                continue;
            }

            collisionCount++;
            Assert.Equal(new DateOnly(year, 6, 23), transfer.ObservedDate);
            Assert.Equal("nativity-of-saint-john-the-baptist", transfer.CelebrationKey);
            Assert.Equal("sacred-heart", ordinaryDay.Celebration?.CalendarKey);
            var ordinaryTarget = ordinaryCalendar.ForDate(transfer.ObservedDate);
            Assert.Equal("nativity-of-saint-john-the-baptist", ordinaryTarget.Celebration?.CalendarKey);
            Assert.Equal(transfer, Assert.Single(ordinaryTarget.Transfers));

            var patronDay = patronCalendar.ForDate(nominal);
            var patronTransfer = Assert.Single(patronDay.Transfers);
            Assert.Equal("sacred-heart", patronTransfer.CelebrationKey);
            Assert.Equal(transfer.ObservedDate, patronTransfer.ObservedDate);
            Assert.Equal(LiturgicalTransferRule.JohnTheBaptistSacredHeartCollision, patronTransfer.Rule);
            Assert.Equal("nativity-of-saint-john-the-baptist", patronDay.Celebration?.CalendarKey);
            var patronTarget = patronCalendar.ForDate(patronTransfer.ObservedDate);
            Assert.Equal("sacred-heart", patronTarget.Celebration?.CalendarKey);
            Assert.Equal(patronTransfer, Assert.Single(patronTarget.Transfers));
        }

        Assert.Equal(15, collisionCount);
    }

    [Theory]
    [InlineData("general")]
    [InlineData("us")]
    [InlineData("us-thursday")]
    public void Unimpeded_solemnities_keep_the_same_identity_and_metadata_on_their_nominal_dates(string profile)
    {
        var calendar = LiturgicalCalendars.Create(PreferredLiturgicalCalendar.Deserialize(profile));
        (int Month, int Day, string Key)[] cases =
        [
            (3, 19, "saint-joseph"),
            (3, 25, "annunciation"),
            (6, 24, "nativity-of-saint-john-the-baptist"),
            (6, 29, "saints-peter-and-paul"),
            (12, 8, "immaculate-conception"),
        ];
        foreach (var item in cases)
        {
            var source = Assert.Single(GeneralRomanCalendarData.ForMonthDay(item.Month, item.Day, calendar.Options.Region));
            var nominal = new DateOnly(2026, item.Month, item.Day);
            var actual = calendar.ForDate(nominal);
            Assert.Equal(item.Key, source.CalendarKey);
            Assert.Equal(source, actual.Celebration);
            Assert.Empty(actual.Transfers);
        }
    }

    [Theory]
    [InlineData("us")]
    [InlineData("us-thursday")]
    public void US_prayer_day_preserves_identity_when_Sunday_moves_it_to_Monday(string profile)
    {
        const string key = "day-of-prayer-for-unborn-children";
        var calendar = LiturgicalCalendars.Create(PreferredLiturgicalCalendar.Deserialize(profile));
        var source = Assert.Single(GeneralRomanCalendarData.ForMonthDay(1, 22, calendar.Options.Region));
        Assert.Equal(key, source.CalendarKey);
        Assert.Equal(LiturgicalPrecedenceClass.ProperMemorial, source.Precedence);

        for (var year = 2027; year <= 2040; year++)
        {
            var nominal = new DateOnly(year, 1, 22);
            var target = nominal.DayOfWeek == DayOfWeek.Sunday ? nominal.AddDays(1) : nominal;
            var actual = calendar.ForDate(target);
            Assert.Equal(source, actual.Celebration);
            Assert.Equal(LiturgicalColor.Violet, actual.Color);
            Assert.Equal(LiturgicalPrecedenceClass.ProperMemorial, actual.Precedence);
            var occurrences = new[] { calendar.ForDate(nominal), calendar.ForDate(nominal.AddDays(1)) }
                .Where(resolved => resolved.Celebration?.CalendarKey == key).Select(resolved => resolved.Date);
            Assert.Equal(new[] { target }, occurrences);
        }

        var general = LiturgicalCalendars.Create(LiturgicalCalendarOptions.Default);
        Assert.DoesNotContain(GeneralRomanCalendarData.ForMonthDay(1, 22), celebration => celebration.CalendarKey == key);
        Assert.NotEqual(key, general.ForDate(new(2034, 1, 23)).Celebration?.CalendarKey);
    }
}
