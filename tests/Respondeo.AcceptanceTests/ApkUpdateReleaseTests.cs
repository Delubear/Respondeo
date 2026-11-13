using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Respondeo.Maui;
using Xunit;

namespace Respondeo.AcceptanceTests;

public sealed class ApkUpdateReleaseTests
{
    private static readonly byte[] Apk = Encoding.UTF8.GetBytes("Invented APK fixture, not an executable");

    [Theory]
    [InlineData(41, true)]
    [InlineData(42, false)]
    [InlineData(43, false)]
    public void Offers_only_a_strictly_newer_version(int installed, bool expected)
    {
        var release = ApkUpdateRelease.Parse(Manifest().ToJsonString());

        Assert.Equal(expected, release.IsNewerThan(installed));
        Assert.Equal("1.0.42", release.Version);
        Assert.Equal("faith.respondeo.app", release.PackageId);
    }

    [Theory]
    [InlineData("versionCode", "0")]
    [InlineData("versionCode", "2100000001")]
    [InlineData("version", "\"1.0.43\"")]
    [InlineData("packageId", "\"other.app\"")]
    [InlineData("url", "\"https://example.com/respondeo.apk\"")]
    [InlineData("url", "\"http://github.com/Delubear/Respondeo/releases/download/v1.0.42/respondeo-1.0.42.apk\"")]
    [InlineData("url", "\"https://github.com/Delubear/Respondeo/releases/download/v1.0.41/respondeo-1.0.41.apk\"")]
    [InlineData("sha256", "\"bad\"")]
    [InlineData("sha256", "null")]
    [InlineData("sizeBytes", "0")]
    [InlineData("sizeBytes", "536870913")]
    public void Rejects_invalid_release_identity_or_integrity_metadata(string property, string jsonValue)
    {
        var manifest = Manifest();
        manifest[property] = JsonNode.Parse(jsonValue);

        Assert.Throws<InvalidDataException>(() => ApkUpdateRelease.Parse(manifest.ToJsonString()));
    }

    [Fact]
    public void Rejects_malformed_and_empty_manifests()
    {
        Assert.Throws<JsonException>(() => ApkUpdateRelease.Parse("{"));
        Assert.Throws<InvalidDataException>(() => ApkUpdateRelease.Parse("null"));
        Assert.Throws<InvalidDataException>(() => ApkUpdateRelease.Parse("{}"));
    }

    [Fact]
    public async Task Downloads_exact_verified_bytes_and_reports_completion()
    {
        var release = ApkUpdateRelease.Parse(Manifest().ToJsonString());
        using var client = Client(Apk, release.SizeBytes);
        var path = Path.GetTempFileName();
        double progress = 0;
        try
        {
            await release.DownloadAsync(client, path, new InlineProgress(value => progress = value), CancellationToken.None);

            Assert.Equal(Apk, await File.ReadAllBytesAsync(path));
            Assert.Equal(1, progress);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Theory]
    [InlineData("corrupt")]
    [InlineData("truncated")]
    [InlineData("oversized")]
    [InlineData("wrong-length-header")]
    public async Task Rejects_bad_downloads_and_removes_partial_files(string failure)
    {
        var release = ApkUpdateRelease.Parse(Manifest().ToJsonString());
        var bytes = failure switch
        {
            "corrupt" => new byte[Apk.Length],
            "truncated" => Apk[..^1],
            "oversized" => Apk.Concat(new byte[] { 1 }).ToArray(),
            _ => Apk,
        };
        using var client = Client(bytes, failure == "wrong-length-header" ? 1 : release.SizeBytes);
        var path = Path.GetTempFileName();
        try
        {
            await Assert.ThrowsAsync<InvalidDataException>(() => release.DownloadAsync(client, path, new InlineProgress(_ => { }), CancellationToken.None));

            Assert.False(File.Exists(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task Cancellation_during_streaming_removes_partial_download()
    {
        var bytes = new byte[200000];
        var release = ApkUpdateRelease.Parse(Manifest(bytes).ToJsonString());
        using var client = Client(bytes, release.SizeBytes);
        using var cancellation = new CancellationTokenSource();
        var path = Path.GetTempFileName();
        try
        {
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => release.DownloadAsync(client, path,
                new InlineProgress(_ => cancellation.Cancel()), cancellation.Token));

            Assert.False(File.Exists(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task Http_failure_is_not_treated_as_an_apk()
    {
        using var client = new HttpClient(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)));
        var release = ApkUpdateRelease.Parse(Manifest().ToJsonString());
        var path = Path.GetTempFileName();
        try
        {
            await Assert.ThrowsAsync<HttpRequestException>(() => release.DownloadAsync(client, path, new InlineProgress(_ => { }), CancellationToken.None));

            Assert.False(File.Exists(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static JsonObject Manifest(byte[]? bytes = null)
    {
        bytes ??= Apk;
        return new JsonObject
        {
            ["versionCode"] = 42,
            ["version"] = "1.0.42",
            ["packageId"] = "faith.respondeo.app",
            ["url"] = "https://github.com/Delubear/Respondeo/releases/download/v1.0.42/respondeo-1.0.42.apk",
            ["sha256"] = Convert.ToHexString(SHA256.HashData(bytes)),
            ["sizeBytes"] = bytes.Length,
        };
    }

    private static HttpClient Client(byte[] bytes, long advertisedLength) => new(new StubHandler(_ =>
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(bytes) };
        response.Content.Headers.ContentLength = advertisedLength;
        return response;
    }));

    private sealed class InlineProgress(Action<double> report) : IProgress<double>
    {
        public void Report(double value) => report(value);
    }

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = respond(request);
            response.RequestMessage = request;
            return Task.FromResult(response);
        }
    }
}
