using Microsoft.JSInterop;
using Respondeo.Services;

namespace Respondeo.UnitTests.Services;

public class BreadcrumbTrailTests
{
    [Fact]
    public async Task Visit_appends_new_nodes_in_order()
    {
        var js = new FakeSessionStorage();
        var trail = new BreadcrumbTrail(js);

        await trail.VisitAsync("a");
        await trail.VisitAsync("b");
        var result = await trail.VisitAsync("c");

        Assert.Equal(["a", "b", "c"], result);
    }

    [Fact]
    public async Task Visit_truncates_back_to_revisited_node()
    {
        var js = new FakeSessionStorage();
        var trail = new BreadcrumbTrail(js);

        await trail.VisitAsync("a");
        await trail.VisitAsync("b");
        await trail.VisitAsync("c");
        var result = await trail.VisitAsync("a");

        Assert.Equal(["a"], result);
    }

    [Fact]
    public async Task Clear_removes_the_trail()
    {
        var js = new FakeSessionStorage();
        var trail = new BreadcrumbTrail(js);

        await trail.VisitAsync("a");
        await trail.VisitAsync("b");
        await trail.ClearAsync();
        var result = await trail.VisitAsync("c");

        Assert.Equal(["c"], result);
    }

    [Fact]
    public async Task Corrupt_storage_is_treated_as_empty()
    {
        var js = new FakeSessionStorage();
        js.Set("respondeo.breadcrumb", "not-json");
        var trail = new BreadcrumbTrail(js);

        var result = await trail.VisitAsync("a");

        Assert.Equal(["a"], result);
    }

    [Fact]
    public async Task Articles_origin_defaults_to_false()
    {
        var js = new FakeSessionStorage();
        var trail = new BreadcrumbTrail(js);

        Assert.False(await trail.IsFromArticlesAsync());
    }

    [Fact]
    public async Task Articles_origin_round_trips_when_set()
    {
        var js = new FakeSessionStorage();
        var trail = new BreadcrumbTrail(js);

        await trail.SetArticlesOriginAsync(true);

        Assert.True(await trail.IsFromArticlesAsync());
    }

    [Fact]
    public async Task Articles_origin_can_be_unset()
    {
        var js = new FakeSessionStorage();
        var trail = new BreadcrumbTrail(js);

        await trail.SetArticlesOriginAsync(true);
        await trail.SetArticlesOriginAsync(false);

        Assert.False(await trail.IsFromArticlesAsync());
    }

    [Fact]
    public async Task Clear_resets_the_articles_origin()
    {
        var js = new FakeSessionStorage();
        var trail = new BreadcrumbTrail(js);

        await trail.SetArticlesOriginAsync(true);
        await trail.ClearAsync();

        Assert.False(await trail.IsFromArticlesAsync());
    }

    /// <summary>Minimal in-memory IJSRuntime emulating the sessionStorage calls the trail makes.</summary>
    private sealed class FakeSessionStorage : IJSRuntime
    {
        private readonly Dictionary<string, string> _store = new();

        public void Set(string key, string value) => _store[key] = value;

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => InvokeAsync<TValue>(identifier, CancellationToken.None, args);

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            var key = args is { Length: > 0 } ? args[0] as string ?? string.Empty : string.Empty;

            switch (identifier)
            {
                case "sessionStorage.getItem":
                    var value = _store.TryGetValue(key, out var v) ? v : null;
                    return ValueTask.FromResult((TValue)(object?)value!);
                case "sessionStorage.setItem":
                    _store[key] = args![1] as string ?? string.Empty;
                    return ValueTask.FromResult(default(TValue)!);
                case "sessionStorage.removeItem":
                    _store.Remove(key);
                    return ValueTask.FromResult(default(TValue)!);
                default:
                    throw new InvalidOperationException($"Unexpected JS call: {identifier}");
            }
        }
    }
}
