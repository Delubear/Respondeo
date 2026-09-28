using Respondeo.Content.Abstractions;
using Respondeo.Content.Markdown.Internal;

namespace Respondeo.Content.Markdown.Services;

/// <summary>
/// Turns a raw Markdown file (with a "---" delimited YAML front-matter block) into a <see cref="ContentNode"/>.
/// Front-matter reading is delegated to the shared <see cref="FrontMatterReader"/> and HTML rendering to the
/// injected <see cref="IContentHtmlRenderer"/> so the Markdown engine stays behind an abstraction. Performs no
/// I/O so it can be tested in isolation.
/// </summary>
internal sealed class ContentParser
{
    private readonly IContentHtmlRenderer _html;
    private readonly FrontMatterReader _reader = new();

    public ContentParser(IContentHtmlRenderer html) => _html = html;

    /// <summary>
    /// Parses raw file content into a node, or returns null when there is no valid front-matter
    /// block or the front-matter lacks an id. The <paramref name="stage"/> is supplied by the
    /// loader (derived from the file's content folder) rather than authored in front matter.
    /// </summary>
    public ContentNode? Parse(string raw, string? stage = null) =>
        _reader.TryRead<ContentFrontMatter>(raw, out var meta, out var body) ? Map(meta!, body, stage) : null;

    /// <summary>
    /// Maps an already-parsed front-matter block and body onto a <see cref="ContentNode"/>. Shared by
    /// <see cref="Parse"/> and the content loader so both produce identical nodes from the same input.
    /// </summary>
    public ContentNode Map(ContentFrontMatter meta, string body, string? stage) => new()
    {
        Id = meta.Id,
        Title = meta.Title,
        Summary = meta.Summary,
        BodyHtml = _html.ToHtml(body),
        Tags = meta.Tags,
        Branches = [.. meta.Branches.Select(b => new BranchLink { To = b.To, Label = b.Label, Prompt = b.Prompt })],
        Sections = meta.Sections,
        NextStage = meta.NextStage is null
            ? null
            : new StageLink { Href = meta.NextStage.Href, Label = meta.NextStage.Label, Prompt = meta.NextStage.Prompt, Icon = meta.NextStage.Icon },
        Stage = stage,
    };
}
