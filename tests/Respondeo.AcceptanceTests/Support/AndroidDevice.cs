using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Respondeo.AcceptanceTests.Support;

internal sealed class AndroidDevice
{
    internal const string Package = "faith.respondeo.app";
    private readonly string _adb;
    private readonly string _serial;
    private readonly string _remoteXml = "/data/local/tmp/respondeo-" + Guid.NewGuid().ToString("N") + ".xml";

    internal string EvidenceDirectory { get; }

    private sealed class AdbCommandException(int exitCode, string message) : InvalidOperationException(message)
    {
        internal int ExitCode { get; } = exitCode;
    }

    internal AndroidDevice()
    {
        _adb = Required("RESPONDEO_ADB_PATH");
        _serial = Required("RESPONDEO_ANDROID_SERIAL");
        if (Environment.GetEnvironmentVariable("RESPONDEO_ANDROID_ALLOW_MUTATION") != "1")
        {
            throw new InvalidOperationException("Set RESPONDEO_ANDROID_ALLOW_MUTATION=1 only for a dedicated test emulator/device.");
        }
        if (!_serial.StartsWith("emulator-", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Automated lifecycle tests require a dedicated emulator; use the release checklist on a physical phone.");
        }
        var root = Required("RESPONDEO_ANDROID_RESULTS");
        EvidenceDirectory = Path.Combine(Path.GetFullPath(root), DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff"), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(EvidenceDirectory);
    }

    internal static string Required(string name) => Environment.GetEnvironmentVariable(name) is { Length: > 0 } value && !string.IsNullOrWhiteSpace(value)
        ? value : throw new InvalidOperationException($"Set {name} explicitly before running Category=AndroidDevice tests.");

    internal Task<string> RunAsync(params string[] args) => ExecuteAsync(false, args);

    private async Task<string> ExecuteAsync(bool includeError, params string[] args)
    {
        var start = new ProcessStartInfo(_adb) { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
        start.ArgumentList.Add("-s");
        start.ArgumentList.Add(_serial);
        foreach (var argument in args)
        {
            start.ArgumentList.Add(argument);
        }
        using var process = Process.Start(start) ?? throw new InvalidOperationException("Unable to start adb.");
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(90));
        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync();
            throw new TimeoutException($"adb {string.Join(' ', args)} timed out: {await stderr}");
        }
        var output = await stdout;
        var error = await stderr;
        if (process.ExitCode != 0)
        {
            throw new AdbCommandException(process.ExitCode, $"adb {string.Join(' ', args)} failed ({process.ExitCode}): {output}\n{error}");
        }
        return (includeError ? output + "\n" + error : output).Trim();
    }

    internal async Task InstallAsync(string apk)
    {
        if (!File.Exists(apk))
        {
            throw new FileNotFoundException("Selected APK does not exist.", apk);
        }
        var result = await RunAsync("install", "-r", apk);
        if (!result.Contains("Success", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("APK installation failed: " + result);
        }
    }

    internal async Task LaunchAsync(bool cold = false)
    {
        if (cold)
        {
            await RunAsync("shell", "am", "force-stop", Package);
        }
        await RunAsync("shell", "input", "keyevent", "224");
        await RunAsync("shell", "wm", "dismiss-keyguard");
        var resolution = await RunAsync("shell", "cmd", "package", "resolve-activity", "--brief", Package);
        var component = resolution.Split('\n').Select(line => line.Trim()).SingleOrDefault(line => line.StartsWith(Package + "/", StringComparison.Ordinal))
            ?? throw new InvalidOperationException("APK launcher could not be resolved: " + resolution);
        var result = await ExecuteAsync(true, "shell", "am", "start", "-n", component);
        if (!result.Contains("Starting: Intent", StringComparison.Ordinal) || result.Contains("Error:", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("APK launch failed: " + result);
        }
        await WaitAsync("Open the quick menu", seconds: 90);
    }

    internal async Task<XDocument> HierarchyAsync()
    {
        var deadline = Stopwatch.StartNew();
        string result;
        do
        {
            await RunAsync("shell", "rm", "-f", _remoteXml);
            try
            {
                result = await ExecuteAsync(true, "shell", "uiautomator", "dump", _remoteXml);
            }
            catch (AdbCommandException exception) when (exception.ExitCode == 137)
            {
                result = exception.Message;
                await File.AppendAllTextAsync(Path.Combine(EvidenceDirectory, "hierarchy-dump.txt"), result + Environment.NewLine);
                await Task.Delay(1000);
                continue;
            }
            await File.AppendAllTextAsync(Path.Combine(EvidenceDirectory, "hierarchy-dump.txt"), result + Environment.NewLine);
            if (result.Contains("dumped to: " + _remoteXml, StringComparison.Ordinal))
            {
                var local = Path.Combine(EvidenceDirectory, "hierarchy.xml");
                await RunAsync("pull", _remoteXml, local);
                return XDocument.Load(local);
            }
            if (!result.Contains("null root node", StringComparison.Ordinal) && !result.Contains("could not get idle state", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("UIAutomator did not produce a hierarchy: " + result);
            }
            await Task.Delay(250);
        } while (deadline.Elapsed < TimeSpan.FromSeconds(30));
        throw new TimeoutException("UIAutomator did not become ready: " + result);
    }

    internal async Task<XElement> WaitAsync(string label, bool contains = false, int seconds = 60, Func<XElement, bool>? predicate = null)
    {
        var deadline = Stopwatch.StartNew();
        while (deadline.Elapsed < TimeSpan.FromSeconds(seconds))
        {
            var document = await HierarchyAsync();
            var match = document.Descendants("node").FirstOrDefault(node =>
                (string?)node.Attribute("package") == Package && HasLabel(node, label, contains)
                && IsVisible(node) && (predicate is null || predicate(node)));
            if (match is not null)
            {
                return match;
            }
            await Task.Delay(250);
        }
        throw new TimeoutException($"Android UI did not expose '{label}' in the expected state within {seconds}s. Evidence: {EvidenceDirectory}");
    }

    internal static bool HasLabel(XElement node, string label, bool contains = false) => new[] { "text", "content-desc" }.Any(attribute =>
        contains ? ((string?)node.Attribute(attribute))?.Contains(label, StringComparison.OrdinalIgnoreCase) == true
            : string.Equals(((string?)node.Attribute(attribute))?.Trim(), label, StringComparison.OrdinalIgnoreCase));

    internal static bool IsVisible(XElement node)
    {
        var bounds = ParseBounds((string?)node.Attribute("bounds") ?? "");
        return bounds.Right > bounds.Left && bounds.Bottom > bounds.Top;
    }

    internal async Task TapAsync(string label, bool contains = false)
    {
        var node = await WaitAsync(label, contains);
        var bounds = ParseBounds((string?)node.Attribute("bounds") ?? "");
        await RunAsync("shell", "input", "tap", ((bounds.Left + bounds.Right) / 2).ToString(), ((bounds.Top + bounds.Bottom) / 2).ToString());
    }

    internal async Task<XElement> ScrollToAsync(string label, bool contains = false)
    {
        for (var attempt = 0; attempt < 10; attempt++)
        {
            var document = await HierarchyAsync();
            var webView = document.Descendants("node").First(node =>
                (string?)node.Attribute("package") == Package && (string?)node.Attribute("class") == "android.webkit.WebView");
            var viewport = ParseBounds((string?)webView.Attribute("bounds") ?? "");
            var match = document.Descendants("node").FirstOrDefault(node =>
            {
                if ((string?)node.Attribute("package") != Package || !HasLabel(node, label, contains) || !IsVisible(node))
                {
                    return false;
                }
                var bounds = ParseBounds((string?)node.Attribute("bounds") ?? "");
                return bounds.Top > viewport.Top + 100 && bounds.Bottom < viewport.Bottom - 200;
            });
            if (match is not null)
            {
                return match;
            }
            var x = ((viewport.Left + viewport.Right) / 2).ToString();
            var height = viewport.Bottom - viewport.Top;
            await RunAsync("shell", "input", "swipe", x, (viewport.Top + height * 3 / 4).ToString(),
                x, (viewport.Top + height / 3).ToString(), "500");
        }
        throw new TimeoutException($"Could not scroll '{label}' into the Android viewport. Evidence: {EvidenceDirectory}");
    }

    internal static (int Left, int Top, int Right, int Bottom) ParseBounds(string value)
    {
        var match = Regex.Match(value, @"^\[(\d+),(\d+)\]\[(\d+),(\d+)\]$");
        if (!match.Success)
        {
            throw new InvalidOperationException("Invalid Android accessibility bounds: " + value);
        }
        var bounds = (Left: int.Parse(match.Groups[1].Value), Top: int.Parse(match.Groups[2].Value),
            Right: int.Parse(match.Groups[3].Value), Bottom: int.Parse(match.Groups[4].Value));
        if (bounds.Right < bounds.Left || bounds.Bottom < bounds.Top)
        {
            throw new InvalidOperationException("Reversed Android accessibility bounds: " + value);
        }
        return bounds;
    }

    internal async Task CaptureAsync(string name)
    {
        var prefix = Path.Combine(EvidenceDirectory, name);
        // Independent captures keep a missing accessibility tree from hiding the screen or app logs.
        await Task.WhenAll(CaptureHierarchyAsync(prefix), CaptureScreenAsync(prefix), CaptureLogAsync());
    }

    private async Task CaptureHierarchyAsync(string prefix) =>
        await File.WriteAllTextAsync(prefix + ".xml", (await HierarchyAsync()).ToString());

    private async Task CaptureScreenAsync(string prefix)
    {
        var remote = "/data/local/tmp/respondeo-" + Guid.NewGuid().ToString("N") + ".png";
        try
        {
            await RunAsync("shell", "screencap", "-p", remote);
            await RunAsync("pull", remote, prefix + ".png");
        }
        finally
        {
            await RunAsync("shell", "rm", "-f", remote);
        }
    }

    internal async Task RecordEnvironmentAsync(string apk, string prefix = "")
    {
        using var archive = System.IO.Compression.ZipFile.OpenRead(apk);
        var entry = archive.GetEntry("assets/wwwroot/build-manifest.json")
            ?? throw new InvalidOperationException("Selected APK has no build identity.");
        using var stream = entry.Open();
        using var reader = new StreamReader(stream);
        await File.WriteAllTextAsync(Path.Combine(EvidenceDirectory, prefix + "build-manifest.json"), await reader.ReadToEndAsync());
        await using var file = File.OpenRead(apk);
        var hash = Convert.ToHexString(await System.Security.Cryptography.SHA256.HashDataAsync(file)).ToLowerInvariant();
        var environment = new
        {
            serial = _serial,
            apkSha256 = hash,
            api = await RunAsync("shell", "getprop", "ro.build.version.sdk"),
            fingerprint = await RunAsync("shell", "getprop", "ro.build.fingerprint"),
            webView = await RunAsync("shell", "dumpsys", "webviewupdate")
        };
        await File.WriteAllTextAsync(Path.Combine(EvidenceDirectory, prefix + "environment.json"),
            JsonSerializer.Serialize(environment, new JsonSerializerOptions { WriteIndented = true }));
    }

    internal async Task CaptureLogAsync()
    {
        var pid = await RunAsync("shell", "pidof", Package);
        await File.WriteAllTextAsync(Path.Combine(EvidenceDirectory, "app-logcat.txt"), await RunAsync("logcat", "-d", "-t", "500", "--pid=" + pid));
    }

    internal Task CleanupAsync() => RunAsync("shell", "rm", "-f", _remoteXml);
}
