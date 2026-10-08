using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Respondeo.Content.Infrastructure;

/// <summary>
/// Shared reader that turns a raw Markdown file into its typed front-matter DTO and body.
/// Owns the camelCase YAML deserializer so every pillar's parser deserializes front matter identically,
/// and enforces the shared rule that a content item is only valid when it declares a non-blank id.
/// Performs no I/O so parsers using it stay unit-testable in isolation.
/// </summary>
public sealed class FrontMatterReader
{
    private static readonly IDeserializer Yaml = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    /// <summary>
    /// Splits and deserializes the "---" delimited YAML front-matter block into <typeparamref name="T"/>.
    /// Returns false (with null <paramref name="meta"/>) when there is no valid front-matter block, the front matter is malformed YAML,
    /// or it lacks an id; otherwise returns true with the parsed metadata and the Markdown body.
    /// Malformed front matter degrades to false so a single bad file never aborts the whole catalog load.
    /// </summary>
    public bool TryRead<T>(string raw, out T? meta, out string body) where T : ContentFrontMatterBase
    {
        var (frontMatter, parsedBody) = FrontMatter.Split(raw);
        body = parsedBody;
        if (frontMatter is null)
        {
            meta = null;
            return false;
        }

        try
        {
            meta = Yaml.Deserialize<T>(frontMatter);
        }
        catch (YamlException)
        {
            meta = null;
            return false;
        }

        if (meta is null || string.IsNullOrWhiteSpace(meta.Id))
        {
            meta = null;
            return false;
        }

        return true;
    }
}
