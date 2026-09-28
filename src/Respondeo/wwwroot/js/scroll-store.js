// Shared scroll-restoration helper for the browse pages (Summa, Credo). The Summa and Credo browse
// scripts each remember a scroll position in sessionStorage and re-apply it on Back; the actual
// re-apply logic is identical between them and lives here so there is a single source of truth.
//
// Two things fight a naive scrollTo, so this does more than one call:
//   1. The browse content renders asynchronously after remount, so the page has no height yet when
//      Blazor first asks us to restore - an early scrollTo would clamp to 0.
//   2. Blazor's <FocusOnNavigate> focuses #content just after navigation (including Back), and
//      focusing that container scrolls it into view, snapping us back to the top a frame or two
//      AFTER we've restored (the visible "flicker then reset").
// So we keep re-applying the target across a short settle window, overriding that focus reset, then
// stop. If the visitor starts scrolling themselves we bail immediately so we never fight their input.
window.respondeoScrollRestore = (function () {
    // Take manual control of scroll restoration. By default the browser restores scroll on Back/
    // Forward itself, but because the browse content renders asynchronously after remount there is no
    // page height at that moment, so the browser "restores" to 0 - and it does so AFTER our own poll
    // runs, stomping the correct position. Owning it manually makes this helper the single source of
    // truth.
    if ('scrollRestoration' in history) {
        history.scrollRestoration = 'manual';
    }

    // Restores the window to `target`, polling across animation frames until the async content is
    // tall enough to honour it, then enforcing it for a short settle window. No-op for target <= 0.
    return function respondeoScrollRestore(target) {
        if (!(target > 0)) {
            return;
        }

        var settleFrames = 20;      // ~330ms: long enough to outlast FocusOnNavigate's reset.
        var maxFrames = 90;         // ~1.5s hard cap while waiting for async height.
        var frame = 0;
        var reached = 0;
        var userScrolled = false;

        function onUserScroll() {
            // A real user gesture (wheel/touch/key) means hands off - stop enforcing.
            userScrolled = true;
        }
        window.addEventListener('wheel', onUserScroll, { passive: true, once: true });
        window.addEventListener('touchmove', onUserScroll, { passive: true, once: true });
        window.addEventListener('keydown', onUserScroll, { once: true });

        function cleanup() {
            window.removeEventListener('wheel', onUserScroll);
            window.removeEventListener('touchmove', onUserScroll);
            window.removeEventListener('keydown', onUserScroll);
        }

        (function tick() {
            if (userScrolled) {
                cleanup();
                return;
            }

            var maxScroll = document.documentElement.scrollHeight - window.innerHeight;
            var clamped = Math.min(target, Math.max(maxScroll, 0));
            window.scrollTo(0, clamped);
            frame++;

            // Once the page is tall enough to honour the target, keep enforcing for a settle window
            // to override the post-navigation focus reset, then stop.
            if (maxScroll >= target) {
                reached++;
                if (reached >= settleFrames) {
                    cleanup();
                    return;
                }
            } else if (frame >= maxFrames) {
                cleanup();
                return;
            }

            requestAnimationFrame(tick);
        })();
    };
})();
