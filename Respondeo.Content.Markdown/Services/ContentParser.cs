using Markdig;
using Respondeo.Content.Abstractions;
using Respondeo.Content.Markdown.Internal;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Respondeo.Content.Markdown.Services;

/// <summary>
/// Turns a raw Markdown file (with a "---" delimited YAML front-matter block) into a <see cref="ContentNode"/>.
/// Owns the YAML deserializer and the Markdig pipeline configuration; performs no I/O so it can be tested in isolation.
/// </summary>
internal sealed class ContentParser
{
    private readonly IDeserializer _yaml = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    private readonly MarkdownPipeline _markdown = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Use<ContentContainerExtension>()
        .Build();

    /// <summary>
    /// Parses raw file content into a node, or returns null when there is no valid front-matter
    /// block or the front-matter lacks an id.
    /// </summary>
    public ContentNode? Parse(string raw)
    {
        var (frontMatter, body) = SplitFrontMatter(raw);
        if (frontMatter is null)
        {
            return null;
        }

        var meta = _yaml.Deserialize<ContentFrontMatter>(frontMatter);
        if (meta is null || string.IsNullOrWhiteSpace(meta.Id))
        {
            return null;
        }

        var html = Markdig.Markdown.ToHtml(body, _markdown);
        return new ContentNode
        {
            Id = meta.Id,
            Title = meta.Title,
            Summary = meta.Summary,
            BodyHtml = html,
            IsEntryPoint = meta.IsEntryPoint,
            Audiences = meta.Audiences,
            Branches = [.. meta.Branches.Select(b => new BranchLink { To = b.To, Label = b.Label, Prompt = b.Prompt })],
            Sections = meta.Sections,
        };
    }

    /// <summary>
    /// Splits a "---" delimited YAML front-matter block from the Markdown body.
    /// Returns (null, raw) when no front-matter block is present.
    /// </summary>
    internal static (string? FrontMatter, string Body) SplitFrontMatter(string raw)
    {
        var text = raw.Replace("\r\n", "\n").TrimStart('\uFEFF', ' ', '\n');
        if (!text.StartsWith("---\n"))
        {
            return (null, raw);
        }

        var end = text.IndexOf("\n---", 4, StringComparison.Ordinal);
        if (end < 0)
        {
            return (null, raw);
        }

        var frontMatter = text.Substring(4, end - 4);
        var bodyStart = text.IndexOf('\n', end + 1);
        var body = bodyStart < 0 ? string.Empty : text[(bodyStart + 1)..];
        return (frontMatter, body);
    }
}
