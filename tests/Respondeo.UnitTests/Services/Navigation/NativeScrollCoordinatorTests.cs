using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Respondeo.UnitTests.Services.Navigation;

public sealed class NativeScrollCoordinatorTests : TestContext
{
    [Fact]
    public async Task BeginNavigation_WaitsForNativeMask()
    {
        var js = new PendingJsRuntime();
        using var sut = CreateCoordinator(js);

        var pending = sut.BeginNavigationAsync("why-god");

        Assert.False(pending.IsCompleted);
        Assert.Equal("respondeoScroll.beginNavigation", js.Identifier);
        Assert.Equal(new object?[] { "why-god" }, js.Arguments);
        js.Completion.SetResult();
        await pending;
    }

    [Fact]
    public async Task Landing_WaitsForNativeScroll()
    {
        var js = new PendingJsRuntime();
        using var sut = CreateCoordinator(js);

        var pending = sut.LandAtAsync("v16", center: true);

        Assert.False(pending.IsCompleted);
        Assert.Equal("respondeoScroll.land", js.Identifier);
        Assert.Equal(new object?[] { "v16", false, true }, js.Arguments);
        js.Completion.SetResult();
        await pending;
    }

    [Fact]
    public async Task Landing_PropagatesInteropFailure()
    {
        var js = new PendingJsRuntime();
        using var sut = CreateCoordinator(js);
        var error = new JSException("WebView scroll failed");

        var pending = sut.LandAtAsync("v16").AsTask();
        js.Completion.SetException(error);

        Assert.Same(error, await Assert.ThrowsAsync<JSException>(() => pending));
    }

    [Fact]
    public async Task InProcessNavigation_DoesNotRequestMask()
    {
        using var sut = CreateCoordinator(JSInterop.JSRuntime);

        await sut.BeginNavigationAsync("why-god");

        Assert.Empty(JSInterop.Invocations);
    }

    private ScrollCoordinator CreateCoordinator(IJSRuntime js) =>
        new(Services.GetRequiredService<FakeNavigationManager>(), new NavigationIntent(), js);

    private sealed class PendingJsRuntime : IJSRuntime
    {
        public TaskCompletionSource Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public string? Identifier { get; private set; }
        public object?[]? Arguments { get; private set; }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
            InvokeAsync<TValue>(identifier, CancellationToken.None, args);

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            Identifier = identifier;
            Arguments = args;
            return new ValueTask<TValue>(CompleteAsync<TValue>(cancellationToken));
        }

        private async Task<TValue> CompleteAsync<TValue>(CancellationToken cancellationToken)
        {
            await Completion.Task.WaitAsync(cancellationToken);
            return default!;
        }
    }
}
