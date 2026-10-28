using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.JSInterop;

namespace Respondeo.Services.Navigation;

/// <summary>
/// The single owner of where the window lands after an in-app navigation.
/// </summary>
/// <remarks>
/// Every landing is one instant scroll, applied once the destination has rendered (the router has already prefetched its data),
/// exactly like following a plain anchor link. Nothing else in the app scrolls the window on navigation.
/// <list type="bullet">
/// <item>A page with its own target (a Summa question's title/article, a Bible verse) calls <see cref="Claim"/> while setting its parameters,
/// then <see cref="LandAtAsync"/> after it renders.</item>
/// <item>Every other page gets the default, applied by the layout via <see cref="ApplyDefaultAsync"/>:
/// the very top for masthead (top-level) navigation, otherwise the top of the content region.</item>
/// <item>A query-only change on the same page glides to the content region; no new page render is coming.</item>
/// </list>
/// </remarks>
public sealed class ScrollCoordinator : IDisposable
{
    private readonly NavigationManager _nav;
    private readonly NavigationIntent _intent;
    private readonly IJSRuntime _js;
    private string _lastPath;
    private string? _pending;
    private bool _claimed;

    public ScrollCoordinator(NavigationManager nav, NavigationIntent intent, IJSRuntime js)
    {
        _nav = nav;
        _intent = intent;
        _js = js;
        _lastPath = PathOf(nav.Uri);
        _nav.LocationChanged += OnLocationChanged;
    }

    private static string PathOf(string uri) => uri.Split('?', '#')[0];

    private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        var resetToTop = _intent.ConsumeResetToTop();
        var path = PathOf(e.Location);
        var samePage = path == _lastPath;
        _lastPath = path;
        _claimed = false;

        if (samePage)
        {
            _pending = null;
            _ = _js.InvokeVoidAsync("respondeoScroll.land", "content", true);
            return;
        }

        _pending = resetToTop ? "top" : "content";
    }

    /// <summary>Marks the current navigation's landing as owned by the page, so the layout skips the default.</summary>
    public void Claim() => _claimed = true;

    /// <summary>
    /// For pages that finish loading after the layout's post-navigation render: holds the default landing (top or content)
    /// until the page calls <see cref="ApplyDeferredAsync"/> once its content has rendered.
    /// </summary>
    public void Defer()
    {
        _deferred = _pending;
        _claimed = true;
    }

    /// <summary>Applies a landing held by <see cref="Defer"/>.</summary>
    public ValueTask ApplyDeferredAsync()
    {
        if (_deferred is null)
        {
            return ValueTask.CompletedTask;
        }

        var target = _deferred;
        _deferred = null;
        _pending = null;
        Land(target, false, false);
        return ValueTask.CompletedTask;
    }

    private string? _deferred;

    /// <summary>Lands instantly on the element with <paramref name="id"/> (or centers it when <paramref name="center"/> is set).</summary>
    /// <remarks>Uses synchronous in-process interop so the scroll runs in the same task as the render, before the browser can paint the new content at the old scroll position.</remarks>
    public ValueTask LandAtAsync(string id, bool center = false)
    {
        _pending = null;
        Land(id, false, center);
        return ValueTask.CompletedTask;
    }

    /// <summary>Applies the default landing for the last navigation, unless a page claimed it. Called by the layout after each render.</summary>
    public ValueTask ApplyDefaultAsync()
    {
        if (_pending is null || _claimed)
        {
            return ValueTask.CompletedTask;
        }

        var target = _pending;
        _pending = null;
        Land(target, false, false);
        return ValueTask.CompletedTask;
    }

    private void Land(string target, bool smooth, bool center)
    {
        if (_js is IJSInProcessRuntime inProcess)
        {
            inProcess.InvokeVoid("respondeoScroll.land", target, smooth, center);
        }
        else
        {
            _ = _js.InvokeVoidAsync("respondeoScroll.land", target, smooth, center);
        }
    }

    public void Dispose() => _nav.LocationChanged -= OnLocationChanged;
}
