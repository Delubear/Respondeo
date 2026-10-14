using System.Diagnostics;

// Maps each repository file to the date of the last commit that touched it, read with a single `git log` walk.
// Used for sitemap <lastmod>, which must reflect real content changes (not build time) or crawlers learn to ignore it.
// If git is unavailable (or the checkout is shallow), files resolve to null or the newest commit, and <lastmod> is simply omitted or coarse.
internal sealed class GitDates
{
    private readonly string _repoRoot;
    private readonly Dictionary<string, DateTimeOffset> _dates;

    private GitDates(string repoRoot, Dictionary<string, DateTimeOffset> dates)
    {
        _repoRoot = repoRoot;
        _dates = dates;
    }

    public static GitDates Load(string repoRoot)
    {
        var dates = new Dictionary<string, DateTimeOffset>(StringComparer.OrdinalIgnoreCase);
        try
        {
            var start = new ProcessStartInfo("git", "log --format=@%cI --name-only -- src")
            {
                WorkingDirectory = repoRoot,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };

            using var git = Process.Start(start);
            if (git is null)
            {
                return new GitDates(repoRoot, dates);
            }

            git.StandardError.ReadToEndAsync();
            DateTimeOffset current = default;
            string? line;
            while ((line = git.StandardOutput.ReadLine()) is not null)
            {
                if (line.StartsWith('@'))
                {
                    DateTimeOffset.TryParse(line[1..], out current);
                }
                else if (line.Length > 0)
                {
                    // Newest commits come first, so the first date seen for a path is its last change.
                    dates.TryAdd(line, current);
                }
            }

            git.WaitForExit();
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            Console.Error.WriteLine($"warning: git unavailable ({ex.Message}); sitemap will omit <lastmod>.");
        }

        return new GitDates(repoRoot, dates);
    }

    public DateTimeOffset? For(string? path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return null;
        }

        var relative = Path.GetRelativePath(_repoRoot, path).Replace('\\', '/');
        return _dates.TryGetValue(relative, out var date) ? date : null;
    }
}
