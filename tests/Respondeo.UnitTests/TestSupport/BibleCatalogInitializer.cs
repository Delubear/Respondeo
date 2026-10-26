using System.Runtime.CompilerServices;
using Respondeo.Bible;

namespace Respondeo.UnitTests.TestSupport;

internal static class BibleCatalogInitializer
{
    [ModuleInitializer]
    internal static void Load()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Respondeo.slnx")))
        {
            dir = dir.Parent;
        }

        if (dir is null)
        {
            throw new InvalidOperationException($"Repo root not found from {AppContext.BaseDirectory}.");
        }

        BibleCatalog.LoadFromDirectory(Path.Combine(dir.FullName, "src", "Respondeo.Bible", "wwwroot"));
    }
}
