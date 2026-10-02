// Scroll affordances: reading-progress, back-to-top visibility, and scroll reset on navigation.
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

export function scrollToTop() {
    const reduce = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    window.scrollTo({ top: 0, behavior: reduce ? 'auto' : 'smooth' });
}

export function jumpToTop() {
    // Used for top-level (masthead) navigation: jump straight to the very top of the page with
    // no animation. Landing at the top (masthead included) is the expected reset for a top-level move.
    const jump = () => window.scrollTo(0, 0);

    // Pin immediately (before the new page paints) so there's no visible scroll movement.
    jump();

    // LocationChanged fires BEFORE the new page renders, and Blazor's
    // <FocusOnNavigate Selector="#content"> focuses the content region AFTER render, which
    // nudges the scroll. Re-assert on a double requestAnimationFrame so we run after that
    // render + focus cycle and remain authoritative.
    requestAnimationFrame(() => requestAnimationFrame(jump));
}

export function jumpToContent() {
    // Used for card/node navigation: settle the viewport at the top of the content region rather
    // than the very top of the page, so the masthead stays out of view and the reader lands on
    // their breadcrumb trail.
    //
    // The breadcrumb is position:sticky; top:0 but sits INSIDE .content__inner, which has a
    // padding-top. Scrolling to .content's top therefore lands a padding's-worth ABOVE the
    // breadcrumb, leaving a gap that only closes once the reader scrolls far enough for the sticky
    // breadcrumb to pin. Prefer the breadcrumb's own offset so it starts flush at the top; fall
    // back to .content (then the page top) on surfaces that have no breadcrumb.
    const scrollToContent = () => {
        const target = document.querySelector('.breadcrumb') || document.querySelector('.content');
        const top = target ? target.getBoundingClientRect().top + window.pageYOffset : 0;
        window.scrollTo(0, top);
    };

    // Pin immediately (before the new page paints) so the masthead never flashes into
    // view. The content element's top is stable regardless of which page is loading.
    scrollToContent();

    // See jumpToTop for why we re-assert after the render + focus cycle.
    requestAnimationFrame(() => requestAnimationFrame(scrollToContent));
}
