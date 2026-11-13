using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using NSubstitute;
using Respondeo.Bible.Contracts;
using Respondeo.Content.Contracts;
using Respondeo.Content.Miracles;
using Respondeo.Content.Summa.Contracts;
using Respondeo.LiturgicalCalendar;
using Respondeo.LiturgyOfTheHours;
using Respondeo.Services;

namespace Respondeo.UnitTests.Services;

public class AppServiceRegistrationTests
{
    private static IConfigurationBuilder Baseline() => new ConfigurationBuilder()
        .AddRequiredEmbeddedSettings(System.Reflection.Assembly.Load("Respondeo"), "Respondeo.appsettings.json");

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Shared_registration_resolves_engines_and_preserves_host_services(bool native)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IJSRuntime>(Substitute.For<IJSRuntime>());
        services.AddSingleton<NavigationManager>(new TestNavigationManager());
        using var transport = new HttpClient { BaseAddress = new Uri(native ? "https://0.0.0.0/" : "https://respondeo.test/") };
        services.AddScoped(_ => transport);
        var capabilities = Substitute.For<IHostCapabilities>();
        var themeSync = Substitute.For<INativeThemeSync>();
        var updates = Substitute.For<IAppUpdateService>();
        if (native)
        {
            services.AddSingleton(capabilities);
            services.AddSingleton(themeSync);
            services.AddSingleton(updates);
        }

        var configuration = Baseline().Build();
        services.AddRespondeoApplication(configuration);
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        using var scope = provider.CreateScope();
        var scoped = scope.ServiceProvider;

        Assert.Same(transport, scoped.GetRequiredService<HttpClient>());
        if (native)
        {
            Assert.Same(capabilities, scoped.GetRequiredService<IHostCapabilities>());
            Assert.Same(themeSync, scoped.GetRequiredService<INativeThemeSync>());
            Assert.Same(updates, scoped.GetRequiredService<IAppUpdateService>());
        }
        else
        {
            Assert.IsType<BrowserHostCapabilities>(scoped.GetRequiredService<IHostCapabilities>());
            Assert.IsType<NoOpNativeThemeSync>(scoped.GetRequiredService<INativeThemeSync>());
            Assert.True(scoped.GetRequiredService<IHostCapabilities>().SupportsPwa);
            Assert.IsType<UnsupportedAppUpdates>(scoped.GetRequiredService<IAppUpdateService>());
            Assert.False(scoped.GetRequiredService<IAppUpdateService>().Supported);
        }
        Assert.Single(scoped.GetServices<IHostCapabilities>());
        Assert.Single(scoped.GetServices<INativeThemeSync>());
        Assert.Single(scoped.GetServices<IAppUpdateService>());
        var calendar = scoped.GetRequiredService<PreferredLiturgicalCalendar>();
        Assert.Same(calendar, scoped.GetRequiredService<ILiturgicalCalendar>());
        Assert.Same(calendar, scoped.GetRequiredService<ICalendarPreference>());
        Assert.NotNull(scoped.GetRequiredService<IContentService>());
        Assert.NotNull(scoped.GetRequiredService<IMiracleService>());
        Assert.NotNull(scoped.GetRequiredService<ILiturgyOfTheHours>());
        Assert.NotNull(scoped.GetRequiredService<ISummaService>());
        Assert.NotNull(scoped.GetRequiredService<IBibleText>());
        Assert.NotNull(scoped.GetRequiredService<ScrollCoordinator>());
        Assert.NotNull(scoped.GetRequiredService<RoutePrefetcher>());
        var flags = scoped.GetRequiredService<FeatureFlags>();
        Assert.Equal(configuration.GetValue<bool>("FeatureFlags:LiturgicalCalendarFeature"), flags.LiturgicalCalendarFeature);
        Assert.Equal(configuration.GetValue<bool>("FeatureFlags:LiturgyOfTheHoursFeature"), flags.LiturgyOfTheHoursFeature);
        Assert.Equal(configuration.GetValue<bool>("FeatureFlags:ExternalFeedback"), flags.ExternalFeedback);
    }

    [Theory]
    [InlineData("LiturgicalCalendarFeature", false)]
    [InlineData("LiturgicalCalendarFeature", true)]
    [InlineData("LiturgyOfTheHoursFeature", false)]
    [InlineData("LiturgyOfTheHoursFeature", true)]
    [InlineData("ExternalFeedback", false)]
    [InlineData("ExternalFeedback", true)]
    public void Later_override_changes_only_its_flag_and_configuration(string flag, bool enabled)
    {
        var baseline = Baseline().Build();
        var calendar = baseline.GetValue<bool>("FeatureFlags:LiturgicalCalendarFeature");
        var hours = baseline.GetValue<bool>("FeatureFlags:LiturgyOfTheHoursFeature");
        var feedback = baseline.GetValue<bool>("FeatureFlags:ExternalFeedback");
        var configuration = new ConfigurationBuilder().AddConfiguration(baseline).AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"FeatureFlags:{flag}"] = enabled.ToString()
        }).Build();
        var services = new ServiceCollection().AddRespondeoApplication(configuration);
        using var provider = services.BuildServiceProvider();
        var flags = provider.GetRequiredService<FeatureFlags>();
        Assert.Equal(flag == nameof(FeatureFlags.LiturgicalCalendarFeature) ? enabled : calendar, flags.LiturgicalCalendarFeature);
        Assert.Equal(flag == nameof(FeatureFlags.LiturgyOfTheHoursFeature) ? enabled : hours, flags.LiturgyOfTheHoursFeature);
        Assert.Equal(flag == nameof(FeatureFlags.ExternalFeedback) ? enabled : feedback, flags.ExternalFeedback);

        using var unchanged = new ServiceCollection().AddRespondeoApplication(baseline).BuildServiceProvider();
        var unchangedFlags = unchanged.GetRequiredService<FeatureFlags>();
        Assert.Equal(calendar, unchangedFlags.LiturgicalCalendarFeature);
        Assert.Equal(hours, unchangedFlags.LiturgyOfTheHoursFeature);
        Assert.Equal(feedback, unchangedFlags.ExternalFeedback);
    }

    [Theory]
    [InlineData(null, null, null)]
    [InlineData("true", null, null)]
    [InlineData("true", "true", null)]
    [InlineData("invalid", "true", "true")]
    [InlineData("true", "", "true")]
    [InlineData("true", "true", "")]
    public void Missing_or_invalid_configuration_fails_at_registration(string? calendar, string? hours, string? feedback)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["FeatureFlags:LiturgicalCalendarFeature"] = calendar,
            ["FeatureFlags:LiturgyOfTheHoursFeature"] = hours,
            ["FeatureFlags:ExternalFeedback"] = feedback
        }).Build();

        Assert.Throws<InvalidOperationException>(() => new ServiceCollection().AddRespondeoApplication(configuration));
    }

    [Fact]
    public void Missing_embedded_configuration_fails_with_resource_identity()
    {
        var error = Assert.Throws<InvalidOperationException>(() => new ConfigurationBuilder()
            .AddRequiredEmbeddedSettings(typeof(AppServiceRegistrationTests).Assembly, "missing.settings.json"));

        Assert.Contains("missing.settings.json", error.Message);
        Assert.Contains("Respondeo.UnitTests", error.Message);
    }

    [Fact]
    public void Embedded_configuration_can_be_built_after_the_resource_stream_is_disposed()
    {
        var builder = Baseline();
        var configuration = builder.Build();
        using var stream = System.Reflection.Assembly.Load("Respondeo").GetManifestResourceStream("Respondeo.appsettings.json");
        Assert.NotNull(stream);
        using var expected = JsonDocument.Parse(stream, new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip });

        foreach (var flag in expected.RootElement.GetProperty(FeatureFlags.SectionName).EnumerateObject())
        {
            Assert.Equal(flag.Value.GetBoolean().ToString(), configuration[$"FeatureFlags:{flag.Name}"], ignoreCase: true);
        }
    }

    private sealed class TestNavigationManager : NavigationManager
    {
        public TestNavigationManager() => Initialize("https://respondeo.test/", "https://respondeo.test/");

        protected override void NavigateToCore(string uri, bool forceLoad) => throw new NotSupportedException();
    }
}
