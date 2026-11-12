using System.Text.Json;
using System.Text.Json.Serialization;

namespace Respondeo.LiturgyOfTheHours;

/// <summary>Strict parsing for repository-authored Office data, retaining comments and trailing commas.</summary>
internal static class OfficeJson
{
    private static readonly JsonSerializerOptions Options = new()
    {
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        PropertyNameCaseInsensitive = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        RespectRequiredConstructorParameters = true,
        Converters = { new JsonStringEnumConverter(allowIntegerValues: false) },
    };

    internal static T Read<T>(Stream stream, string source)
    {
        try
        {
            using var document = JsonDocument.Parse(stream, new()
            {
                CommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true,
            });
            RejectDuplicates(document.RootElement, "$");
            return document.Deserialize<T>(Options) ?? throw new JsonException("The root value must not be null.");
        }
        catch (JsonException error)
        {
            throw new InvalidOperationException($"Invalid Office JSON in '{source}': {error.Message}", error);
        }
    }

    private static void RejectDuplicates(JsonElement element, string path)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var property in element.EnumerateObject())
            {
                if (!names.Add(property.Name))
                {
                    throw new JsonException($"Duplicate property '{property.Name}' at {path} (including case variants).");
                }

                RejectDuplicates(property.Value, path + "." + property.Name);
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            var index = 0;
            foreach (var item in element.EnumerateArray())
            {
                RejectDuplicates(item, $"{path}[{index++}]");
            }
        }
    }
}
