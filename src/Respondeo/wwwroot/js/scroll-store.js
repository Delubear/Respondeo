// Shared scroll-restoration helper for the browse pages (Summa, Discover). Each browse list remembers
// its scroll position in sessionStorage and re-applies it on Back via this one function.
//
// The page calls it after its content has rendered, so a single scroll lands exactly; there is no
// settle loop. Scroll restoration is manual so the browser doesn't restore its own (stale) position.
window.respondeoScrollRestore = (function () {
    if ('scrollRestoration' in history) {
        history.scrollRestoration = 'manual';
    }

    return function respondeoScrollRestore(target) {
        if (target > 0) {
            window.respondeoScroll.restore(target);
        }
    };
})();
