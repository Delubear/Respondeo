using Bunit;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Respondeo.Content.Contracts;
using Respondeo.LiturgicalCalendar;
using Respondeo.Services;

namespace Respondeo.UnitTests.TestSupport;

/// <summary>
/// Registers the services the floating <c>LiturgicalOrb</c> injects (<see cref="ILiturgicalCalendar"/>,
/// <see cref="ISaintService"/>, and the current-date delegate) so Discover root pages that host the orb
/// can render inside bUnit without wiring up the liturgical calendar in every page test.
/// </summary>
internal static class LiturgicalOrbStubs
{
    public static void AddLiturgicalOrbStubs(this TestContext context)
    {
        context.Services.AddSingleton<ILiturgicalCalendar>(new RomanCalendar());
        context.Services.AddSingleton<Func<DateOnly>>(() => new DateOnly(2025, 1, 1));

        // The orb (and the other date-driven surfaces it shares a host with) subscribe to day rollover.
        // A no-op stub satisfies the dependency without a real timer firing during tests.
        if (!context.Services.Any(s => s.ServiceType == typeof(IDayWatcher)))
        {
            var dayWatcher = Substitute.For<IDayWatcher>();
            dayWatcher.Today.Returns(new DateOnly(2025, 1, 1));
            dayWatcher.StartAsync().Returns(Task.CompletedTask);
            context.Services.AddSingleton(dayWatcher);
        }

        if (!context.Services.Any(s => s.ServiceType == typeof(ISaintService)))
        {
            var saints = Substitute.For<ISaintService>();
            saints.GetByIdAsync(Arg.Any<string>()).Returns((SaintRecord?)null);
            context.Services.AddSingleton(saints);
        }

        // The orb's FloatingGuide hosts a Dialog, which injects DialogInterop.
        if (!context.Services.Any(s => s.ServiceType == typeof(DialogInterop)))
        {
            context.Services.AddScoped<DialogInterop>();
        }
    }
}
