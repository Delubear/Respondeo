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
    private const string TopTarget = "top";
    private const string ContentTarget = "content";

    private readonly NavigationManager _nav;
    private readonly NavigationIntent _intent;
    private readonly IJSRuntime _js;

    // The last location processed by Sync.
    private string _lastPath;
    private string _lastUri;

    // The default landing for the current navigation: still waiting on the layout (_pending, unless _claimed),
    // or held by a page that finishes loading late (_deferred).
    private string? _pending;
    private string? _deferred;
    private bool _claimed;

    public ScrollCoordinator(NavigationManager nav, NavigationIntent intent, IJSRuntime js)
    {
        _nav = nav;
        _intent = intent;
        _js = js;
        _lastUri = nav.Uri;
        _lastPath = PathOf(nav.Uri);
        _nav.LocationChanged += OnLocationChanged;
    }

    private static string PathOf(string uri) => uri.Split('?', '#')[0];

    private void OnLocationChanged(object? sender, LocationChangedEventArgs e) => Sync();

    // The router may re-render the destination page (which calls Claim/Defer) before this service's LocationChanged handler runs,
    // so every entry point first processes the current location once; otherwise a page would act on the previous navigation's landing.
    private void Sync()
    {
        var uri = _nav.Uri;
        if (uri == _lastUri)
        {
            return;
        }

        _lastUri = uri;
        _deferred = null;
        var resetToTop = _intent.ConsumeResetToTop();
        var path = PathOf(uri);
        var samePage = path == _lastPath;
        _lastPath = path;
        _claimed = false;

        if (samePage)
        {
            _pending = null;
            _ = _js.InvokeVoidAsync("respondeoScroll.land", ContentTarget, true);
            return;
        }

        _pending = resetToTop ? TopTarget : ContentTarget;
    }

    /// <summary>Marks the current navigation's landing as owned by the page, so the layout skips the default.</summary>
    public void Claim()
    {
        Sync();
        _claimed = true;
    }

    /// <summary>
    /// For pages that finish loading after the layout's post-navigation render: holds the default landing (top or content)
    /// until the page calls <see cref="ApplyDeferredAsync"/> once its content has rendered.
    /// </summary>
    public void Defer()
    {
        Sync();
        _deferred = _pending;
        _claimed = true;
    }

    /// <summary>Masks native route content before rendering, until the asynchronous scroll landing settles.</summary>
    public ValueTask BeginNavigationAsync(string path) => _js is IJSInProcessRuntime
        ? ValueTask.CompletedTask
        : _js.InvokeVoidAsync("respondeoScroll.beginNavigation", path);

    /// <summary>Applies a landing held by <see cref="Defer"/>.</summary>
    public ValueTask ApplyDeferredAsync() => _deferred is not null
        ? LandAsync(_deferred, center: false)
        : ValueTask.CompletedTask;

    /// <summary>Lands instantly on the element with <paramref name="id"/> (or centers it when <paramref name="center"/> is set).</summary>
    /// <remarks>WebAssembly lands synchronously before paint; the native host awaits interop and unmasks content after landing.</remarks>
    public ValueTask LandAtAsync(string id, bool center = false) => LandAsync(id, center);

    /// <summary>Applies the default landing for the last navigation, unless a page claimed it. Called by the layout after each render.</summary>
    public ValueTask ApplyDefaultAsync()
    {
        Sync();
        return _pending is not null && !_claimed
            ? LandAsync(_pending, center: false)
            : ValueTask.CompletedTask;
    }

    // Settles the current navigation's landing: whatever lands now consumes any pending or deferred default.
    private ValueTask LandAsync(string target, bool center)
    {
        _pending = null;
        _deferred = null;
        const bool smooth = false;
        if (_js is IJSInProcessRuntime inProcess)
        {
            inProcess.InvokeVoid("respondeoScroll.land", target, smooth, center);
            return ValueTask.CompletedTask;
        }

        return _js.InvokeVoidAsync("respondeoScroll.land", target, smooth, center);
    }

    public void Dispose() => _nav.LocationChanged -= OnLocationChanged;
}
