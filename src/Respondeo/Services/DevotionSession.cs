using Respondeo.Content.Contracts;

namespace Respondeo.Services;

// A single row on the devotion thread: either a mystery announcement (with its reflection) or a
// prayer the reader taps to mark prayed. Repeated prayers each get their own row (their own bead).
public sealed class DevotionBead
{
    public string? Heading { get; init; }
    public string? MysteryTitle { get; init; }
    public string? MysteryReflectionHtml { get; init; }
    public string? PrayerTitle { get; init; }
    public string? PrayerHtml { get; init; }
    public string? BeadType { get; init; }
    public string? Note { get; init; }

    // Index of this row among prayer rows (for in-order completion); -1 for mystery announcements.
    public int PrayerOrdinal { get; set; } = -1;

    public bool IsPrayer => PrayerHtml is not null;
}

// The outcome of a completion toggle, so the hosting view can react (persist, announce, move focus)
// without re-implementing the ordering rules.
public enum DevotionToggleResult
{
    // The ordinal was neither the current nor the just-completed step; nothing changed.
    NoChange,

    // The current step was marked prayed and more steps remain.
    Advanced,

    // The most recently prayed step was un-marked.
    SteppedBack,

    // The final step was marked prayed; the devotion is now complete.
    Completed,
}

/// <summary>
/// The in-memory state and rules of a single devotion praying session: expanding the data-driven
/// sequence into a flat thread of beads, numbering the prayer rows, and advancing completion strictly
/// in order. Persistence, rendering and JS interop are the caller's concern; this type is pure logic.
/// </summary>
public sealed class DevotionSession
{
    private readonly Devotion _devotion;
    private readonly IReadOnlyDictionary<string, Prayer> _prayers;

    public DevotionSession(Devotion devotion, IReadOnlyDictionary<string, Prayer> prayers)
    {
        _devotion = devotion;
        _prayers = prayers;
    }

    /// <summary>The flat thread of rows for the current set. Empty until <see cref="Start"/> is called.</summary>
    public IReadOnlyList<DevotionBead> Beads { get; private set; } = [];

    /// <summary>The number of prayer rows in the current thread.</summary>
    public int PrayerCount { get; private set; }

    /// <summary>How many prayer rows have been prayed, in order.</summary>
    public int CompletedCount { get; private set; }

    /// <summary>The mystery set this session is praying, if any.</summary>
    public string? SelectedSetId { get; private set; }

    /// <summary>True once every prayer row has been prayed.</summary>
    public bool IsComplete => CompletedCount >= PrayerCount;

    // Builds (or rebuilds) the thread for the given set and restores a place in it. A non-zero
    // startCompleted resumes an earlier spot; the value is clamped so it can never exceed the rows.
    public void Start(string? selectedSetId, int startCompleted = 0)
    {
        SelectedSetId = selectedSetId;
        Beads = BuildBeads(selectedSetId);

        // Number the prayer rows so completion can advance through them strictly in order.
        var ordinal = 0;
        foreach (var bead in Beads)
        {
            if (bead.IsPrayer)
            {
                bead.PrayerOrdinal = ordinal++;
            }
        }

        PrayerCount = ordinal;
        CompletedCount = Math.Clamp(startCompleted, 0, PrayerCount);
    }

    // Counts how many prayer rows a given set expands to, without disturbing the live thread, so a
    // resume banner can show a total. Mirrors the ordinal numbering done in Start.
    public int CountPrayers(string? selectedSetId) => BuildBeads(selectedSetId).Count(b => b.IsPrayer);

    // Marks the next prayer complete, or un-marks the most recently completed one. Completion can only
    // move forward one step at a time, so nothing can be marked out of order.
    public DevotionToggleResult Toggle(int ordinal)
    {
        if (ordinal == CompletedCount)
        {
            CompletedCount++;
            return IsComplete ? DevotionToggleResult.Completed : DevotionToggleResult.Advanced;
        }

        if (ordinal == CompletedCount - 1)
        {
            CompletedCount--;
            return DevotionToggleResult.SteppedBack;
        }

        return DevotionToggleResult.NoChange;
    }

    // A row is "done" once completion has passed it. A mystery announcement is treated as done once
    // the pointer reaches the first prayer of the decade it introduces.
    public bool IsItemDone(int index)
    {
        var beads = Beads;
        if (beads[index].IsPrayer)
        {
            return beads[index].PrayerOrdinal < CompletedCount;
        }

        for (var j = index + 1; j < beads.Count; j++)
        {
            if (beads[j].IsPrayer)
            {
                return beads[j].PrayerOrdinal < CompletedCount;
            }
        }

        return IsComplete;
    }

    // The prayer step currently to be prayed, or null when the devotion is complete.
    public DevotionBead? CurrentStep => Beads.FirstOrDefault(b => b.IsPrayer && b.PrayerOrdinal == CompletedCount);

    // Spoken progress text for the step now to be prayed (title and position), or null when complete,
    // so screen-reader users get feedback after each mark, mirroring the visual "current" highlight.
    public string? DescribeCurrentStep()
    {
        var current = CurrentStep;
        return current is null ? null : $"Step {CompletedCount + 1} of {PrayerCount}: {current.PrayerTitle}.";
    }

    // Spoken label for a prayer step: its title plus where it sits in the thread and its state, so a
    // non-visual user hears "Hail Mary, step 12 of 59, current" rather than just "Hail Mary".
    public string PrayerStepLabel(DevotionBead step, bool done, bool current)
    {
        var position = $"step {step.PrayerOrdinal + 1} of {PrayerCount}";
        var state = done ? "prayed" : current ? "current, press to mark prayed" : "not yet prayed";
        var note = string.IsNullOrWhiteSpace(step.Note) ? null : $", {step.Note}";
        return $"{step.PrayerTitle}{note}, {position}, {state}";
    }

    // Expands the data-driven sequence into the flat list of rows. A "mysteries" step iterates the
    // chosen set, emitting an announcement row for each mystery followed by its per-mystery prayers.
    private List<DevotionBead> BuildBeads(string? selectedSetId)
    {
        var beads = new List<DevotionBead>();
        var set = _devotion.MysterySets.FirstOrDefault(s => s.Id == selectedSetId) ?? _devotion.MysterySets.FirstOrDefault();

        foreach (var step in _devotion.Sequence)
        {
            if (step.Kind == "mysteries" && set is not null)
            {
                foreach (var mystery in set.Mysteries)
                {
                    beads.Add(new DevotionBead
                    {
                        Heading = step.Title,
                        MysteryTitle = mystery.Title,
                        MysteryReflectionHtml = mystery.ReflectionHtml,
                    });

                    foreach (var sub in step.PerMystery)
                    {
                        AddPrayerRow(beads, sub, null);
                    }
                }
            }
            else
            {
                AddPrayerRow(beads, step, step.Title);
            }
        }

        return beads;
    }

    // Adds one row per repetition, so each repeated prayer (e.g. every Hail Mary of a decade) is its
    // own bead on the thread and its own step to mark complete.
    private void AddPrayerRow(List<DevotionBead> beads, DevotionStep step, string? heading)
    {
        if (string.IsNullOrWhiteSpace(step.PrayerId) || !_prayers.TryGetValue(step.PrayerId, out var prayer))
        {
            return;
        }

        var count = Math.Max(1, step.Repeat);
        for (var i = 0; i < count; i++)
        {
            beads.Add(new DevotionBead
            {
                // Only label the first of a repeated group so the heading is not shouted many times.
                Heading = i == 0 ? (heading ?? step.Title) : null,
                PrayerTitle = prayer.Title,
                PrayerHtml = prayer.Html,
                BeadType = step.Bead,
                Note = i == 0 ? step.Note : null,
            });
        }
    }
}
