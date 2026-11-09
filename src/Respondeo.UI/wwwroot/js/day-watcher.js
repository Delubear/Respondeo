// Day-rollover trigger: notifies .NET when the tab becomes visible again.
//
// The app runs entirely client-side, so a tab left open across midnight keeps showing yesterday's
// liturgical day and saint of the day. Rather than waking a background timer (which drains battery on
// an idle tab for no benefit, since the only thing that ever changes is the date at midnight), we lean
// on the moment the user actually returns to the tab: 'visibilitychange'. When the page becomes visible
// we call back into .NET, which cheaply compares the date and re-renders only if the day truly advanced.
let handler = null;

// Subscribes `ref` to tab visibility; its [JSInvokable] OnBecameVisible() runs each time the tab
// becomes visible. Returns nothing to unregister (there is a single subscriber, the day watcher).
export function register(ref) {
    handler = () => {
        if (document.visibilityState === 'visible') {
            ref.invokeMethodAsync('OnBecameVisible');
        }
    };
    document.addEventListener('visibilitychange', handler);
}

export function unregister() {
    if (handler) {
        document.removeEventListener('visibilitychange', handler);
        handler = null;
    }
}
