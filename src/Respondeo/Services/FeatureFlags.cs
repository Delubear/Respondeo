namespace Respondeo.Services;

/// <summary>
/// Deploy-time toggles for optional site features. Bound from the "FeatureFlags" section of
/// <c>wwwroot/appsettings.json</c> and registered as a singleton, so flipping a flag there (or in an
/// environment-specific <c>appsettings.{Environment}.json</c>) turns the feature off for a deploy
/// without a code change. Every flag defaults to <see langword="true"/> so features are on unless
/// explicitly disabled.
/// </summary>
public sealed class FeatureFlags
{
    /// <summary>The configuration section these flags bind from.</summary>
    public const string SectionName = "FeatureFlags";

    /// <summary>Shows the St. Thomas Aquinas portrait in the masthead when enabled.</summary>
    public bool AquinasPortrait { get; set; } = true;

    /// <summary>Shows the Summa Theologiae pillar pill in the pillar switcher when enabled.</summary>
    public bool SummaPillar { get; set; } = true;

    /// <summary>Shows the Miracles pillar pill in the pillar switcher when enabled.</summary>
    public bool MiraclesPillar { get; set; } = true;
}
