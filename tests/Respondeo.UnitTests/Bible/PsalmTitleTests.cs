using System.Text.Json;
using Respondeo.Bible;

namespace Respondeo.UnitTests.Bible;

public class PsalmTitleTests
{
    [Theory]
    [InlineData(1, 1, "Blessed is the man.", "Blessed is the man.")]
    [InlineData(3, 1, "The Psalm of David, when he fled. Lord why are they multiplied?", "Lord why are they multiplied?")]
    [InlineData(17, 1, "Unto the end, to the servant of our Lord David, and he said: I will love thee o Lord my strength:",
        "I will love thee o Lord my strength:")]
    [InlineData(96, 1, "To this David, when his land was restored again to him. Our Lord hath reigned, let the earth rejoice.",
        "Our Lord hath reigned, let the earth rejoice.")]
    [InlineData(33, 1, "To David, when he changed his countenance before Abimelech", "")]
    [InlineData(50, 1, "Unto the end, a Psalm of David,", "")]
    [InlineData(50, 2, "when Nathan the Prophet came to him.", "")]
    [InlineData(50, 3, "Have mercy on me O God.", "Have mercy on me O God.")]
    [InlineData(71, 1, "Concerning Salomon.", "")]
    public void CleanPsalmVerse_StripsTitles(int chapter, int verse, string text, string expected) =>
        Assert.Equal(expected, BibleText.CleanPsalmVerse(chapter, verse, text));

    [Fact]
    public void EveryDouayRheimsPsalm_StartsWithoutTitle()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Respondeo.slnx")))
        {
            dir = dir.Parent;
        }

        Assert.NotNull(dir);
        var path = Path.Combine(dir.FullName, "src", "Respondeo.Bible", "wwwroot", "douay-rheims", "psalms.json");
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        var root = doc.RootElement;
        var chapters = root.GetProperty("chapters").EnumerateArray().ToList();
        Assert.Equal(150, chapters.Count);

        string[] titleStarts = ["Unto the end", "To David", "Concerning", "A Psalm", "To this David", "The Psalm", "A Canticle"];
        foreach (var chapter in chapters)
        {
            var number = chapter.GetProperty("chapter").GetInt32();
            var first = chapter.GetProperty("verses").EnumerateArray()
                .Select(v => BibleText.CleanPsalmVerse(number, v.GetProperty("verse").GetInt32(), v.GetProperty("text").GetString()!.Trim()))
                .First(t => t.Length > 0);
            Assert.False(titleStarts.Any(s => first.StartsWith(s, StringComparison.OrdinalIgnoreCase)), $"Psalm {number}: {first}");
        }
    }
}
