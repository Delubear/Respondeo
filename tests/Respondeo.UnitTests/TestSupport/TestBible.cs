using Respondeo.Bible;
using Respondeo.Bible.Contracts;

namespace Respondeo.UnitTests.TestSupport;

/// <summary>The bundled translations, loaded once from the repository's Bible static assets.</summary>
internal static class TestBible
{
    public static readonly IReadOnlyList<BibleTranslation> All = BibleTranslationCatalog.LoadFromDirectory(
        Path.Combine(FindRepoRoot(), "src", "Respondeo.Bible", "wwwroot"));

    public static BibleTranslation Drb => All.Single(t => t.Code == "drb");

    public static BibleTranslation Cpdv => All.Single(t => t.Code == "cpdv");

    public static BibleTranslation Get(string code) => All.Single(t => t.Matches(code));

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Respondeo.slnx")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException($"Repo root not found from {AppContext.BaseDirectory}.");
    }
}
