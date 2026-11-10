using System.Net;

namespace Respondeo.Maui;

/// <summary>
/// Serves <see cref="HttpClient"/> requests from the bundled wwwroot assets. In Blazor Hybrid, .NET-side HTTP calls
/// bypass the WebView, so the virtual https://0.0.0.0/ origin is unreachable over the network.
/// </summary>
internal sealed class AppPackageHttpHandler : HttpMessageHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var path = Uri.UnescapeDataString(request.RequestUri?.AbsolutePath.TrimStart('/') ?? string.Empty);

        try
        {
            using var stream = await FileSystem.OpenAppPackageFileAsync($"wwwroot/{path}");
            var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer, cancellationToken);
            buffer.Position = 0;

            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(buffer), RequestMessage = request };
        }
        catch (Exception ex) when (ex is IOException or Java.IO.IOException)
        {
            return new HttpResponseMessage(HttpStatusCode.NotFound) { RequestMessage = request };
        }
    }
}
