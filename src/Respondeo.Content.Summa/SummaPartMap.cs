namespace Respondeo.Content.Summa;

/// <summary>
/// Adapts the shared <see cref="ISummaPartMap"/> contract onto the corpus-local <see cref="SummaParts"/> registry,
/// letting the rendering layer resolve part labels and slugs without referencing this project.
/// </summary>
internal sealed class SummaPartMap : ISummaPartMap
{
    public string LabelForKey(string key) => SummaParts.LabelForKey(key);

    public string SlugForKey(string key) => SummaParts.SlugForKey(key);
}
