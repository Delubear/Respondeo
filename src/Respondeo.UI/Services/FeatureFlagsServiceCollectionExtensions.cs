using Microsoft.Extensions.Configuration;

namespace Respondeo.Services;

/// <summary>Registers <see cref="FeatureFlags"/> bound from configuration, shared by every host.</summary>
public static class FeatureFlagsServiceCollectionExtensions
{
    /// <summary>Binds the "FeatureFlags" section (falling back to all-on defaults) and registers it as a singleton.</summary>
    public static IServiceCollection AddFeatureFlags(this IServiceCollection services, IConfiguration configuration) =>
        services.AddSingleton(configuration.GetSection(FeatureFlags.SectionName).Get<FeatureFlags>() ?? new FeatureFlags());
}
