using Respondeo.Content.Summa.Contracts;

namespace Respondeo.Content.Summa;

/// <summary>
/// Adapts the narrow <see cref="ISummaPartMap"/> contract onto the <see cref="ISummaPartCatalog"/>,
/// letting the rendering layer resolve part labels and slugs without depending on the full catalog surface.
/// </summary>
internal sealed class SummaPartMap(ISummaPartCatalog parts) : ISummaPartMap
{
    public string LabelForKey(string key) => parts.LabelForKey(key);

    public string SlugForKey(string key) => parts.SlugForKey(key);
}
