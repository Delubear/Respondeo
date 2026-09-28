using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Respondeo.Services;

namespace Respondeo.UnitTests.Services;

/// <summary>
/// Guards that the publicly shipped <c>wwwroot/appsettings.json</c> stays in sync with the
/// <see cref="FeatureFlags"/> model. Because this is a Blazor WebAssembly app, the config file is a
/// static asset bound at startup, so a rename in one place but not the other silently drops a flag.
/// </summary>
public class FeatureFlagsBindingTests
{
    private static string AppSettingsPath([CallerFilePath] string thisFile = "")
    {
        // This file lives at <repo>/tests/Respondeo.UnitTests/Services/FeatureFlagsBindingTests.cs.
        var repoRoot = Directory.GetParent(thisFile)!.Parent!.Parent!.Parent!.FullName;
        return Path.Combine(repoRoot, "src", "Respondeo", "wwwroot", "appsettings.json");
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

        Assert.NotNull(flags);
        Assert.True(flags!.AquinasPortrait);
        Assert.True(flags.SummaPillar);
        Assert.True(flags.DiscoverPillar);
        Assert.True(flags.MiraclesFeature);
        Assert.True(flags.PrayerFeature);
        Assert.True(flags.DevotionsFeature);
        Assert.True(flags.ArticlesFeature);
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
