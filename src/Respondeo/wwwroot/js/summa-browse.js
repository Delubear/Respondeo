// Persists the Summa browse accordion (which parts and treatises are expanded) in sessionStorage,
// entirely outside Blazor.
//
// Why this lives in JS rather than a Blazor @ontoggle handler: a <details> element is natively
// browser-controlled. If Blazor handles its `toggle` event it re-renders, which re-applies the
// `open` attribute, which makes the browser fire `toggle` again - an endless render/toggle feedback
// loop (the "screen jumps forever" bug). By recording toggles here and never notifying Blazor, the
// component renders `open` exactly once from the restored snapshot and then leaves the element
// alone, so no loop can occur.
window.respondeoSummaBrowse = (function () {
    var KEY = 'respondeo.summaBrowseOpen';
    var SCROLL_KEY = 'respondeo.summaBrowseScroll';

    // Take manual control of scroll restoration. By default the browser restores scroll on Back/
    // Forward itself, but because the Summa content renders asynchronously after remount there is no
    // page height at that moment, so the browser "restores" to 0 - and it does so AFTER our own
    // restore poll runs, stomping the correct position. Owning it manually lets restoreScroll() be
    // the single source of truth.
    if ('scrollRestoration' in history) {
        history.scrollRestoration = 'manual';
    }

    function readSet() {
        try {
            var raw = sessionStorage.getItem(KEY);
            return raw ? JSON.parse(raw) : [];
        } catch (e) {
            return [];
        }
    }

    function writeSet(ids) {
        try {
            sessionStorage.setItem(KEY, JSON.stringify(ids));
        } catch (e) { }
    }

    function readScroll() {
        try {
            var raw = sessionStorage.getItem(SCROLL_KEY);
            var value = raw ? parseInt(raw, 10) : 0;
            return isNaN(value) ? 0 : value;
        } catch (e) {
            return 0;
        }
    }

    return {
        // The ids of every currently-expanded section (read synchronously by the page at mount to
        // seed the initial `open` attributes).
        getOpen: function () {
            return readSet();
        },
        // Marks a section open or closed. Used both by the toggle listener below and by the page to
        // seed a part-scoped view (/summa/part/...) as open.
        setOpen: function (id, open) {
            var ids = readSet();
            var i = ids.indexOf(id);
            if (open && i === -1) {
                ids.push(id);
                writeSet(ids);
            } else if (!open && i !== -1) {
                ids.splice(i, 1);
                writeSet(ids);
            }
        },
        // Restores the remembered scroll position. Two things fight us here, so this does more than
        // a single scrollTo:
        //  1. The Summa content renders asynchronously after remount, so the page has no height yet
        //     when Blazor first calls this - an early scrollTo would clamp to 0.
        //  2. Blazor's <FocusOnNavigate> focuses #content just after navigation (including Back), and
        //     focusing that container scrolls it into view, snapping us back to the top a frame or
        //     two AFTER we've restored (the visible "flicker then reset").
        // So we keep re-applying the target for a short settle window, overriding that focus reset,
        // then stop. If the visitor starts scrolling themselves we bail immediately so we never fight
        // their input.
        restoreScroll: function () {
            var target = readScroll();
            if (target <= 0) {
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

                // Once the page is tall enough to honour the target, keep enforcing for a settle
                // window to override the post-navigation focus reset, then stop.
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
        },
        clear: function () {
            sessionStorage.removeItem(KEY);
            try {
                sessionStorage.removeItem(SCROLL_KEY);
            } catch (e) { }
        }
    };
})();

// Record the window scroll position the instant the visitor clicks a link that navigates away from
// the browse page, so pressing Back can return them to where they were.
//
// Why capture-phase click rather than a continuous scroll listener: Blazor resets the window scroll
// to the top when it handles an in-app navigation, and it does so while the old Summa DOM (still
// carrying [data-summa-section]) is present. A scroll listener would therefore immediately overwrite
// the saved position with 0. Capturing on the click - in the capture phase, before Blazor's own
// delegated handler runs - records the true position at the moment of departure. Reaching the Summa
// again always happens via Back, so this is the only save point that matters.
(function () {
    var SCROLL_KEY = 'respondeo.summaBrowseScroll';

    document.addEventListener('click', function (e) {
        // Only care about primary clicks that will actually navigate.
        if (e.defaultPrevented || e.button !== 0 || e.metaKey || e.ctrlKey || e.shiftKey || e.altKey) {
            return;
        }
        var anchor = e.target && e.target.closest ? e.target.closest('a[href]') : null;
        if (!anchor) {
            return;
        }
        // Only record while the browse markup is on the page, so unrelated pages don't clobber it.
        if (!document.querySelector('[data-summa-section]')) {
            return;
        }
        try {
            sessionStorage.setItem(SCROLL_KEY, String(Math.round(window.scrollY)));
        } catch (err) { }
    }, true);
})();


// The `toggle` event does not bubble, so listen in the capture phase to catch it from any
// <details data-summa-section> on the page. Recording here keeps persistence purely in the DOM
// layer; Blazor is never notified, so it never re-renders or re-applies `open`.
document.addEventListener('toggle', function (e) {
    var el = e.target;
    if (!el || el.tagName !== 'DETAILS') {
        return;
    }
    var id = el.getAttribute('data-summa-section');
    if (id) {
        window.respondeoSummaBrowse.setOpen(id, el.open);
    }
}, true);
