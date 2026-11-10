using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Respondeo.Services;

public sealed record BuildInfo(
    string Version,
    string SourceRevision,
    string Host,
    string BuildKind,
    string ContentSha256,
    string HoursMode,
    string HoursRevision)
{
    public static BuildInfo Load(Assembly hostAssembly)
    {
        using var stream = hostAssembly.GetManifestResourceStream("Respondeo.BuildManifest.json") ?? throw new InvalidOperationException("The host build manifest is missing.");
        return JsonSerializer.Deserialize(stream, BuildInfoJsonContext.Default.BuildInfo) ?? throw new InvalidOperationException("The host build manifest is empty.");
    }
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    RespectRequiredConstructorParameters = true, RespectNullableAnnotations = true)]
[JsonSerializable(typeof(BuildInfo))]
internal partial class BuildInfoJsonContext : JsonSerializerContext;
