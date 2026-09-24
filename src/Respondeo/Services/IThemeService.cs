namespace Respondeo.Services;

/// <summary>
/// The available color themes for the app.
/// </summary>
public enum Theme
{
    Light,
    Dark,
}

/// <summary>
/// Reads and applies the visitor's preferred color <see cref="Theme"/>,
/// persisting the choice across sessions.
/// </summary>
public interface IThemeService
{
    /// <summary>
    /// Gets the currently applied theme. Defaults to <see cref="Theme.Light"/> until initialized.
    /// </summary>
    Theme Current { get; }

    /// <summary>
    /// Reads the persisted preference (falling back to the OS setting) and applies it to the document.
    /// </summary>
    Task InitializeAsync();

    /// <summary>
    /// Switches between light and dark, persists the choice, and applies it to the document.
    /// </summary>
    Task ToggleAsync();
}
