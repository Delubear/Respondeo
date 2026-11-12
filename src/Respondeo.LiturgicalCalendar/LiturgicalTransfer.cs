namespace Respondeo.LiturgicalCalendar;

/// <summary>The rule that moves a solemnity from its nominal date.</summary>
public enum LiturgicalTransferRule
{
    /// <summary>Saint Joseph is anticipated to the Saturday before Palm Sunday when impeded during Holy Week.</summary>
    JosephAnticipatedBeforePalmSunday,

    /// <summary>A celebration in Holy Week or the Easter octave is moved to the Monday after the octave.</summary>
    HolyWeekOrEasterOctave,

    /// <summary>A solemnity on a Sunday of Lent or Advent is moved to the following Monday.</summary>
    PrivilegedSunday,

    /// <summary>John the Baptist and the Sacred Heart have a specific transfer direction when they coincide.</summary>
    JohnTheBaptistSacredHeartCollision,

    /// <summary>Saints Peter and Paul move to the next available day when impeded by the Sacred Heart.</summary>
    PeterAndPaulSacredHeartCollision,
}

/// <summary>A contiguous range of dates rejected while searching for an available transfer destination.</summary>
public sealed record LiturgicalDateRange
{
    public LiturgicalDateRange(DateOnly Start, DateOnly End)
    {
        if (End < Start)
        {
            throw new ArgumentOutOfRangeException(nameof(End), End, "The end date must be on or after the start date.");
        }

        this.Start = Start;
        this.End = End;
    }

    public DateOnly Start { get; }

    public DateOnly End { get; }

    /// <summary>The number of dates in the inclusive range.</summary>
    public int DayCount => End.DayNumber - Start.DayNumber + 1;

    public void Deconstruct(out DateOnly Start, out DateOnly End)
    {
        Start = this.Start;
        End = this.End;
    }
}

/// <summary>A solemnity's nominal date, observed date, identity, and transfer rule.</summary>
public sealed record LiturgicalTransfer
{
    public LiturgicalTransfer(
        string CelebrationKey,
        DateOnly NominalDate,
        DateOnly ObservedDate,
        LiturgicalTransferRule Rule)
        : this(CelebrationKey, NominalDate, ObservedDate, Rule, null)
    {
    }

    public LiturgicalTransfer(
        string CelebrationKey,
        DateOnly NominalDate,
        DateOnly ObservedDate,
        LiturgicalTransferRule Rule,
        LiturgicalDateRange? SkippedDateRange)
    {
        if (Rule == LiturgicalTransferRule.PeterAndPaulSacredHeartCollision)
        {
            if (CelebrationKey != "saints-peter-and-paul"
                || NominalDate is not { Month: 6, Day: 29 }
                || ObservedDate <= NominalDate)
            {
                throw new ArgumentException(
                    "The Peter and Paul collision rule requires its June 29 solemnity and a later observed date.",
                    nameof(ObservedDate));
            }

            var firstCandidate = NominalDate.AddDays(1);
            if (ObservedDate == firstCandidate && SkippedDateRange is not null
                || ObservedDate > firstCandidate && (SkippedDateRange is null
                    || SkippedDateRange.Start != firstCandidate
                    || SkippedDateRange.End != ObservedDate.AddDays(-1)))
            {
                throw new ArgumentException(
                    "Skipped dates must exactly describe rejected dates before the observed date.",
                    nameof(SkippedDateRange));
            }
        }
        else if (SkippedDateRange is not null)
        {
            throw new ArgumentException(
                "Skipped dates are only available for the Peter and Paul collision rule.",
                nameof(SkippedDateRange));
        }

        this.CelebrationKey = CelebrationKey;
        this.NominalDate = NominalDate;
        this.ObservedDate = ObservedDate;
        this.Rule = Rule;
        this.SkippedDateRange = SkippedDateRange;
    }

    public string CelebrationKey { get; }

    public DateOnly NominalDate { get; }

    public DateOnly ObservedDate { get; }

    public LiturgicalTransferRule Rule { get; }

    /// <summary>Contiguous dates rejected before the observed date; used only by the Peter and Paul/Sacred Heart search.</summary>
    public LiturgicalDateRange? SkippedDateRange { get; }

    public void Deconstruct(
        out string CelebrationKey,
        out DateOnly NominalDate,
        out DateOnly ObservedDate,
        out LiturgicalTransferRule Rule)
    {
        CelebrationKey = this.CelebrationKey;
        NominalDate = this.NominalDate;
        ObservedDate = this.ObservedDate;
        Rule = this.Rule;
    }

    /// <summary>A stable identifier within the plan's CAL-03 transfer rule family.</summary>
    public string RuleId => Rule switch
    {
        LiturgicalTransferRule.JosephAnticipatedBeforePalmSunday => "CAL-03/joseph-anticipated-before-palm-sunday",
        LiturgicalTransferRule.HolyWeekOrEasterOctave => "CAL-03/holy-week-or-easter-octave",
        LiturgicalTransferRule.PrivilegedSunday => "CAL-03/privileged-sunday",
        LiturgicalTransferRule.JohnTheBaptistSacredHeartCollision => "CAL-03/john-the-baptist-sacred-heart-collision",
        LiturgicalTransferRule.PeterAndPaulSacredHeartCollision => "CAL-03/peter-and-paul-sacred-heart-collision",
        _ => throw new InvalidOperationException($"Unknown liturgical transfer rule: {Rule}."),
    };
}

internal readonly record struct LiturgicalTransferResolution(
    DateOnly Target,
    LiturgicalTransferRule Rule,
    LiturgicalDateRange? SkippedDateRange = null);

internal static class LiturgicalTransferResolver
{
    internal static LiturgicalTransferResolution? Resolve(
        DateOnly original,
        DateOnly easter,
        LiturgicalSeason season,
        string celebrationKey,
        bool johnTheBaptistIsPatron,
        Func<DateOnly, bool> isAvailable)
    {
        var easterOffset = original.DayNumber - easter.DayNumber;
        if (original is { Month: 6, Day: 24 } && easterOffset == 68
            && (celebrationKey == "nativity-of-saint-john-the-baptist" && !johnTheBaptistIsPatron
                || celebrationKey == "sacred-heart" && johnTheBaptistIsPatron))
        {
            return new(original.AddDays(-1), LiturgicalTransferRule.JohnTheBaptistSacredHeartCollision);
        }

        if (original is { Month: 6, Day: 29 } && easterOffset == 68 && celebrationKey == "saints-peter-and-paul")
        {
            var target = original.AddDays(1);
            DateOnly? firstSkippedDate = null;
            while (!isAvailable(target))
            {
                firstSkippedDate ??= target;
                target = target.AddDays(1);
            }

            var skippedDateRange = firstSkippedDate is { } firstDate
                ? new LiturgicalDateRange(firstDate, target.AddDays(-1))
                : null;
            return new(target, LiturgicalTransferRule.PeterAndPaulSacredHeartCollision, skippedDateRange);
        }

        if (original is { Month: 3, Day: 19 } && easterOffset is >= -7 and <= -1)
        {
            return new(easter.AddDays(-8), LiturgicalTransferRule.JosephAnticipatedBeforePalmSunday);
        }

        if (easterOffset is >= -7 and <= 7)
        {
            return new(easter.AddDays(8), LiturgicalTransferRule.HolyWeekOrEasterOctave);
        }

        if (original.DayOfWeek == DayOfWeek.Sunday && season is LiturgicalSeason.Lent or LiturgicalSeason.Advent)
        {
            return new(original.AddDays(1), LiturgicalTransferRule.PrivilegedSunday);
        }

        return null;
    }
}
