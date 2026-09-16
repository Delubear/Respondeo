// Scroll affordances: reading-progress, back-to-top visibility, and scroll reset on navigation.
let handler = null;
let dotNetRef = null;

export function register(ref) {
    dotNetRef = ref;
    handler = () => {
        const doc = document.documentElement;
        const scrollTop = doc.scrollTop || document.body.scrollTop || 0;
        const height = doc.scrollHeight - doc.clientHeight;
        const progress = height > 0 ? (scrollTop / height) * 100 : 0;
        const showButton = scrollTop > 400;
        dotNetRef.invokeMethodAsync('OnScroll', progress, showButton);
    };
    window.addEventListener('scroll', handler, { passive: true });
    handler();
}

export function unregister() {
    if (handler) {
        window.removeEventListener('scroll', handler);
        handler = null;
    }
    dotNetRef = null;
}

export function scrollToTop() {
    const reduce = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    window.scrollTo({ top: 0, behavior: reduce ? 'auto' : 'smooth' });
}

export function jumpToTop() {
    // On navigation, settle the viewport at the top of the content region (which begins
    // with the breadcrumb) rather than the very top of the page, so the masthead stays
    // out of view and the reader lands on their breadcrumb trail.
    const scrollToContent = () => {
        const content = document.querySelector('.content');
        const top = content ? content.getBoundingClientRect().top + window.pageYOffset : 0;
        window.scrollTo(0, top);
    };

    // Pin immediately (before the new page paints) so the masthead never flashes into
    // view. The content element's top is stable regardless of which page is loading.
    scrollToContent();

    // LocationChanged fires BEFORE the new page renders, and Blazor's
    // <FocusOnNavigate Selector="h1"> focuses the new heading AFTER render, which nudges
    // the scroll. Re-assert on a double requestAnimationFrame so we run after that render
    // + focus cycle and remain authoritative.
    requestAnimationFrame(() => requestAnimationFrame(scrollToContent));
}
