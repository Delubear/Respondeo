using Markdig;

namespace Respondeo.Content.Shared;

/// <summary>
/// Markdig-backed implementation of <see cref="IContentHtmlRenderer"/>.
/// Owns the pipeline configuration (advanced extensions plus the shared <see cref="ContentContainerExtension"/> directive vocabulary)
/// so that Markdig stays entirely behind the abstraction and content pillars need no Markdown-engine reference.
/// Stateless and thread-safe, so it can be registered as a singleton.
/// </summary>
internal sealed class MarkdigContentHtmlRenderer : IContentHtmlRenderer
{
    private readonly MarkdownPipeline _pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Use<ContentContainerExtension>()
        .Build();

    // Same pipeline as above, but soft line breaks (single newlines) render as <br /> so
    // authored line breaks in verse-like content such as prayers are preserved.
    private readonly MarkdownPipeline _lineBreakPipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Use<ContentContainerExtension>()
        .UseSoftlineBreakAsHardlineBreak()
        .Build();

    public string ToHtml(string markdown) => Markdown.ToHtml(markdown, _pipeline);

    public string ToHtmlPreservingLineBreaks(string markdown) => Markdown.ToHtml(markdown, _lineBreakPipeline);
}
