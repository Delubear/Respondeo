// Remembers the window scroll position for the Discover browse lists (Prayers, Devotions, Articles)
// so that drilling into an item and pressing Back returns the visitor to where they were, rather
// than resetting to the top.
//
// Mirrors the approach proven for the Summa browse page: take manual control of scroll restoration,
// save the position in the capture phase of a link click (before Blazor resets it to the top), and
// re-apply the target across a short settle window because <FocusOnNavigate> otherwise snaps the
// page back to the top a frame or two after we restore. State is keyed per list so each Discover
// page keeps its own position, and stored in sessionStorage so it survives the component remount.
window.respondeoDiscoverBrowse = (function () {
    var SCROLL_PREFIX = 'respondeo.discoverBrowseScroll.';

    function readScroll(key) {
        try {
            var raw = sessionStorage.getItem(SCROLL_PREFIX + key);
            var value = raw ? parseInt(raw, 10) : 0;
            return isNaN(value) ? 0 : value;
        } catch (e) {
            return 0;
        }
    }

    return {
        // Records the current scroll position for a list. Called in the capture phase of a link click
        // (see below) so it captures the true position before Blazor's navigation resets it.
        save: function (key) {
            try {
                sessionStorage.setItem(SCROLL_PREFIX + key, String(Math.round(window.scrollY)));
            } catch (e) { }
        },
        // Restores the remembered scroll position for a list by delegating to the shared restore
        // helper (window.respondeoScrollRestore), which polls across a settle window to survive both
        // the async content height and Blazor's <FocusOnNavigate> reset. See scroll-store.js.
        restoreScroll: function (key) {
            window.respondeoScrollRestore(readScroll(key));
        },
        // Forgets the remembered scroll for a list (used when the visitor leaves the Discover area).
        clear: function (key) {
            try {
                sessionStorage.removeItem(SCROLL_PREFIX + key);
            } catch (e) { }
        }
    };
})();

// Capture the scroll position the instant the visitor clicks a link that navigates away from a
// Discover browse list. Capture phase runs before Blazor's delegated click handler resets scroll to
// the top, so it records the true departure position. The list root carries
// [data-discover-browse="<key>"] so we know which list to save under.
(function () {
    document.addEventListener('click', function (e) {
        if (e.defaultPrevented || e.button !== 0 || e.metaKey || e.ctrlKey || e.shiftKey || e.altKey) {
            return;
        }
        var anchor = e.target && e.target.closest ? e.target.closest('a[href]') : null;
        if (!anchor) {
            return;
        }
        var browse = document.querySelector('[data-discover-browse]');
        if (!browse) {
            return;
        }
        var key = browse.getAttribute('data-discover-browse');
        if (key) {
            window.respondeoDiscoverBrowse.save(key);
        }
    }, true);
})();
