using System.Text.RegularExpressions;
using Respondeo.LiturgyOfTheHours;

namespace Respondeo.UnitTests.LiturgyOfTheHours;

public class HoursContentFileTests
{
    private static string ContentDirectory()
    {
        var env = Environment.GetEnvironmentVariable("HOURS_CONTENT_DIR");
        if (!string.IsNullOrWhiteSpace(env))
        {
            return env;
        }

        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Respondeo.slnx")))
        {
            dir = dir.Parent;
        }

        Assert.NotNull(dir);
        return Path.Combine(dir!.FullName, "src", "Respondeo.LiturgyOfTheHours", "SampleContent", "hours");
    }

    private static HoursContentFile LoadDirectory(string dir) =>
        HoursContentFile.Merge(Directory.GetFiles(dir, "*.json", SearchOption.AllDirectories)
            .Where(path => !path.EndsWith(".schema.json", StringComparison.OrdinalIgnoreCase))
            .Order(StringComparer.Ordinal)
            .Select(path =>
            {
                using var stream = File.OpenRead(path);
                return (Path.GetFileName(path), EmbeddedHoursTextProvider.Parse(stream));
            })
            .ToList());

    [Fact]
    public void ContentDirectory_MergesWithoutConflicts()
    {
        var content = LoadDirectory(ContentDirectory());

        Assert.NotEmpty(content.Texts);
        Assert.InRange(content.SchemaVersion, 1, HoursContentFile.SupportedSchemaVersion);
        if (!content.IsSample)
        {
            Assert.True(HoursAcknowledgment.IsValid(content.Acknowledgment));
        }
    }

    [Fact]
    public void ContentDirectory_KeysAreWellFormed()
    {
        var content = LoadDirectory(ContentDirectory());
        var pattern = new Regex("^[a-z0-9-]+(/[a-z0-9-]+)*$");

        foreach (var (key, text) in content.Texts)
        {
            Assert.Matches(pattern, key);
            Assert.False(string.IsNullOrWhiteSpace(text.Text) && string.IsNullOrWhiteSpace(text.Antiphon), key);
        }
    }

    [Fact]
    public void ContentDirectory_CommonsReferencedByCelebrationsExist()
    {
        var content = LoadDirectory(ContentDirectory());
        var commons = content.Texts.Keys
            .Where(key => key.StartsWith("common/", StringComparison.Ordinal))
            .Select(key => key.Split('/')[1])
            .ToHashSet(StringComparer.Ordinal);

        foreach (var celebration in content.Celebrations)
        {
            if (celebration.Value.Common is { } common)
            {
                Assert.Contains(common, commons);
            }
        }
    }

    [Fact]
    public void Merge_RejectsDuplicateKeysAndUnsupportedVersions()
    {
        var a = new HoursContentFile { Texts = new() { ["hymn"] = new LicensedText("a") } };
        var b = new HoursContentFile { Texts = new() { ["hymn"] = new LicensedText("b") } };
        Assert.Throws<InvalidDataException>(() => HoursContentFile.Merge([("a.json", a), ("b.json", b)]));

        var future = new HoursContentFile { SchemaVersion = HoursContentFile.SupportedSchemaVersion + 1 };
        Assert.Throws<InvalidDataException>(() => HoursContentFile.Merge([("future.json", future)]));
    }

    [Theory]
    [InlineData("Saint Agnes, Virgin and Martyr", "saint-agnes-virgin-and-martyr")]
    [InlineData("Our Lady of Lourdes", "our-lady-of-lourdes")]
    [InlineData("Saint Th\u00e9r\u00e8se of the Child Jesus", "saint-therese-of-the-child-jesus")]
    public void CelebrationId_Slugifies(string name, string expected) =>
        Assert.Equal(expected, OrdinaryFormOffice.CelebrationId(name));
}
