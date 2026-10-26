using System.Text.Json;
using Respondeo.Bible;
using Respondeo.Bible.Contracts;
using Respondeo.UnitTests.TestSupport;

namespace Respondeo.UnitTests.Bible;

public class BibleTranslationTests
{
    [Theory]
    [InlineData("cpdv", "cpdv")]
    [InlineData("CPDV", "cpdv")]
    [InlineData("drb", "drb")]
    [InlineData("douay-rheims", "drb")]
    public void Find_KnownCodes(string code, string expected) =>
        Assert.Equal(expected, TestBible.All.Single(t => t.Matches(code)).Code);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("kjv")]
    public void Find_UnknownCodes_ReturnsNull(string? code) => Assert.DoesNotContain(TestBible.All, t => t.Matches(code));

    [Fact]
    public void Default_IsDouayRheims() => Assert.Equal("drb", TestBible.All[0].Code);

    [Fact]
    public void CpdvData_CoversEveryCanonicalBook()
    {
        var folder = Path.Combine(FindRepoRoot(), "src", "Respondeo.Bible", "wwwroot", "cpdv");
        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(folder, "manifest.json")));
        var books = manifest.RootElement.GetProperty("books");
        foreach (var book in BibleBooks.All.Where(b => b.Testament != BibleTestament.Appendix))
        {
            Assert.True(books.TryGetProperty(book.Slug, out var entry), $"Missing CPDV manifest entry for {book.Slug}");
            var path = Path.Combine(folder, entry.GetProperty("file").GetString()!);
            Assert.True(File.Exists(path), $"Missing CPDV file for {book.Slug}");
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            Assert.True(doc.RootElement.GetProperty("chapters").GetArrayLength() > 0, $"Empty CPDV file for {book.Slug}");
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
