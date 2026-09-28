using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;

namespace Respondeo.Services;

/// <summary>
/// Shared lifecycle for the per-area "browse view state" services (Summa, Discover, Miracles).
/// Each of those remembers transient view state (search text, facet filters, scroll position)
/// across the repeated remounts of a browse page so that pressing Back returns the visitor to the same view,
/// and each resets itself once the visitor navigates out of its area.
/// <para>
/// That navigation lifecycle - subscribe to <see cref="NavigationManager.LocationChanged"/>,
/// decide whether the new route is still "in the area", and reset on exit - was identical across all three services and lives here.
/// Subclasses supply their area's root segment and override <see cref="OnExitArea"/> to clear their own payload.
/// Registered as scoped services, which in Blazor WebAssembly live for the whole app session, so the state outlives the browse-page remounts.
/// </para>
/// </summary>
public abstract class AreaBrowseState : IDisposable
{
    private readonly NavigationManager _nav;
    private readonly string _areaSegment;

    /// <param name="nav">The navigation manager whose location changes drive the reset-on-exit.</param>
    /// <param name="areaSegment">
    /// The first path segment that identifies the area (e.g. "summa").
    /// A route counts as "in the area" when it equals this segment or begins with it followed by "/",
    /// so drilling into a detail page keeps the state while a route outside the area clears it.
    /// </param>
    protected AreaBrowseState(NavigationManager nav, string areaSegment)
    {
        _nav = nav;
        _areaSegment = areaSegment;
        _nav.LocationChanged += OnLocationChanged;
    }

    /// <summary>
    /// Clears this service's remembered view state. Called when the visitor navigates out of the area;
    /// subclasses override it to reset their own fields (and coordinate any JS-owned stores).
    /// </summary>
    protected abstract void OnExitArea();

    private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        var path = new Uri(e.Location).AbsolutePath.Trim('/');
        var inArea = path.Equals(_areaSegment, StringComparison.OrdinalIgnoreCase) || path.StartsWith(_areaSegment + "/", StringComparison.OrdinalIgnoreCase);

        // Detail pages live under the area segment; those still count as "in the area" so the browse state is kept while drilling into an item and coming back.
        // Only a route outside it clears it.
        if (!inArea)
        {
            OnExitArea();
        }
    }

    // Only unhooks a managed event; there are no unmanaged resources and no finalizer,
    // so GC.SuppressFinalize would be a no-op. Suppressed rather than adding meaningless noise.
    [SuppressMessage("Usage", "CA1816:Dispose methods should call SuppressFinalize", Justification = "No finalizer to suppress; type holds only managed state.")]
    public void Dispose() => _nav.LocationChanged -= OnLocationChanged;
}
