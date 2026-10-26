using System.Text.Json;
using Respondeo.Bible;
using Respondeo.Bible.Contracts;

namespace Respondeo.UnitTests.Bible;

public class BibleTranslationTests
{
    [Theory]
    [InlineData("cpdv", BibleTranslation.Cpdv)]
    [InlineData("CPDV", BibleTranslation.Cpdv)]
    [InlineData("drb", BibleTranslation.DouayRheims)]
    [InlineData("douay-rheims", BibleTranslation.DouayRheims)]
    public void Parse_KnownCodes(string code, BibleTranslation expected) =>
        Assert.Equal(expected, BibleTranslations.Parse(code));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("kjv")]
    public void Parse_UnknownCodes_ReturnsNull(string? code) => Assert.Null(BibleTranslations.Parse(code));

    [Fact]
    public void Codes_RoundTrip()
    {
        foreach (var translation in BibleTranslations.All)
        {
            Assert.Equal(translation, BibleTranslations.Parse(translation.Code()));
        }
    }

    [Fact]
    public void CpdvData_CoversEveryCanonicalBook()
    {
        var folder = Path.Combine(FindRepoRoot(), "src", "Respondeo.Bible", "wwwroot", "cpdv");
        foreach (var book in BibleBooks.All.Where(b => b.Testament != BibleTestament.Appendix))
        {
            var path = Path.Combine(folder, $"{book.FileSlug}.json");
            Assert.True(File.Exists(path), $"Missing CPDV file for {book.FileSlug}");
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            Assert.True(doc.RootElement.GetProperty("chapters").GetArrayLength() > 0, $"Empty CPDV file for {book.FileSlug}");
        }
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Respondeo.slnx")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }
}
