#if APK_UPDATES
using System.Text.Json;
using Android.Content;
using Android.Content.PM;
using Android.Provider;
using Respondeo.Services;
using FileProvider = AndroidX.Core.Content.FileProvider;

namespace Respondeo.Maui;

internal sealed class ApkUpdater : IAppUpdateService
{
    private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromMinutes(10), MaxResponseContentBufferSize = 16384 };
    private const string PendingKey = "apk-update.pending";
    private DateTimeOffset _nextCheck;
    private int _offeredVersion;
    private bool _busy;

    public bool Supported => true;

    public Task CheckAsync(bool userInitiated = false) => MainThread.InvokeOnMainThreadAsync(() =>
    {
        var page = Application.Current?.Windows.FirstOrDefault()?.Page
            ?? throw new InvalidOperationException("The app has no active window for update checks.");
        return CheckAsync(page, userInitiated);
    });

    private async Task CheckAsync(Page page, bool userInitiated)
    {
        if (_busy)
        {
            if (userInitiated)
            {
                await page.DisplayAlertAsync("Update in progress", "An update check or download is already in progress.", "OK");
            }
            return;
        }
        _busy = true;
        var requested = false;
        try
        {
            var pendingJson = Preferences.Default.Get(PendingKey, string.Empty);
            if (pendingJson.Length > 0)
            {
                Preferences.Default.Remove(PendingKey);
                requested = true;
                var pending = ApkUpdateRelease.Parse(pendingJson);
                if (!CanInstall())
                {
                    if (userInitiated)
                    {
                        await page.DisplayAlertAsync("Install permission needed",
                            "Android has not allowed installs from Respondeo. Enable 'Allow from this source' in Android settings, then check for updates again.", "OK");
                    }
                    return;
                }
                if (await page.DisplayAlertAsync("Install update", $"Respondeo {pending.Version} is ready to install.", "Install", "Not now"))
                {
                    Install(pending, DownloadPath);
                }
                return;
            }
            if (!userInitiated && DateTimeOffset.UtcNow < _nextCheck)
            {
                return;
            }
            if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
            {
                if (userInitiated)
                {
                    await page.DisplayAlertAsync("You're offline", "Connect to the internet to check for updates. You can keep reading offline.", "OK");
                }
                return;
            }
            _nextCheck = DateTimeOffset.UtcNow.AddMinutes(15);
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            using var response = await Client.GetAsync(ApkUpdateRelease.ManifestUrl, timeout.Token);
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                // Older releases do not yet carry update metadata.
                if (userInitiated)
                {
                    await page.DisplayAlertAsync("Unable to check for updates", "Update information is not available yet. Please try again later.", "OK");
                }
                return;
            }
            response.EnsureSuccessStatusCode();
            var release = ApkUpdateRelease.Parse(await response.Content.ReadAsStringAsync(timeout.Token));
            _nextCheck = DateTimeOffset.UtcNow.AddHours(6);
            if (!release.IsNewerThan(long.Parse(AppInfo.Current.BuildString, System.Globalization.CultureInfo.InvariantCulture)))
            {
                if (userInitiated)
                {
                    await page.DisplayAlertAsync("You're up to date", "You already have the latest available version of Respondeo.", "OK");
                }
                return;
            }
            if (!userInitiated && _offeredVersion == release.VersionCode)
            {
                return;
            }
            _offeredVersion = release.VersionCode;
            requested = await page.DisplayAlertAsync("Update available",
                $"Respondeo {release.Version} is available. Download the update? Android will ask you to confirm installation. Your reading data will be kept.",
                "Download", "Not now");
            if (!requested)
            {
                return;
            }
            Directory.CreateDirectory(Path.GetDirectoryName(DownloadPath)!);
            if (!await DownloadAsync(page, release, DownloadPath))
            {
                return;
            }
            ValidatePackage(release, DownloadPath);
            if (!CanInstall() && OperatingSystem.IsAndroidVersionAtLeast(26))
            {
                if (await page.DisplayAlertAsync("Allow app updates",
                    "Android needs your permission to install updates from Respondeo. Enable 'Allow from this source' on the next screen, then return here.",
                    "Open settings", "Not now"))
                {
                    Preferences.Default.Set(PendingKey, JsonSerializer.Serialize(release, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
                    var intent = new Intent(Settings.ActionManageUnknownAppSources, Android.Net.Uri.Parse($"package:{AppInfo.Current.PackageName}"));
                    var activity = Platform.CurrentActivity ?? throw new InvalidOperationException("The app is not active.");
                    activity.StartActivity(intent);
                }
                return;
            }
            Install(release, DownloadPath);
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException or InvalidDataException or JsonException or OperationCanceledException or
            Java.Lang.SecurityException or ActivityNotFoundException or InvalidOperationException or FormatException)
        {
            Preferences.Default.Remove(PendingKey);
            Android.Util.Log.Warn("Respondeo.Update", ex.ToString());
            if (requested)
            {
                await page.DisplayAlertAsync("Update not installed", "The update could not be completed. You can keep using Respondeo and try again from the quick menu.", "OK");
            }
            else if (userInitiated)
            {
                await page.DisplayAlertAsync("Unable to check for updates", "The update check could not be completed. Please try again later.", "OK");
            }
        }
        finally
        {
            _busy = false;
        }
    }

    private static async Task<bool> DownloadAsync(Page owner, ApkUpdateRelease release, string path)
    {
        using var cancel = new CancellationTokenSource();
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancel.Token);
        deadline.CancelAfter(TimeSpan.FromMinutes(10));
        var progress = new ProgressBar();
        var status = new Label { Text = $"Downloading Respondeo {release.Version}..." };
        var button = new Button { Text = "Cancel" };
        button.Clicked += (_, _) => cancel.Cancel();
        var sheet = new ContentPage
        {
            Title = "Downloading update",
            Content = new VerticalStackLayout { Padding = 24, Spacing = 20, VerticalOptions = LayoutOptions.Center, Children = { status, progress, button } },
        };
        sheet.Disappearing += (_, _) => cancel.Cancel();
        await owner.Navigation.PushModalAsync(sheet);
        try
        {
            await release.DownloadAsync(Client, path, new Progress<double>(value => progress.Progress = value), deadline.Token);
            return true;
        }
        catch (OperationCanceledException) when (cancel.IsCancellationRequested)
        {
            return false;
        }
        finally
        {
            if (owner.Navigation.ModalStack.Contains(sheet))
            {
                await owner.Navigation.PopModalAsync();
            }
        }
    }

    private static string DownloadPath => Path.Combine(FileSystem.Current.CacheDirectory, "apk-updates", "update.apk");

    private static bool CanInstall() => !OperatingSystem.IsAndroidVersionAtLeast(26) ||
        Platform.AppContext.PackageManager!.CanRequestPackageInstalls();

    private static void Install(ApkUpdateRelease release, string path)
    {
        ValidatePackage(release, path);
        var context = Platform.CurrentActivity ?? throw new InvalidOperationException("The app is not active.");
        var uri = FileProvider.GetUriForFile(context, $"{context.PackageName}.apk-updates", new Java.IO.File(path));
        var intent = new Intent(Intent.ActionView);
        intent.SetDataAndType(uri, "application/vnd.android.package-archive");
        intent.AddFlags(ActivityFlags.GrantReadUriPermission);
        context.StartActivity(intent);
    }

    private static void ValidatePackage(ApkUpdateRelease release, string path)
    {
        var manager = Platform.AppContext.PackageManager!;
        var flags = OperatingSystem.IsAndroidVersionAtLeast(28) ? PackageInfoFlags.SigningCertificates : PackageInfoFlags.Signatures;
        using var archive = manager.GetPackageArchiveInfo(path, flags) ?? throw new InvalidDataException("The download is not an Android package.");
        using var installed = manager.GetPackageInfo(AppInfo.Current.PackageName, flags)
            ?? throw new InvalidDataException("The installed Android package could not be read.");
        if (archive.PackageName != AppInfo.Current.PackageName || archive.VersionName != release.Version ||
            VersionCode(archive) != release.VersionCode || VersionCode(archive) <= VersionCode(installed) ||
            !Signers(archive).SequenceEqual(Signers(installed), StringComparer.Ordinal))
        {
            throw new InvalidDataException("The APK identity, version, or signing certificate does not match this installation.");
        }
    }

    private static long VersionCode(PackageInfo package) => OperatingSystem.IsAndroidVersionAtLeast(28) ? package.LongVersionCode : package.VersionCode;

    private static string[] Signers(PackageInfo package)
    {
        var signatures = OperatingSystem.IsAndroidVersionAtLeast(28) ? package.SigningInfo?.GetApkContentsSigners() : package.Signatures;
        if (signatures is not { Count: > 0 })
        {
            throw new InvalidDataException("The APK has no signing certificates.");
        }
        return signatures.Select(signature => Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(signature.ToByteArray()!)))
            .Order(StringComparer.Ordinal).ToArray();
    }
}
#endif
