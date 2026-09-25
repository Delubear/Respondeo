namespace Respondeo.Content.Abstractions;

/// <summary>
/// Contract for turning authored Markdown (including the shared "::: youtube" / "::: pdf" / "::: button"
/// directive vocabulary) into HTML. Kept free of any Markdown-engine types so the abstractions project
/// stays contract-only; the concrete Markdig-based implementation lives in a separate rendering project.
/// </summary>
public interface IContentHtmlRenderer
{
    /// <summary>Renders the supplied Markdown body to HTML.</summary>
    string ToHtml(string markdown);
}
