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
    window.scrollTo({ top: 0, behavior: 'smooth' });
}

export function jumpToTop() {
    window.scrollTo(0, 0);
}
