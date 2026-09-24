using System.Text.Json;
using Markdig;
using Respondeo.SummaImporter;

// Usage: dotnet run -- <path-to-summa.txt> <output-wwwroot-dir>
// Emits:
//   <output>/summa/summa-index.json              (lightweight browse/search index)
//   <output>/summa/<part-folder>/<id>.json        (full content per question, fetched on demand)

if (args.Length < 2)
{
    Console.Error.WriteLine("Usage: dotnet run -- <path-to-summa.txt> <output-wwwroot-dir>");
    return 1;
}

var sourcePath = args[0];
var outputRoot = args[1];

if (!File.Exists(sourcePath))
{
    Console.Error.WriteLine($"Source file not found: {sourcePath}");
    return 1;
}

var lines = File.ReadAllLines(sourcePath);
Console.WriteLine($"Read {lines.Length:N0} lines from {sourcePath}");

var parts = SummaParser.Parse(lines);

var pipeline = new MarkdownPipelineBuilder().Build();
string ToHtml(string markdown) => string.IsNullOrWhiteSpace(markdown)
    ? string.Empty
    : Markdown.ToHtml(markdown, pipeline);

var summaDir = Path.Combine(outputRoot, "summa");
Directory.CreateDirectory(summaDir);

// Question content is split into one subfolder per part so a single directory does not hold the entire corpus.
// The folder name mirrors the human-readable part title in kebab-case; the service derives the same folder from a question's part id when fetching on demand.
static string PartFolder(string partId) => partId switch
{
    "p1" => "first-part",
    "p2a" => "first-part-of-the-second-part",
    "p2b" => "second-part-of-the-second-part",
    "p3" => "third-part",
    "sup" => "supplement",
    _ => partId,
};

var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = false };

// --- Emit per-question content files and build the index in one pass. ---
var indexParts = new List<object>();
var totalQuestions = 0;
var totalArticles = 0;

foreach (var part in parts)
{
    var indexQuestions = new List<object>();

    foreach (var question in part.Questions)
    {
        totalQuestions++;

        var contentArticles = question.Articles.Select(a => new
        {
            number = a.Number,
            title = a.Title,
            bodyHtml = ToHtml(a.BodyMarkdown),
        }).ToList();
        totalArticles += contentArticles.Count;

        var content = new
        {
            id = question.Id,
            partId = question.PartId,
            number = question.Number,
            title = question.Title,
            prologueHtml = ToHtml(question.PrologueMarkdown),
            articles = contentArticles,
        };

        var contentJson = JsonSerializer.Serialize(content, jsonOptions);
        var partDir = Path.Combine(summaDir, PartFolder(question.PartId));
        Directory.CreateDirectory(partDir);
        File.WriteAllText(Path.Combine(partDir, $"{question.Id}.json"), contentJson);

        indexQuestions.Add(new
        {
            id = question.Id,
            number = question.Number,
            title = question.Title,
            treatise = question.Treatise,
            articles = question.Articles.Select(a => new { number = a.Number, title = a.Title }).ToList(),
        });
    }

    indexParts.Add(new
    {
        id = part.Id,
        title = part.Title,
        questions = indexQuestions,
    });
}

var index = new { parts = indexParts };
var indexJson = JsonSerializer.Serialize(index, jsonOptions);
File.WriteAllText(Path.Combine(summaDir, "summa-index.json"), indexJson);

// --- Report structure so the counts can be validated against known totals. ---
Console.WriteLine();
Console.WriteLine("Structure:");
foreach (var part in parts)
{
    Console.WriteLine($"  {part.Id,-3} {part.Title,-32} questions={part.Questions.Count,4}");
}

Console.WriteLine();
Console.WriteLine($"Total questions: {totalQuestions:N0}");
Console.WriteLine($"Total articles:  {totalArticles:N0}");
Console.WriteLine($"Index written to: {Path.Combine(summaDir, "summa-index.json")}");
Console.WriteLine($"Question files:  {summaDir}");

return 0;
