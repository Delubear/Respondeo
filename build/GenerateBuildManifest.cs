using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

public sealed class GenerateBuildManifest : Task
{
    [Required] public string Root { get; set; }
    [Required] public string OutputPath { get; set; }
    [Required] public string Host { get; set; }
    [Required] public string Version { get; set; }
    [Required] public string SourceRevision { get; set; }
    [Required] public string BuildKind { get; set; }
    [Required] public ITaskItem[] ContentFiles { get; set; }
    [Required] public ITaskItem[] HoursFiles { get; set; }
    [Required] public string HoursDirectory { get; set; }
    public string HoursSingleFile { get; set; }
    public string LicensedDirectory { get; set; }
    public string LicensedRevision { get; set; }

    public override bool Execute()
    {
        var licensed = !string.IsNullOrEmpty(LicensedDirectory) || !string.IsNullOrEmpty(HoursSingleFile);
        if (BuildKind == "ci" && (!IsCommit(SourceRevision) || Version == "0.0.0-dev"))
        {
            Log.LogError("CI builds require a source commit and a non-development build version.");
            return false;
        }
        if (BuildKind == "ci" && licensed && !IsCommit(LicensedRevision))
        {
            Log.LogError("CI builds with licensed Hours content require its pinned commit revision.");
            return false;
        }

        var files = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var item in ContentFiles)
        {
            var path = item.GetMetadata("FullPath");
            var prefix = Path.GetFullPath(Root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (!path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                Log.LogError("A content identity input is outside the source root.");
                return false;
            }
            files.Add(path.Substring(prefix.Length).Replace('\\', '/'), path);
        }
        foreach (var item in HoursFiles)
        {
            var path = item.GetMetadata("FullPath");
            var relative = string.IsNullOrEmpty(HoursSingleFile)
                ? path.Substring(Path.GetFullPath(HoursDirectory).TrimEnd(Path.DirectorySeparatorChar).Length + 1)
                : Path.GetFileName(path);
            files.Add("hours/content/" + relative.Replace('\\', '/'), path);
        }

        // Hash canonical logical paths and file hashes, not absolute paths, timestamps, or build outputs.
        var inventory = new StringBuilder("respondeo-content-v1\n");
        using (var sha = SHA256.Create())
        {
            foreach (var file in files)
            {
                using (var stream = File.OpenRead(file.Value))
                {
                    inventory.Append(file.Key).Append('\0').Append(Hex(sha.ComputeHash(stream))).Append('\n');
                }
            }
            var digest = Hex(sha.ComputeHash(Encoding.UTF8.GetBytes(inventory.ToString())));
            var revision = licensed ? (string.IsNullOrEmpty(LicensedRevision) ? "development" : LicensedRevision) : "sample";
            var json = "{\n" +
                "  \"version\": " + Quote(Version) + ",\n" +
                "  \"sourceRevision\": " + Quote(SourceRevision) + ",\n" +
                "  \"host\": " + Quote(Host) + ",\n" +
                "  \"buildKind\": " + Quote(BuildKind) + ",\n" +
                "  \"contentSha256\": " + Quote(digest) + ",\n" +
                "  \"hoursMode\": " + Quote(licensed ? "licensed" : "sample") + ",\n" +
                "  \"hoursRevision\": " + Quote(revision) + "\n}\n";
            Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));
            if (!File.Exists(OutputPath) || File.ReadAllText(OutputPath) != json)
            {
                File.WriteAllText(OutputPath, json, new UTF8Encoding(false));
            }
        }
        return true;
    }

    private static bool IsCommit(string value) => value != null && Regex.IsMatch(value, "\\A[0-9a-fA-F]{40}([0-9a-fA-F]{24})?\\z");

    private static string Hex(byte[] value) => BitConverter.ToString(value).Replace("-", "").ToLowerInvariant();

    private static string Quote(string value)
    {
        var result = new StringBuilder("\"");
        foreach (var character in value)
        {
            if (character == '"' || character == '\\')
            {
                result.Append('\\').Append(character);
            }
            else if (char.IsControl(character))
            {
                result.Append("\\u").Append(((int)character).ToString("x4"));
            }
            else
            {
                result.Append(character);
            }
        }
        return result.Append('"').ToString();
    }
}
