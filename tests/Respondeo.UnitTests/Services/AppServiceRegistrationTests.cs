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
        if (native)
        {
            services.AddSingleton(capabilities);
            services.AddSingleton(themeSync);
        }

        services.AddRespondeoApplication(Baseline().Build());
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        using var scope = provider.CreateScope();
        var scoped = scope.ServiceProvider;

        Assert.Same(transport, scoped.GetRequiredService<HttpClient>());
        if (native)
        {
            Assert.Same(capabilities, scoped.GetRequiredService<IHostCapabilities>());
            Assert.Same(themeSync, scoped.GetRequiredService<INativeThemeSync>());
        }
        else
        {
            Assert.IsType<BrowserHostCapabilities>(scoped.GetRequiredService<IHostCapabilities>());
            Assert.IsType<NoOpNativeThemeSync>(scoped.GetRequiredService<INativeThemeSync>());
            Assert.True(scoped.GetRequiredService<IHostCapabilities>().SupportsPwa);
        }
        Assert.Single(scoped.GetServices<IHostCapabilities>());
        Assert.Single(scoped.GetServices<INativeThemeSync>());
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
        Assert.True(scoped.GetRequiredService<FeatureFlags>().LiturgyOfTheHoursFeature);
        Assert.True(scoped.GetRequiredService<FeatureFlags>().ExternalFeedback);
    }

    [Theory]
    [InlineData("LiturgyOfTheHoursFeature")]
    [InlineData("ExternalFeedback")]
    public void Later_override_changes_only_its_flag_and_configuration(string flag)
    {
        var configuration = Baseline().AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"FeatureFlags:{flag}"] = "false"
        }).Build();
        var services = new ServiceCollection().AddRespondeoApplication(configuration);
        using var provider = services.BuildServiceProvider();
        var flags = provider.GetRequiredService<FeatureFlags>();
        Assert.Equal(flag != nameof(FeatureFlags.LiturgyOfTheHoursFeature), flags.LiturgyOfTheHoursFeature);
        Assert.Equal(flag != nameof(FeatureFlags.ExternalFeedback), flags.ExternalFeedback);

        using var unchanged = new ServiceCollection().AddRespondeoApplication(Baseline().Build()).BuildServiceProvider();
        Assert.True(unchanged.GetRequiredService<FeatureFlags>().LiturgyOfTheHoursFeature);
        Assert.True(unchanged.GetRequiredService<FeatureFlags>().ExternalFeedback);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("true", null)]
    [InlineData(null, "false")]
    [InlineData("invalid", "true")]
    [InlineData("true", "")]
    public void Missing_or_invalid_configuration_fails_at_registration(string? hours, string? feedback)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
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

        Assert.Equal("True", configuration["FeatureFlags:LiturgyOfTheHoursFeature"], ignoreCase: true);
        Assert.Equal("True", configuration["FeatureFlags:ExternalFeedback"], ignoreCase: true);
    }

    private sealed class TestNavigationManager : NavigationManager
    {
        public TestNavigationManager() => Initialize("https://respondeo.test/", "https://respondeo.test/");

        protected override void NavigateToCore(string uri, bool forceLoad) => throw new NotSupportedException();
    }
}
