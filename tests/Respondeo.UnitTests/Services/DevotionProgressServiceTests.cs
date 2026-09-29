using Microsoft.JSInterop;
using Respondeo.Services;

namespace Respondeo.UnitTests.Services;

public class DevotionProgressServiceTests
{
    [Fact]
    public async Task Load_returns_null_when_nothing_saved()
    {
        var js = new FakeStorageJs();
        var service = new DevotionProgressService(js);

        var progress = await service.LoadAsync("rosary");

        Assert.Null(progress);
    }

    [Fact]
    public async Task Save_then_load_round_trips_progress()
    {
        var js = new FakeStorageJs();
        var service = new DevotionProgressService(js);

        await service.SaveAsync(new DevotionProgress("rosary", "sorrowful", 7));
        var progress = await service.LoadAsync("rosary");

        Assert.NotNull(progress);
        Assert.Equal("rosary", progress!.DevotionId);
        Assert.Equal("sorrowful", progress.SetId);
        Assert.Equal(7, progress.CompletedCount);
    }

    [Fact]
    public async Task Load_ignores_a_slot_for_a_different_devotion()
    {
        var js = new FakeStorageJs();
        var service = new DevotionProgressService(js);
        await service.SaveAsync(new DevotionProgress("rosary", "sorrowful", 7));

        var progress = await service.LoadAsync("divine-mercy-chaplet");

        Assert.Null(progress);
    }

    [Fact]
    public async Task Save_replaces_the_single_active_slot()
    {
        var js = new FakeStorageJs();
        var service = new DevotionProgressService(js);

        await service.SaveAsync(new DevotionProgress("rosary", "sorrowful", 7));
        await service.SaveAsync(new DevotionProgress("divine-mercy-chaplet", null, 3));

        Assert.Null(await service.LoadAsync("rosary"));
        var current = await service.LoadAsync("divine-mercy-chaplet");
        Assert.NotNull(current);
        Assert.Null(current!.SetId);
        Assert.Equal(3, current.CompletedCount);
    }

    [Fact]
    public async Task Clear_removes_the_slot_for_the_matching_devotion()
    {
        var js = new FakeStorageJs();
        var service = new DevotionProgressService(js);
        await service.SaveAsync(new DevotionProgress("rosary", "sorrowful", 7));

        await service.ClearAsync("rosary");

        Assert.Null(await service.LoadAsync("rosary"));
    }

    [Fact]
    public async Task Clear_leaves_a_slot_belonging_to_another_devotion()
    {
        var js = new FakeStorageJs();
        var service = new DevotionProgressService(js);
        await service.SaveAsync(new DevotionProgress("rosary", "sorrowful", 7));

        await service.ClearAsync("divine-mercy-chaplet");

        Assert.NotNull(await service.LoadAsync("rosary"));
    }

    [Fact]
    public async Task Load_returns_null_for_a_corrupt_value()
    {
        var js = new FakeStorageJs();
        js.Set("respondeo.devotion.progress", "{ not valid json");
        var service = new DevotionProgressService(js);

        var progress = await service.LoadAsync("rosary");

        Assert.Null(progress);
    }

    /// <summary>Minimal in-memory IJSRuntime emulating localStorage get/set/remove.</summary>
    private sealed class FakeStorageJs : IJSRuntime
    {
        private readonly Dictionary<string, string> _store = new();

        public void Set(string key, string value) => _store[key] = value;

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => InvokeAsync<TValue>(identifier, CancellationToken.None, args);

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            var key = args is { Length: > 0 } ? args[0] as string ?? string.Empty : string.Empty;
            switch (identifier)
            {
                case "localStorage.getItem":
                    var value = _store.TryGetValue(key, out var v) ? v : null;
                    return ValueTask.FromResult((TValue)(object?)value!);
                case "localStorage.setItem":
                    _store[key] = args![1] as string ?? string.Empty;
                    return ValueTask.FromResult(default(TValue)!);
                case "localStorage.removeItem":
                    _store.Remove(key);
                    return ValueTask.FromResult(default(TValue)!);
                default:
                    throw new InvalidOperationException($"Unexpected JS call: {identifier}");
            }
        }
    }
}
