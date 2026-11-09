namespace Respondeo.Services;

/// <summary>
/// Opens the quick menu from outside its own toggle (e.g. a reader or player bar on phones, where the masthead is hidden).
/// </summary>
public sealed class QuickMenuLauncher
{
    public event Action? OpenRequested;

    public void Open() => OpenRequested?.Invoke();
}
