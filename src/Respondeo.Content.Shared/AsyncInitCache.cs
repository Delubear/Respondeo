namespace Respondeo.Content.Shared;

/// <summary>
/// A one-time asynchronous lazy-initialization cache.
/// Runs the supplied factory at most once even under concurrent access,
/// using the double-checked <see cref="SemaphoreSlim"/> pattern that every content service would otherwise hand-roll, then hands back the cached value on subsequent calls.
/// </summary>
/// <typeparam name="T">The reference type produced once and cached for the app's lifetime.</typeparam>
public sealed class AsyncInitCache<T>
    where T : class
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private T? _value;

    /// <summary>True once the value has been initialized.</summary>
    public bool IsInitialized => _value is not null;

    /// <summary>
    /// Returns the cached value, invoking <paramref name="factory"/> exactly once to create it on the first call.
    /// Concurrent callers await the single in-flight initialization.
    /// </summary>
    public async Task<T> GetAsync(Func<Task<T>> factory)
    {
        if (_value is not null)
        {
            return _value;
        }

        await _gate.WaitAsync();
        try
        {
            _value ??= await factory();
            return _value;
        }
        finally
        {
            _gate.Release();
        }
    }
}
