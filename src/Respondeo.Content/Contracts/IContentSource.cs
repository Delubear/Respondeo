namespace Respondeo.Content.Contracts;

/// <summary>
/// A citation or further-reading reference shared across content types (articles, miracles, ...),
/// letting one presentation component render any source list.
/// </summary>
public interface IContentSource
{
    /// <summary>The display label of the source.</summary>
    string Label { get; }

    /// <summary>Optional URL of the source.</summary>
    string? Url { get; }
}
