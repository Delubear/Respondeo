namespace Respondeo.LiturgyOfTheHours;

/// <summary>How the morning Invitatory psalm is chosen from its options.</summary>
public enum InvitatoryMode
{
    /// <summary>List every option and let the reader choose.</summary>
    ShowAll,

    /// <summary>Always use one chosen psalm.</summary>
    Specific,

    /// <summary>Pick one at random each time the Hour opens.</summary>
    Random,

    /// <summary>Rotate through the options in order, one per day.</summary>
    Cycle,
}

/// <summary>
/// Picks which Invitatory psalm options to show for a given <see cref="InvitatoryMode"/>. Pure, so it is unit-testable.
/// </summary>
public static class InvitatorySelector
{
    /// <summary>The stable key of an option: its Scripture citation, falling back to its title.</summary>
    public static string Key(OfficeStep option) => option.Reference?.Citation ?? option.Title;

    /// <summary>
    /// Returns the indices (into <paramref name="options"/>) to render. Every mode except
    /// <see cref="InvitatoryMode.ShowAll"/> returns exactly one index when there are options.
    /// </summary>
    public static IReadOnlyList<int> Select(
        IReadOnlyList<OfficeStep> options, InvitatoryMode mode, string? specificKey, int cycleIndex, Random random)
    {
        if (options.Count == 0)
        {
            return [];
        }

        return mode switch
        {
            InvitatoryMode.Specific => [Math.Max(0, IndexOf(options, specificKey))],
            InvitatoryMode.Random => [random.Next(options.Count)],
            InvitatoryMode.Cycle => [Wrap(cycleIndex, options.Count)],
            _ => [.. Enumerable.Range(0, options.Count)],
        };
    }

    /// <summary>
    /// Advances the cycle once per day: on a new day the index moves to the next option and the date is recorded;
    /// on the same day the stored index is reused so the day's psalm stays the same.
    /// </summary>
    public static (int Index, DateOnly Date) AdvanceCycle(int index, DateOnly? lastDate, DateOnly today, int optionCount)
    {
        if (optionCount <= 0)
        {
            return (0, today);
        }

        if (lastDate == today)
        {
            return (Wrap(index, optionCount), today);
        }

        return (lastDate is null ? Wrap(index, optionCount) : Wrap(index + 1, optionCount), today);
    }

    private static int IndexOf(IReadOnlyList<OfficeStep> options, string? key)
    {
        for (var i = 0; i < options.Count; i++)
        {
            if (string.Equals(Key(options[i]), key, StringComparison.Ordinal))
            {
                return i;
            }
        }

        return -1;
    }

    private static int Wrap(int value, int count) => ((value % count) + count) % count;
}
