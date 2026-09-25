using Respondeo.Content.Abstractions;
using Respondeo.Content.Markdown.Internal;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Respondeo.Content.Markdown.Services;

/// <summary>
/// Turns a raw Markdown file (with a "---" delimited YAML front-matter block) into a <see cref="ContentNode"/>.
/// Owns the YAML deserializer; HTML rendering is delegated to the injected <see cref="IContentHtmlRenderer"/> so the
/// Markdown engine stays behind an abstraction. Performs no I/O so it can be tested in isolation.
/// </summary>
internal sealed class ContentParser
{
    private readonly IContentHtmlRenderer _html;

    public ContentParser(IContentHtmlRenderer html) => _html = html;

    private readonly IDeserializer _yaml = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    /// <summary>
    /// Parses raw file content into a node, or returns null when there is no valid front-matter
    /// block or the front-matter lacks an id. The <paramref name="stage"/> is supplied by the
    /// loader (derived from the file's content folder) rather than authored in front matter.
    /// </summary>
    public ContentNode? Parse(string raw, string? stage = null)
    {
        var (frontMatter, body) = FrontMatter.Split(raw);
        if (frontMatter is null)
        {
            return null;
        }

        var meta = _yaml.Deserialize<ContentFrontMatter>(frontMatter);
        if (meta is null || string.IsNullOrWhiteSpace(meta.Id))
        {
            return null;
        }

        var html = _html.ToHtml(body);
        return new ContentNode
        {
            Id = meta.Id,
            Title = meta.Title,
            Summary = meta.Summary,
            BodyHtml = html,
            Topics = meta.Topics,
            Branches = [.. meta.Branches.Select(b => new BranchLink { To = b.To, Label = b.Label, Prompt = b.Prompt })],
            Sections = meta.Sections,
            NextStage = meta.NextStage is null
                ? null
                : new StageLink { Href = meta.NextStage.Href, Label = meta.NextStage.Label, Prompt = meta.NextStage.Prompt, Icon = meta.NextStage.Icon },
            Stage = stage,
        };
    }

    /// <summary>
    /// Splits a "---" delimited YAML front-matter block from the Markdown body.
    /// Returns (null, raw) when no front-matter block is present.
    /// </summary>
    internal static (string? FrontMatter, string Body) SplitFrontMatter(string raw) => FrontMatter.Split(raw);
}
