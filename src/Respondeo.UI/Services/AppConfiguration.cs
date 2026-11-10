using System.Reflection;
using Microsoft.Extensions.Configuration;

namespace Respondeo.Services;

/// <summary>Loads required packaged configuration without silently substituting feature defaults.</summary>
public static class AppConfiguration
{
    /// <summary>Adds embedded JSON; intentional host overrides must be added after this baseline and before service registration.</summary>
    public static IConfigurationBuilder AddRequiredEmbeddedSettings(this IConfigurationBuilder builder, Assembly assembly, string resourceName)
    {
        using var settings = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Required embedded configuration '{resourceName}' is missing from '{assembly.GetName().Name}'.");
        var configuration = new ConfigurationBuilder().AddJsonStream(settings).Build();
        return builder.AddConfiguration(configuration);
    }
}
