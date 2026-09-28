namespace Respondeo.Content.Shared;

/// <summary>
/// Expands the neutral Summa placeholder tokens emitted by the importer (cross-references and
/// section cues) into final HTML at render time. Kept as a contract in the abstractions project so
/// the app depends only on this interface; the concrete implementation lives in the rendering project.
/// </summary>
public interface ISummaReferenceRenderer
{
    /// <summary>
    /// Replaces every reference and section-cue placeholder in the given HTML. Returns the input
    /// unchanged when it contains no tokens. When <paramref name="articleNumber"/> is supplied, the
    /// section cues emit stable in-page anchor ids so cross-references can deep-link to them.
    /// </summary>
    string Expand(string? html, int? articleNumber = null);
}
