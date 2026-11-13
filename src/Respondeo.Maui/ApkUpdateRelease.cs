using System.Security.Cryptography;
using System.Text.Json;

namespace Respondeo.Maui;

internal sealed record ApkUpdateRelease(int VersionCode, string Version, string PackageId, string Url, string Sha256, long SizeBytes)
{
    internal const string ManifestUrl = "https://github.com/Delubear/Respondeo/releases/latest/download/android-update.json";
    internal const long MaximumApkBytes = 512L * 1024 * 1024;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    internal static ApkUpdateRelease Parse(string json)
    {
        var release = JsonSerializer.Deserialize<ApkUpdateRelease>(json, JsonOptions)
            ?? throw new InvalidDataException("The Android update manifest is empty.");
        if (release.VersionCode is < 1 or > 2100000000 || release.Version != $"1.0.{release.VersionCode}" ||
            release.PackageId != "faith.respondeo.app" || release.SizeBytes is <= 0 or > MaximumApkBytes ||
            release.Url != $"https://github.com/Delubear/Respondeo/releases/download/v{release.Version}/respondeo-{release.Version}.apk" ||
            release.Sha256 is not { Length: 64 } || !release.Sha256.All(char.IsAsciiHexDigit))
        {
            throw new InvalidDataException("The Android update manifest contains invalid release information.");
        }
        return release;
    }

    internal bool IsNewerThan(long installedVersionCode) => VersionCode > installedVersionCode;

    internal async Task DownloadAsync(HttpClient client, string path, IProgress<double> progress, CancellationToken cancellationToken)
    {
        var complete = false;
        try
        {
            using var response = await client.GetAsync(Url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();
            if (response.RequestMessage?.RequestUri?.Scheme != Uri.UriSchemeHttps ||
                response.Content.Headers.ContentLength is long length && length != SizeBytes)
            {
                throw new InvalidDataException("The APK download does not match the advertised release.");
            }
            await using var source = await response.Content.ReadAsStreamAsync(cancellationToken);
            await using var destination = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true);
            using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            var buffer = new byte[81920];
            long received = 0;
            int count;
            while ((count = await source.ReadAsync(buffer, cancellationToken)) != 0)
            {
                received += count;
                if (received > SizeBytes)
                {
                    throw new InvalidDataException("The APK download exceeds its advertised size.");
                }
                hash.AppendData(buffer, 0, count);
                await destination.WriteAsync(buffer.AsMemory(0, count), cancellationToken);
                progress.Report((double)received / SizeBytes);
            }
            if (received != SizeBytes || !Convert.ToHexString(hash.GetHashAndReset()).Equals(Sha256, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException("The APK download failed its SHA-256 integrity check.");
            }
            complete = true;
        }
        finally
        {
            if (!complete && File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
