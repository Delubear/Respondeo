using Respondeo.Content.Shared;

namespace Respondeo.UnitTests.Shared;

/// <summary>
/// Unit tests for <see cref="AsyncInitCache{T}"/>, the single-flight lazy cache shared by the content
/// services. Covers first-call factory execution, subsequent cache reuse, single invocation under
/// concurrency, and retry after a failed initialization.
/// </summary>
public class AsyncInitCacheTests
{
    private sealed class Box
    {
        public int Value { get; init; }
    }

    [Fact]
    public async Task GetAsync_invokes_factory_on_first_call()
    {
        var cache = new AsyncInitCache<Box>();

        Assert.False(cache.IsInitialized);

        var result = await cache.GetAsync(() => Task.FromResult(new Box { Value = 7 }));

        Assert.Equal(7, result.Value);
        Assert.True(cache.IsInitialized);
    }

    [Fact]
    public async Task GetAsync_caches_value_and_runs_factory_once()
    {
        var cache = new AsyncInitCache<Box>();
        var calls = 0;

        var first = await cache.GetAsync(() =>
        {
            calls++;
            return Task.FromResult(new Box { Value = calls });
        });

        var second = await cache.GetAsync(() =>
        {
            calls++;
            return Task.FromResult(new Box { Value = calls });
        });

        Assert.Same(first, second);
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task GetAsync_runs_factory_once_under_concurrent_callers()
    {
        var cache = new AsyncInitCache<Box>();
        var calls = 0;
        var gate = new TaskCompletionSource();

        async Task<Box> Factory()
        {
            Interlocked.Increment(ref calls);
            await gate.Task;
            return new Box { Value = 42 };
        }

        var callers = Enumerable.Range(0, 10).Select(_ => cache.GetAsync(Factory)).ToArray();
        gate.SetResult();
        var results = await Task.WhenAll(callers);

        Assert.Equal(1, calls);
        Assert.All(results, r => Assert.Same(results[0], r));
    }

    [Fact]
    public async Task GetAsync_allows_retry_after_factory_throws()
    {
        var cache = new AsyncInitCache<Box>();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => cache.GetAsync(() => Task.FromException<Box>(new InvalidOperationException("boom"))));

        Assert.False(cache.IsInitialized);

        var result = await cache.GetAsync(() => Task.FromResult(new Box { Value = 99 }));

        Assert.Equal(99, result.Value);
        Assert.True(cache.IsInitialized);
    }
}
