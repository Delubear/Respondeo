using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Respondeo.Services;

namespace Respondeo.UnitTests.Services;

/// <summary>
/// Guards that the shared settings baseline packaged by both hosts stays in sync with the
/// <see cref="FeatureFlags"/> model.
/// </summary>
public class FeatureFlagsBindingTests
{
    private static string AppSettingsPath([CallerFilePath] string thisFile = "")
    {
        // This file lives at <repo>/tests/Respondeo.UnitTests/Services/FeatureFlagsBindingTests.cs.
        var repoRoot = Directory.GetParent(thisFile)!.Parent!.Parent!.Parent!.FullName;
        return Path.Combine(repoRoot, "src", "Respondeo.UI", "Configuration", "appsettings.json");
    }

    private static IConfiguration LoadConfiguration() =>
        new ConfigurationBuilder()
            .AddJsonFile(AppSettingsPath(), optional: false)
            .Build();

    [Fact]
    public void Binds_all_flags_from_the_shipped_appsettings()
    {
        var config = LoadConfiguration();

        var flags = config.GetSection(FeatureFlags.SectionName).Get<FeatureFlags>();
        using var expected = JsonDocument.Parse(File.ReadAllText(AppSettingsPath()),
            new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip });
        var defaults = expected.RootElement.GetProperty(FeatureFlags.SectionName);

        Assert.NotNull(flags);
        Assert.Equal(defaults.GetProperty(nameof(FeatureFlags.LiturgicalCalendarFeature)).GetBoolean(), flags.LiturgicalCalendarFeature);
        Assert.Equal(defaults.GetProperty(nameof(FeatureFlags.LiturgyOfTheHoursFeature)).GetBoolean(), flags.LiturgyOfTheHoursFeature);
        Assert.Equal(defaults.GetProperty(nameof(FeatureFlags.ExternalFeedback)).GetBoolean(), flags.ExternalFeedback);
    }

    [Fact]
    public void Every_json_key_maps_to_a_model_property()
    {
        var json = File.ReadAllText(AppSettingsPath());
        using var doc = JsonDocument.Parse(json, new JsonDocumentOptions
        {
            CommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
        });

        var jsonKeys = doc.RootElement.GetProperty(FeatureFlags.SectionName)
            .EnumerateObject()
            .Select(p => p.Name)
            .ToList();

        var modelProps = typeof(FeatureFlags)
            .GetProperties()
            .Where(p => p.PropertyType == typeof(bool))
            .Select(p => p.Name)
            .ToHashSet();

        var unknown = jsonKeys.Where(k => !modelProps.Contains(k)).ToList();
        Assert.True(unknown.Count == 0, $"appsettings.json has FeatureFlags keys with no matching property: {string.Join(", ", unknown)}");

        var missing = modelProps.Where(p => !jsonKeys.Contains(p)).ToList();
        Assert.True(missing.Count == 0, $"FeatureFlags model has properties not present in appsettings.json: {string.Join(", ", missing)}");
    }
}
