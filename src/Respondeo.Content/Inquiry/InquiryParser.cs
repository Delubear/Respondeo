using Respondeo.Content.Infrastructure;
using Respondeo.Content.Rendering;

namespace Respondeo.Content.Inquiry;

/// <summary>
/// Turns a raw Markdown file (with a "---" delimited YAML front-matter block) into a <see cref="InquiryNodeDocument"/>.
/// Front-matter reading is delegated to the shared <see cref="FrontMatterReader"/> and HTML rendering to the injected <see cref="IContentHtmlRenderer"/>
/// so the Markdown engine stays behind an abstraction. Performs no I/O so it can be tested in isolation.
/// </summary>
internal sealed class InquiryParser(IContentHtmlRenderer html)
{
    private readonly FrontMatterReader _reader = new();

    /// <summary>
    /// Parses raw file content into a node, or returns null when there is no valid front-matter block or the front-matter lacks an id.
    /// The <paramref name="stage"/> is supplied by the loader (derived from the file's content folder) rather than authored in front matter.
    /// </summary>
    public InquiryNodeDocument? Parse(string raw, string? stage = null) => _reader.TryRead<InquiryFrontMatter>(raw, out var meta, out var body) ? Map(meta!, body, stage) : null;

    /// <summary>
    /// Maps an already-parsed front-matter block and body onto a <see cref="InquiryNodeDocument"/>.
    /// Shared by <see cref="Parse"/> and the content loader so both produce identical nodes from the same input.
    /// </summary>
    public InquiryNodeDocument Map(InquiryFrontMatter meta, string body, string? stage) => new()
    {
        Id = meta.Id,
        Title = meta.Title,
        Summary = meta.Summary,
        BodyHtml = html.ToHtml(body),
        Tags = meta.Tags,
        Sections = meta.Sections,
        Stage = stage,
    };
}
