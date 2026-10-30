using System.Text.Json;
using System.Text.RegularExpressions;
using Respondeo.UnitTests.TestSupport;
using Respondeo.Bible;
using Respondeo.Bible.Contracts;

namespace Respondeo.UnitTests.Services;

public class BibleCitationPsalmTests
{
    public static TheoryData<string, int, int, int, int> SingleVerses()
    {
        (int, int, int, int)[] map =
        [
            (1, 1, 1, 1), (9, 1, 9, 1), (9, 21, 9, 21), (10, 1, 9, 22), (10, 18, 9, 39),
            (11, 1, 10, 1), (23, 1, 22, 1), (51, 3, 50, 3), (113, 1, 112, 1),
            (114, 1, 113, 1), (114, 8, 113, 8), (115, 1, 113, 9), (115, 18, 113, 26),
            (116, 1, 114, 1), (116, 9, 114, 9), (116, 10, 115, 1), (116, 19, 115, 10),
            (117, 1, 116, 1), (118, 1, 117, 1), (146, 1, 145, 1), (147, 1, 146, 1),
            (147, 11, 146, 11), (147, 12, 147, 1), (147, 20, 147, 9), (148, 1, 148, 1), (150, 6, 150, 6),
        ];
        var data = new TheoryData<string, int, int, int, int>();
        foreach (var code in new[] { "drb", "cpdv" })
        {
            foreach (var (hc, hv, vc, vv) in map)
            {
                data.Add(code, hc, hv, vc, vv);
            }
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(SingleVerses))]
    public void SingleVerse_MapsHebrewToVulgate(string code, int hebrewChapter, int hebrewVerse, int chapter, int verse)
    {
        var translation = TestBible.All.Single(t => t.Code == code);
        var passage = BibleCitation.Parse($"Psalm {hebrewChapter}:{hebrewVerse}", translation);

        Assert.NotNull(passage);
        Assert.Equal("psalms", passage.BookSlug);
        Assert.Equal(new VerseSpan(chapter, verse, chapter, verse), Assert.Single(passage.Spans));
    }

    [Theory]
    [InlineData(95, 94)]
    [InlineData(100, 99)]
    [InlineData(67, 66)]
    [InlineData(24, 23)]
    public void WholePsalm_ParsesToWholeVulgateChapter(int hebrew, int vulgate)
    {
        var translation = TestBible.All.Single(t => t.Code == "drb");
        var passage = BibleCitation.Parse($"Psalm {hebrew}", translation);

        Assert.NotNull(passage);
        Assert.Equal(new VerseSpan(vulgate, 1, vulgate, BibleCitation.EndOfChapter), Assert.Single(passage.Spans));
    }

    [Theory]
    [InlineData("Psalm 116:1-19", 114, 1, 115, 10)]
    [InlineData("Psalm 147:1-20", 146, 1, 147, 9)]
    [InlineData("Psalm 9:1-10:18", 9, 1, 9, 39)]
    [InlineData("Psalm 114:1-115:18", 113, 1, 113, 26)]
    [InlineData("Psalm 116:9b-10a", 114, 9, 115, 1)]
    public void Range_AcrossSplitOrMergedPsalms(string citation, int chapter, int start, int endChapter, int end) =>
        Assert.Equal(new VerseSpan(chapter, start, endChapter, end), Assert.Single(BibleCitation.Parse(citation, TestBible.Drb)!.Spans));

    [Fact]
    public void CommaList_KeepsHebrewChapterForLaterParts() =>
        Assert.Equal(
            [new VerseSpan(114, 1, 114, 2), new VerseSpan(115, 3, 115, 4)],
            BibleCitation.Parse("Psalm 116:1-2, 12-13", TestBible.Drb)!.Spans);

    [Fact]
    public void NonPsalm_IsNotRenumbered() =>
        Assert.Equal(new VerseSpan(15, 1, 15, 1), Assert.Single(BibleCitation.Parse("Exodus 15:1", TestBible.Drb)!.Spans));

    [Fact]
    public void WholeChapter_SpansEntireChapter() =>
        Assert.Equal(new VerseSpan(22, 1, 22, BibleCitation.EndOfChapter), Assert.Single(BibleCitation.Parse("Psalm 23", TestBible.Drb)!.Spans));

    [Fact]
    public void CpdvData_HasNoHebrewNumberingMarkers()
    {
        var folder = Path.Combine(FindRepoRoot(), "src", "Respondeo.Bible", "wwwroot", "cpdv");
        var marker = new Regex(@"\(\d+[AB]?\)\s*$");
        foreach (var file in Directory.GetFiles(folder, "*.json").Where(f => Path.GetFileName(f) != "manifest.json"))
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(file));
            foreach (var chapter in doc.RootElement.GetProperty("chapters").EnumerateArray())
            {
                foreach (var verse in chapter.GetProperty("verses").EnumerateArray())
                {
                    var text = verse.GetProperty("text").GetString() ?? string.Empty;
                    Assert.False(
                        marker.IsMatch(text),
                        $"{Path.GetFileName(file)} {chapter.GetProperty("chapter")}:{verse.GetProperty("verse")} ends with a Hebrew marker");
                }
            }
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
