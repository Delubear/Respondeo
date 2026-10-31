namespace Respondeo.Services;

/// <summary>
/// Opens a floating help orb's dialog on request (e.g. from a pinned quick-menu entry), even on routes where that orb is not normally shown.
/// </summary>
public sealed class OrbLauncher
{
    public const string Liturgical = "liturgical";
    public const string Prayers = "prayers";
    public const string Saints = "saints";
    public const string Summa = "summa";
    public const string HourSettings = "hour-settings";

    /// <summary>The orb waiting to be opened by a guide that has not mounted yet.</summary>
    public string? Pending { get; private set; }

    public event Action<string>? Requested;

    public void Request(string key)
    {
        Pending = key;
        Requested?.Invoke(key);
    }

    public bool TryConsume(string? key)
    {
        if (key is null || !string.Equals(Pending, key, StringComparison.Ordinal))
        {
            return false;
        }

        Pending = null;
        return true;
    }
}
