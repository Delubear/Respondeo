namespace Respondeo.Content.Summa.Contracts;

/// <summary>
/// One part of the Summa and the several identifiers we attach to it.
/// </summary>
/// <param name="Key">
/// The stable, neutral storage key baked into the corpus:
/// question ids, JSON filenames, and the <c>sref</c>/<c>sobj</c> cross-reference tokens all use this (e.g. <c>p1</c>, <c>p2a</c>).
/// It never changes, so the slug and label below can be re-styled freely without regenerating the corpus.
/// </param>
/// <param name="Slug">The URL segment shown to readers (e.g. <c>prima</c>): <c>/summa/prima-q002</c>.</param>
/// <param name="Label">The short display label used in cross-references (e.g. <c>II-II</c>).</param>
/// <param name="Title">The human-readable part title (e.g. <c>First Part</c>).</param>
/// <param name="Folder">The on-disk corpus subfolder holding this part's question files.</param>
/// <param name="LatinName">The traditional Latin name of the part (e.g. <c>Prima Pars</c>).</param>
/// <param name="Description">A short plain-language summary of what the part covers.</param>
public sealed record SummaPart(
    string Key,
    string Slug,
    string Label,
    string Title,
    string Folder,
    string LatinName,
    string Description);

