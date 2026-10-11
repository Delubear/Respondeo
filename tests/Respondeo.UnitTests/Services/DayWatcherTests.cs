using Microsoft.JSInterop;
using NSubstitute;
using Respondeo.Services;

namespace Respondeo.UnitTests.Services;

public class DayWatcherTests
{
    // The watcher only touches JS when StartAsync runs; these tests drive Poll() directly, so a bare substitute suffices.
    private static DayWatcher Create(Func<DateOnly> today) => new(today, Substitute.For<IJSRuntime>());

    [Fact]
    public async Task Today_reflects_the_clock_at_construction()
    {
        var date = new DateOnly(2025, 6, 15);
        await using var watcher = Create(() => date);

        Assert.Equal(date, watcher.Today);
    }

    [Fact]
    public async Task Poll_does_not_raise_when_the_date_is_unchanged()
    {
        var date = new DateOnly(2025, 6, 15);
        await using var watcher = Create(() => date);
        var raised = 0;
        watcher.DayChanged += () => raised++;

        watcher.Poll();

        Assert.Equal(0, raised);
    }

    [Fact]
    public async Task Poll_raises_once_when_the_date_advances()
    {
        var date = new DateOnly(2025, 6, 15);
        await using var watcher = Create(() => date);
        var raised = 0;
        watcher.DayChanged += () => raised++;

        date = new DateOnly(2025, 6, 16);
        watcher.Poll();

        Assert.Equal(1, raised);
        Assert.Equal(new DateOnly(2025, 6, 16), watcher.Today);
    }

    [Fact]
    public async Task Poll_raises_once_per_distinct_date_not_once_per_poll()
    {
        var date = new DateOnly(2025, 6, 15);
        await using var watcher = Create(() => date);
        var raised = 0;
        watcher.DayChanged += () => raised++;

        date = new DateOnly(2025, 6, 16);
        watcher.Poll();
        watcher.Poll();
        watcher.Poll();

        Assert.Equal(1, raised);
    }

    [Fact]
    public async Task Poll_raises_again_on_a_further_rollover()
    {
        var date = new DateOnly(2025, 6, 15);
        await using var watcher = Create(() => date);
        var raised = 0;
        watcher.DayChanged += () => raised++;

        date = new DateOnly(2025, 6, 16);
        watcher.Poll();
        date = new DateOnly(2025, 6, 17);
        watcher.Poll();

        Assert.Equal(2, raised);
    }

    [Fact]
    public async Task Poll_after_dispose_does_not_raise()
    {
        var date = new DateOnly(2025, 6, 15);
        var watcher = Create(() => date);
        var raised = 0;
        watcher.DayChanged += () => raised++;
        await watcher.DisposeAsync();

        date = new DateOnly(2025, 6, 16);
        watcher.Poll();

        // Dispose stops the watcher, so a stray poll afterward must not raise notifications.
        Assert.Equal(0, raised);
    }
}
