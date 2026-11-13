namespace Respondeo.Services;

/// <summary>Host-provided app updates; unsupported hosts keep the manual action hidden.</summary>
public interface IAppUpdateService
{
    bool Supported { get; }

    /// <summary>Checks for updates. Manual checks bypass automatic throttling and report every outcome.</summary>
    Task CheckAsync(bool userInitiated = false);
}

public sealed class UnsupportedAppUpdates : IAppUpdateService
{
    public bool Supported => false;

    public Task CheckAsync(bool userInitiated = false) => throw new NotSupportedException("App updates are not available in this host.");
}
