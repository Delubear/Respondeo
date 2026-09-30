using Microsoft.JSInterop;
using Respondeo.Services;

namespace Respondeo.UnitTests.Services;

public class VisitedNodesTests
{
    [Fact]
    public async Task Marking_records_nodes()
    {
        var js = new FakeSessionStorage();
        var visited = new VisitedNodes(js);

        await visited.MarkVisitedAsync("a");
        await visited.MarkVisitedAsync("b");
        var result = await visited.GetVisitedAsync();

        Assert.Equal(new HashSet<string> { "a", "b" }, result);
    }

    [Fact]
    public async Task Marking_is_idempotent()
    {
        var js = new FakeSessionStorage();
        var visited = new VisitedNodes(js);

        await visited.MarkVisitedAsync("a");
        await visited.MarkVisitedAsync("a");
        var result = await visited.GetVisitedAsync();

        Assert.Single(result);
        Assert.Contains("a", result);
    }

    [Fact]
    public async Task Marking_survives_back_navigation_order()
    {
        var js = new FakeSessionStorage();
        var visited = new VisitedNodes(js);

        await visited.MarkVisitedAsync("a");
        await visited.MarkVisitedAsync("b");
        await visited.MarkVisitedAsync("c");
        await visited.MarkVisitedAsync("a");
        var result = await visited.GetVisitedAsync();

        Assert.Equal(new HashSet<string> { "a", "b", "c" }, result);
    }

    [Fact]
    public async Task Clear_removes_all_visited_nodes()
    {
        var js = new FakeSessionStorage();
        var visited = new VisitedNodes(js);

        await visited.MarkVisitedAsync("a");
        await visited.MarkVisitedAsync("b");
        await visited.ClearAsync();
        var result = await visited.GetVisitedAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task Corrupt_storage_is_treated_as_empty()
    {
        var js = new FakeSessionStorage();
        js.Set("respondeo.visited", "not-json");
        var visited = new VisitedNodes(js);

        var result = await visited.GetVisitedAsync();

        Assert.Empty(result);
    }

    /// <summary>Minimal in-memory IJSRuntime emulating the sessionStorage calls the service makes.</summary>
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
