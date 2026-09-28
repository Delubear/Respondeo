namespace Respondeo.Content.Abstractions;

/// <summary>
/// Resolves a stable Summa part storage key (e.g. <c>p2b</c>) to the presentation values the
/// reference renderer needs: the cross-reference display label (e.g. <c>II-II</c>) and the URL slug
/// (e.g. <c>prima</c>). Kept in the abstractions project so the rendering layer can expand
/// cross-reference tokens without depending on the full Summa corpus project.
/// </summary>
public interface ISummaPartMap
{
    /// <summary>The cross-reference display label for a storage part key (e.g. <c>p2b</c> -&gt; <c>II-II</c>).</summary>
    string LabelForKey(string key);

    /// <summary>The URL slug for a storage part key (e.g. <c>p1</c> -&gt; <c>prima</c>).</summary>
    string SlugForKey(string key);
}
