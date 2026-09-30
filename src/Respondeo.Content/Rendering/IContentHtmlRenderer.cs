namespace Respondeo.Content.Rendering;

/// <summary>
/// Contract for turning authored Markdown (including the shared "::: youtube" / "::: pdf" / "::: button" directive vocabulary) into HTML.
/// Kept free of any Markdown-engine types so the abstractions project stays contract-only; the concrete Markdig-based implementation lives in a separate rendering project.
/// </summary>
internal interface IContentHtmlRenderer
{
    /// <summary>Renders the supplied Markdown body to HTML.</summary>
    string ToHtml(string markdown);

    /// <summary>
    /// Renders the supplied Markdown body to HTML, treating a single (soft) line break as a hard
    /// line break so authored line breaks are preserved. Intended for verse-like content such as
    /// prayers, where the traditional line layout is significant.
    /// </summary>
    string ToHtmlPreservingLineBreaks(string markdown);
}
