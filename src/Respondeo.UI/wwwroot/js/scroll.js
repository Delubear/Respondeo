// Scroll tracking: reading-progress and back-to-top visibility. (Scrolling itself lives in
// window.respondeoScroll in site.js; navigation landings are owned by ScrollCoordinator.cs.)
//
// This is the single scroll-position tracker for the whole app. Several components can watch the
// window scroll at once (e.g. the layout's reading-progress bar / back-to-top button AND the Summa
// reading guide's floating control), so register() supports multiple independent subscribers rather
// than a single module-level handler: each call adds its own listener keyed by a subscription id and
// unregister(id) removes just that one.
const subscribers = new Map(); // id -> handler
let nextId = 1;

// The current scroll metrics shared by every subscriber: reading progress (0-100%) and whether the
// page has scrolled past the back-to-top reveal threshold (400px).
function metrics() {
    const doc = document.documentElement;
    const scrollTop = doc.scrollTop || document.body.scrollTop || 0;
    const height = doc.scrollHeight - doc.clientHeight;
    const progress = height > 0 ? (scrollTop / height) * 100 : 0;
    return { progress, showButton: scrollTop > 400 };
}

// Subscribes `ref` to window scroll; its [JSInvokable] OnScroll(double progress, bool showButton) is
// called immediately and on every scroll. Returns a subscription id to pass back to unregister().
export function register(ref) {
    const id = nextId++;
    const handler = () => {
        const { progress, showButton } = metrics();
        ref.invokeMethodAsync('OnScroll', progress, showButton);
    };
    subscribers.set(id, handler);
    window.addEventListener('scroll', handler, { passive: true });
    handler();
    return id;
}

export function unregister(id) {
    const handler = subscribers.get(id);
    if (handler) {
        window.removeEventListener('scroll', handler);
        subscribers.delete(id);
    }
}

