using Microsoft.JSInterop;
using Respondeo.Services;

namespace Respondeo.UnitTests.Services;

public class ThemeServiceTests
{
    [Fact]
    public async Task Initialize_uses_stored_preference()
    {
        var js = new FakeThemeJs();
        js.Set("respondeo.theme", "dark");
        var service = new ThemeService(js);

        await service.InitializeAsync();

        Assert.Equal(Theme.Dark, service.Current);
        Assert.Equal("dark", js.AppliedTheme);
    }

    [Fact]
    public async Task Initialize_defaults_to_light_when_no_preference()
    {
        var js = new FakeThemeJs();
        var service = new ThemeService(js);

        await service.InitializeAsync();

        Assert.Equal(Theme.Light, service.Current);
        Assert.Equal("light", js.AppliedTheme);
    }

    [Fact]
    public async Task Toggle_switches_persists_and_applies()
    {
        var js = new FakeThemeJs();
        var service = new ThemeService(js);
        await service.InitializeAsync();

        await service.ToggleAsync();

        Assert.Equal(Theme.Dark, service.Current);
        Assert.Equal("dark", js.AppliedTheme);
        Assert.Equal("dark", js.Get("respondeo.theme"));

        await service.ToggleAsync();

        Assert.Equal(Theme.Light, service.Current);
        Assert.Equal("light", js.AppliedTheme);
        Assert.Equal("light", js.Get("respondeo.theme"));
    }

    /// <summary>Minimal in-memory IJSRuntime emulating localStorage and the respondeoTheme helper.</summary>
    private sealed class FakeThemeJs : IJSRuntime
    {
        private readonly Dictionary<string, string> _store = new();

        public string? AppliedTheme { get; private set; }

        public void Set(string key, string value) => _store[key] = value;

        public string? Get(string key) => _store.TryGetValue(key, out var v) ? v : null;

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => InvokeAsync<TValue>(identifier, CancellationToken.None, args);

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            switch (identifier)
            {
                case "localStorage.getItem":
                    var value = Get(args![0] as string ?? string.Empty);
                    return ValueTask.FromResult((TValue)(object?)value!);
                case "localStorage.setItem":
                    _store[args![0] as string ?? string.Empty] = args[1] as string ?? string.Empty;
                    return ValueTask.FromResult(default(TValue)!);
                case "respondeoTheme.apply":
                    AppliedTheme = args![0] as string;
                    return ValueTask.FromResult(default(TValue)!);
                default:
                    throw new InvalidOperationException($"Unexpected JS call: {identifier}");
            }
        }
    }
}
