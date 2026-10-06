using Microsoft.JSInterop;

namespace Respondeo.Services.Summa;

/// <summary>
/// Typed wrapper over the <c>window.respondeoSummaRef</c> JS module (see <c>wwwroot/js/summa-refs.js</c>),
/// which records the origin of a Summa cross-reference click and drives same-question fragment navigation on the raw injected <c>a.summa-ref</c> anchors.
/// </summary>
/// <remarks>
/// Centralising the interop here keeps the JS method names in one place instead of scattering magic strings across <see cref="Pages.SummaQuestion"/>,
/// matching how <see cref="BrowseStateInterop"/> and the other services wrap their JS.
/// </remarks>
public sealed class SummaRefInterop(IJSRuntime js)
{
    /// <summary>
    /// Reads the cross-reference origin recorded client-side when a <c>summa-ref</c> link was clicked,
    /// or <c>null</c> when the reader did not arrive via a cross-reference.
    /// </summary>
    public ValueTask<ReferenceOrigin?> ReadOriginAsync() => js.InvokeAsync<ReferenceOrigin?>("respondeoSummaRef.read");

    /// <summary>Consumes the recorded origin so a later refresh or normal browse stops showing the back crumb.</summary>
    public ValueTask ClearOriginAsync() => js.InvokeVoidAsync("respondeoSummaRef.clear");

    /// <summary>
    /// Registers the current page's <see cref="DotNetObjectReference{T}"/> so the delegated
    /// click listener can call back into it for references that stay within the question.
    /// </summary>
    public ValueTask RegisterPageAsync<T>(DotNetObjectReference<T> page) where T : class => js.InvokeVoidAsync("respondeoSummaRef.registerPage", page);
}
