using Microsoft.JSInterop;
using Respondeo.LiturgicalCalendar;

namespace Respondeo.Services.Calendar;

/// <summary>
/// Remembers which liturgical calendar the reader follows (the General Roman Calendar or the United States proper calendar,
/// with the Ascension kept on Thursday or Sunday), persisting the choice in the browser's <c>localStorage</c>.
/// </summary>
public interface ICalendarPreference
{
    /// <summary>The currently selected calendar options. Defaults to the General Roman Calendar until initialized.</summary>
    LiturgicalCalendarOptions Options { get; }

    /// <summary>Raised after the selection changes (including when the stored preference first loads).</summary>
    event Action? Changed;

    /// <summary>Loads the stored preference (safe to call more than once).</summary>
    Task InitializeAsync();

    /// <summary>Sets and persists the selection.</summary>
    Task SetAsync(LiturgicalCalendarOptions options);
}

/// <summary>
/// The app's <see cref="ILiturgicalCalendar"/>: it delegates to the engine for the reader's chosen calendar,
/// so every date-driven surface follows the same preference. Modeled on <see cref="ThemeService"/> for persistence.
/// </summary>
public sealed class PreferredLiturgicalCalendar(IJSRuntime js) : ICalendarPreference, ILiturgicalCalendar
{
    private const string StorageKey = "respondeo.calendar";
    private readonly LocalStorage _storage = new(js);
    private ILiturgicalCalendar _engine = LiturgicalCalendars.Create(LiturgicalCalendarOptions.Default);
    private Task? _initialization;

    public LiturgicalCalendarOptions Options { get; private set; } = LiturgicalCalendarOptions.Default;

    public event Action? Changed;

    public LiturgicalDay ForDate(DateOnly date) => _engine.ForDate(date);

    public Task InitializeAsync() => _initialization ??= LoadAsync();

    public async Task SetAsync(LiturgicalCalendarOptions options)
    {
        Apply(options);
        await _storage.SetAsync(StorageKey, Serialize(options));
    }

    private async Task LoadAsync()
    {
        var stored = await _storage.GetAsync(StorageKey);
        var options = Deserialize(stored);
        if (options != Options)
        {
            Apply(options);
        }
    }

    private void Apply(LiturgicalCalendarOptions options)
    {
        Options = options;
        _engine = LiturgicalCalendars.Create(options);
        Changed?.Invoke();
    }

    internal static string Serialize(LiturgicalCalendarOptions options) => options switch
    {
        { Region: CalendarRegion.UnitedStates, AscensionOnThursday: true } => "us-thursday",
        { Region: CalendarRegion.UnitedStates } => "us",
        _ => "general",
    };

    internal static LiturgicalCalendarOptions Deserialize(string? stored) => stored switch
    {
        "us" => new(CalendarRegion.UnitedStates),
        "us-thursday" => new(CalendarRegion.UnitedStates, AscensionOnThursday: true),
        _ => LiturgicalCalendarOptions.Default,
    };
}
