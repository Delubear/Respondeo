using System.Runtime.ExceptionServices;
using Respondeo.AcceptanceTests.Support;
using Xunit;
using Xunit.Abstractions;

namespace Respondeo.AcceptanceTests;

[CollectionDefinition("Android device", DisableParallelization = true)]
public sealed class AndroidDeviceCollection;

[Collection("Android device")]
[Trait("Category", "AndroidDevice")]
public sealed class AndroidLifecycleTests(ITestOutputHelper output)
{
    [Fact]
    public Task Release_apk_starts_offline_and_retains_preferences_across_restarts_and_resume() => WithEvidenceAsync(async device =>
    {
        var apk = AndroidDevice.Required("RESPONDEO_APK_PATH");
        await device.RecordEnvironmentAsync(apk);
        await device.InstallAsync(apk);
        await WithAirplaneModeAsync(device, async () =>
        {
            await device.LaunchAsync(cold: true);
            await device.WaitAsync("One question leads to the next");
            var textHeight = await SetPreferencesAsync(device);
            await PinMiraclesAsync(device);
            await device.CaptureAsync("offline-home-dark");
            await device.LaunchAsync(cold: true);
            await AssertPreferencesAsync(device, textHeight);
            await device.RunAsync("shell", "input", "keyevent", "3");
            await device.LaunchAsync();
            await AssertPreferencesAsync(device, textHeight);
            await device.WaitAsync("Show the next stage");
            await device.CaptureAsync("resumed-dark");
            await BrowseMiraclesAsync(device);
            await device.RunAsync("shell", "input", "keyevent", "4");
            await device.LaunchAsync();
            await AssertPreferencesAsync(device, textHeight);
        });
    });

    [Fact]
    [Trait("Category", "AndroidUpgrade")]
    public Task Older_same_key_apk_upgrades_in_place_without_losing_reader_preferences() => WithEvidenceAsync(async device =>
    {
        var baseline = AndroidDevice.Required("RESPONDEO_ANDROID_BASELINE_APK");
        var candidate = AndroidDevice.Required("RESPONDEO_APK_PATH");
        await device.RecordEnvironmentAsync(candidate);
        await device.RecordEnvironmentAsync(baseline, "baseline-");
        await device.InstallAsync(baseline);
        await device.LaunchAsync(cold: true);
        await device.WaitAsync("One question leads to the next");
        var textHeight = await SetPreferencesAsync(device);
        await PinMiraclesAsync(device);
        var before = await InstalledVersionAsync(device);
        await device.RunAsync("shell", "am", "force-stop", AndroidDevice.Package);
        await device.InstallAsync(candidate);
        var after = await InstalledVersionAsync(device);
        await File.WriteAllTextAsync(Path.Combine(device.EvidenceDirectory, "upgrade-versions.json"),
            System.Text.Json.JsonSerializer.Serialize(new { baselineVersionCode = before, candidateVersionCode = after }));
        Assert.True(after > before, $"Upgrade requires a strictly newer version code: baseline={before}, candidate={after}.");
        await device.LaunchAsync(cold: true);
        await AssertPreferencesAsync(device, textHeight);
        await device.CaptureAsync("upgraded-preferences");
    });

    private static async Task<long> InstalledVersionAsync(AndroidDevice device)
    {
        var package = await device.RunAsync("shell", "dumpsys", "package", AndroidDevice.Package);
        var match = System.Text.RegularExpressions.Regex.Match(package, @"\bversionCode=(\d+)");
        Assert.True(match.Success, "Installed package did not report a version code.");
        return long.Parse(match.Groups[1].Value);
    }

    private const string Lede = "Reason moves from one honest question";

    private static int Height(System.Xml.Linq.XElement node)
    {
        var bounds = AndroidDevice.ParseBounds((string?)node.Attribute("bounds") ?? "");
        return bounds.Bottom - bounds.Top;
    }

    private static async Task<int> SetPreferencesAsync(AndroidDevice device)
    {
        await device.TapAsync("Open the quick menu");
        await device.TapAsync("Default text");
        await device.TapAsync("Light");
        await device.TapAsync("Close");
        await device.WaitAsync("Switch to dark theme");
        var originalHeight = Height(await device.WaitAsync(Lede, contains: true));
        await device.RunAsync("shell", "input", "keyevent", "3");
        await device.LaunchAsync();
        await device.WaitAsync("Switch to dark theme");
        await device.WaitAsync("Show the next stage");
        await device.CaptureAsync("home-light-default");
        await device.TapAsync("Open the quick menu");
        await device.TapAsync("Large text");
        await device.TapAsync("Dark");
        await device.TapAsync("Close");
        await device.WaitAsync("Switch to light theme");
        var enlarged = await device.WaitAsync(Lede, contains: true, predicate: node => Height(node) > originalHeight * 1.1);
        return Height(enlarged);
    }

    private static async Task AssertPreferencesAsync(AndroidDevice device, int textHeight)
    {
        await device.WaitAsync("One question leads to the next");
        await device.WaitAsync("Switch to light theme");
        var lede = await device.WaitAsync(Lede, contains: true, predicate: node => Math.Abs(Height(node) - textHeight) <= 2);
        Assert.InRange(Height(lede), textHeight - 2, textHeight + 2);
        await device.TapAsync("Open the quick menu");
        await device.WaitAsync("Miracles");
        await device.TapAsync("Close");
    }

    private static async Task PinMiraclesAsync(AndroidDevice device)
    {
        await device.TapAsync("Open the quick menu");
        await device.TapAsync("Customize");
        await device.ScrollToAsync("Miracles", contains: true);
        var hierarchy = await device.HierarchyAsync();
        if (!hierarchy.Descendants("node").Any(node => AndroidDevice.HasLabel(node, "Unpin Miracles")))
        {
            await device.TapAsync("Pin Miracles");
        }
        await device.WaitAsync("Unpin Miracles");
        await device.TapAsync("Done");
        await device.WaitAsync("Miracles");
        await device.TapAsync("Close");
    }

    private static async Task BrowseMiraclesAsync(AndroidDevice device)
    {
        const string article = "The Eucharistic Miracle of Lanciano";
        await device.TapAsync("Open the quick menu");
        await device.TapAsync("Miracles");
        var heading = await device.WaitAsync("Catholic Miracles");
        var hierarchy = await device.HierarchyAsync();
        var webView = hierarchy.Descendants("node").First(node => (string?)node.Attribute("class") == "android.webkit.WebView");
        var viewport = AndroidDevice.ParseBounds((string?)webView.Attribute("bounds") ?? "");
        Assert.InRange(AndroidDevice.ParseBounds((string?)heading.Attribute("bounds") ?? "").Top,
            viewport.Top, viewport.Top + (viewport.Bottom - viewport.Top) / 3);
        var card = await device.ScrollToAsync(article, contains: true);
        var originalTop = AndroidDevice.ParseBounds((string?)card.Attribute("bounds") ?? "").Top;
        await device.CaptureAsync("miracles-scrolled");
        for (var attempt = 0; attempt < 2; attempt++)
        {
            await device.TapAsync(article, contains: true);
            await device.WaitAsync(article);
            await device.WaitAsync("Miracle tools");
            await device.CaptureAsync("miracle-detail-" + attempt);
            await device.TapAsync("Miracles");
            var restored = await device.WaitAsync(article, contains: true, predicate: node =>
                Math.Abs(AndroidDevice.ParseBounds((string?)node.Attribute("bounds") ?? "").Top - originalTop) <= 12);
            Assert.InRange(AndroidDevice.ParseBounds((string?)restored.Attribute("bounds") ?? "").Top, originalTop - 12, originalTop + 12);
        }
        await device.CaptureAsync("miracles-restored");
        await device.TapAsync("Back to top");
        await device.TapAsync("RESPONDEO");
        await device.WaitAsync("One question leads to the next");
    }

    private static async Task WithAirplaneModeAsync(AndroidDevice device, Func<Task> scenario)
    {
        var airplane = await device.RunAsync("shell", "settings", "get", "global", "airplane_mode_on");
        var wifi = await device.RunAsync("shell", "settings", "get", "global", "wifi_on");
        Assert.Contains(airplane, new[] { "0", "1" });
        Assert.Contains(wifi, new[] { "0", "1" });
        try
        {
            await device.RunAsync("shell", "cmd", "connectivity", "airplane-mode", "enable");
            await device.RunAsync("shell", "svc", "wifi", "disable");
            Assert.Equal("1", await device.RunAsync("shell", "settings", "get", "global", "airplane_mode_on"));
            Assert.Equal("0", await device.RunAsync("shell", "settings", "get", "global", "wifi_on"));
            await scenario();
        }
        finally
        {
            await device.RunAsync("shell", "cmd", "connectivity", "airplane-mode", airplane == "1" ? "enable" : "disable");
            await device.RunAsync("shell", "svc", "wifi", wifi == "1" ? "enable" : "disable");
        }
    }

    private async Task WithEvidenceAsync(Func<AndroidDevice, Task> scenario)
    {
        var device = new AndroidDevice();
        output.WriteLine("Android evidence: " + device.EvidenceDirectory);
        Exception? failure = null;
        try
        {
            await scenario(device);
        }
        catch (Exception exception)
        {
            failure = exception;
            try
            {
                await device.CaptureAsync("failure");
            }
            catch (Exception captureFailure)
            {
                failure = new AggregateException("Android scenario and failure capture both failed.", exception, captureFailure);
            }
        }
        finally
        {
            try
            {
                await device.CleanupAsync();
            }
            catch (Exception cleanupFailure)
            {
                failure = failure is null ? cleanupFailure
                    : new AggregateException("Android verification and cleanup both failed.", failure, cleanupFailure);
            }
        }
        if (failure is not null)
        {
            ExceptionDispatchInfo.Capture(failure).Throw();
        }
    }
}
