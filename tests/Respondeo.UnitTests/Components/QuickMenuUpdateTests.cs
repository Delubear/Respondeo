using Bunit;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Respondeo.Components;
using Respondeo.Content.Contracts;
using Respondeo.Services;

namespace Respondeo.UnitTests.Components;

public sealed class QuickMenuUpdateTests : TestContext
{
    private IAppUpdateService Configure(bool supported)
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        var updates = Substitute.For<IAppUpdateService>();
        updates.Supported.Returns(supported);
        Services.AddSingleton(updates);
        var pins = Substitute.For<IPinService>();
        pins.Pins.Returns(Array.Empty<string>());
        Services.AddSingleton(pins);
        var devotions = Substitute.For<IDevotionService>();
        devotions.GetIndexAsync().Returns(Task.FromResult<IReadOnlyList<DevotionSummary>>([]));
        Services.AddSingleton(devotions);
        Services.AddSingleton(new FeatureFlags());
        Services.AddSingleton(Substitute.For<ITextSizeService>());
        Services.AddSingleton(Substitute.For<IThemeService>());
        Services.AddSingleton<QuickMenuLauncher>();
        Services.AddSingleton(new OfflinePacks(new OfflinePacksInterop(JSInterop.JSRuntime), Substitute.For<IHostCapabilities>()));
        Services.AddSingleton(new DialogInterop(JSInterop.JSRuntime));
        return updates;
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Update_action_is_visible_only_when_the_host_supports_it(bool supported)
    {
        var updates = Configure(supported);

        var cut = RenderComponent<QuickMenu>();

        Assert.Equal(supported ? 1 : 0, cut.FindAll("button").Count(button => button.TextContent.Trim() == "Check for updates"));
        _ = updates.DidNotReceive().CheckAsync(Arg.Any<bool>());
    }

    [Fact]
    public async Task Manual_check_closes_the_menu_and_disables_duplicate_requests_until_completion()
    {
        var updates = Configure(true);
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        updates.CheckAsync(true).Returns(_ =>
        {
            started.SetResult();
            return completion.Task;
        });
        var cut = RenderComponent<QuickMenu>();
        cut.Find(".quick-menu__toggle").Click();
        Assert.Equal("true", cut.Find(".quick-menu__toggle").GetAttribute("aria-expanded"));

        var check = cut.FindAll("button").Single(button => button.TextContent.Trim() == "Check for updates");
        var click = check.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        try
        {
            await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.Contains(JSInterop.Invocations, invocation => invocation.Identifier == "respondeoDialog.close");
            Assert.True(cut.FindAll("button").Single(button => button.TextContent.Trim() == "Checking for updates...").HasAttribute("disabled"));
            _ = updates.Received(1).CheckAsync(true);
        }
        finally
        {
            completion.SetResult();
            await click;
        }

        Assert.False(cut.FindAll("button").Single(button => button.TextContent.Trim() == "Check for updates").HasAttribute("disabled"));
        _ = updates.Received(1).CheckAsync(true);
    }
}
