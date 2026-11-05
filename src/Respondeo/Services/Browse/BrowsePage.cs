using Respondeo.Services.Interop;

namespace Respondeo.Services.Browse;

/// <summary>
/// Facade that browse pages inject instead of pairing <see cref="BrowseState"/> with <see cref="BrowseStateInterop"/> by hand.
/// It wires each section's exit callback to clear the matching JS-owned store and passes the scroll/accordion interop through.
/// </summary>
public sealed class BrowsePage(BrowseState state, BrowseStateInterop js)
{
    /// <summary>
    /// Returns the browse section for <paramref name="area"/>, clearing the JS store for <paramref name="jsKey"/> when the visitor leaves the area.
    /// </summary>
    public BrowseSection Section(string area, string jsKey) => state.Section(area, () => js.ClearAsync(jsKey));

    /// <inheritdoc cref="BrowseStateInterop.RestoreScrollAsync"/>
    public ValueTask RestoreScrollAsync(string key) => js.RestoreScrollAsync(key);

    /// <inheritdoc cref="BrowseStateInterop.GetOpenAsync"/>
    public ValueTask<string[]> GetOpenAsync(string key) => js.GetOpenAsync(key);
}
