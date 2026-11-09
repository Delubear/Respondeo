namespace Respondeo.Services.Interop;

/// <summary>
/// Collects event subscriptions so a component can undo all of them in one call from Dispose.
/// </summary>
public sealed class EventSubscriptions : IDisposable
{
    private readonly List<Action> _unsubscribers = [];

    public EventSubscriptions Add(Action subscribe, Action unsubscribe)
    {
        subscribe();
        _unsubscribers.Add(unsubscribe);
        return this;
    }

    public void Dispose()
    {
        for (var i = _unsubscribers.Count - 1; i >= 0; i--)
        {
            _unsubscribers[i]();
        }

        _unsubscribers.Clear();
    }
}
