using Respondeo.Services;

namespace Respondeo.Maui;

/// <summary>Native host: no service worker or install prompt.</summary>
public sealed class MauiHostCapabilities : IHostCapabilities
{
    public bool SupportsPwa => false;
}
