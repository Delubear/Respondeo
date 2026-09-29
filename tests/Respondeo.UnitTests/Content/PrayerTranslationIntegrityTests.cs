using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Respondeo.UnitTests.Content;

/// <summary>
/// Guards the prayer corpus against orphaned translations: every non-primary-language prayer
/// (e.g. a Latin file) must share its translationKey with a primary-language (e.g. English)
/// prayer, and every translationKey must have exactly one primary prayer. This catches a Latin
/// prayer whose English companion is missing, renamed, or given a mismatched key at test time
/// rather than as a broken parallel-text panel at runtime.
/// </summary>
public class PrayerTranslationIntegrityTests
{
    private const string PrimaryLanguage = "en";

    private static string PrayersDirectory([CallerFilePath] string thisFile = "")
    {
        // This file lives at <repo>/tests/Respondeo.UnitTests/Content/PrayerTranslationIntegrityTests.cs.
        // Walk up to the repo root, then into the Discover content library's prayers folder.
        var repoRoot = Directory.GetParent(thisFile)!.Parent!.Parent!.Parent!.FullName;
        return Path.Combine(repoRoot, "src", "Respondeo.Content.Discover", "wwwroot", "discover", "prayers");
    }

    private static IReadOnlyList<PrayerMeta> ReadPrayers()
    {
        var dir = PrayersDirectory();

        return Directory
            .EnumerateFiles(dir, "*.md", SearchOption.TopDirectoryOnly)
            .Select(path => ReadMeta(path, File.ReadAllText(path)))
            .ToList();
    }

    private static PrayerMeta ReadMeta(string path, string raw)
    {
        var fileName = Path.GetFileName(path);

        return new PrayerMeta(
            FileName: fileName,
            Id: FrontMatterValue(raw, "id"),
            Language: FrontMatterValue(raw, "language"),
            TranslationKey: FrontMatterValue(raw, "translationKey"));
    }

    // Reads a single scalar front-matter value (e.g. `language: la`), trimming quotes if present.
    private static string? FrontMatterValue(string raw, string key)
    {
        var match = Regex.Match(raw, $@"^{Regex.Escape(key)}:\s*(?<value>.+?)\s*$", RegexOptions.Multiline);

        if (!match.Success)
        {
            return null;
        }

        return match.Groups["value"].Value.Trim().Trim('"');
    }

    [Fact]
    public void Every_prayer_declares_a_language_and_translation_key()
    {
        var prayers = ReadPrayers();

        Assert.NotEmpty(prayers);

        var missing = prayers
            .Where(p => string.IsNullOrWhiteSpace(p.Language) || string.IsNullOrWhiteSpace(p.TranslationKey))
            .Select(p => p.FileName)
            .ToList();

        Assert.True(
            missing.Count == 0,
            $"Prayers missing a language and/or translationKey: {string.Join(", ", missing)}");
    }

    [Fact]
    public void Every_non_primary_language_prayer_has_a_matching_primary_prayer()
    {
        var prayers = ReadPrayers();

        Assert.NotEmpty(prayers);

        var primaryKeys = prayers
            .Where(p => string.Equals(p.Language, PrimaryLanguage, StringComparison.OrdinalIgnoreCase))
            .Where(p => !string.IsNullOrWhiteSpace(p.TranslationKey))
            .Select(p => p.TranslationKey!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var orphaned = prayers
            .Where(p => !string.IsNullOrWhiteSpace(p.Language) && !string.Equals(p.Language, PrimaryLanguage, StringComparison.OrdinalIgnoreCase))
            .Where(p => string.IsNullOrWhiteSpace(p.TranslationKey) || !primaryKeys.Contains(p.TranslationKey!))
            .Select(p => p.FileName)
            .ToList();

        Assert.True(
            orphaned.Count == 0,
            $"Non-primary-language prayers have no matching '{PrimaryLanguage}' prayer sharing their translationKey: {string.Join(", ", orphaned)}");
    }

    [Fact]
    public void Every_translation_key_has_exactly_one_primary_language_prayer()
    {
        var prayers = ReadPrayers();

        var duplicates = prayers
            .Where(p => string.Equals(p.Language, PrimaryLanguage, StringComparison.OrdinalIgnoreCase))
            .Where(p => !string.IsNullOrWhiteSpace(p.TranslationKey))
            .GroupBy(p => p.TranslationKey!, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => $"{g.Key} => [{string.Join(", ", g.Select(p => p.FileName))}]")
            .ToList();

        Assert.True(
            duplicates.Count == 0,
            $"translationKeys with more than one primary '{PrimaryLanguage}' prayer: {string.Join("; ", duplicates)}");
    }

    private sealed record PrayerMeta(string FileName, string? Id, string? Language, string? TranslationKey);
}
