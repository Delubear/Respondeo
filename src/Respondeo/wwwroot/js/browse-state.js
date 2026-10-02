// Persists browse-page UI state in sessionStorage, entirely outside Blazor, so that drilling into an
// item and pressing Back returns the visitor to exactly where they were. Two concerns live here, both
// keyed per browse list (Summa, Prayers, Devotions, Articles) via a [data-browse-scroll="<key>"] root:
//
//   1. Scroll position - every browse list uses this.
//   2. Accordion open/closed state - optional; only lists whose rows are <details> with a
//      [data-browse-section="<id>"] attribute opt in. Presence of that attribute IS the opt-in, so a
//      flat list (Prayers/Devotions/Articles) simply never records any open state.
//
// Why the accordion state lives in JS rather than a Blazor @ontoggle handler: a <details> element is
// natively browser-controlled. If Blazor handles its `toggle` event it re-renders, which re-applies
// the `open` attribute, which makes the browser fire `toggle` again - an endless render/toggle
// feedback loop (the "screen jumps forever" bug). By recording toggles here and never notifying
// Blazor, the component renders `open` exactly once from the restored snapshot and then leaves the
// element alone, so no loop can occur.
window.respondeoBrowseState = (function () {
    var SCROLL_PREFIX = 'respondeo.browseScroll.';
    var OPEN_PREFIX = 'respondeo.browseOpen.';

    function readScroll(key) {
        try {
            var raw = sessionStorage.getItem(SCROLL_PREFIX + key);
            var value = raw ? parseInt(raw, 10) : 0;
            return isNaN(value) ? 0 : value;
        } catch (e) {
            return 0;
        }
    }

    function readSet(key) {
        try {
            var raw = sessionStorage.getItem(OPEN_PREFIX + key);
            return raw ? JSON.parse(raw) : [];
        } catch (e) {
            return [];
        }
    }

    function writeSet(key, ids) {
        try {
            sessionStorage.setItem(OPEN_PREFIX + key, JSON.stringify(ids));
        } catch (e) { }
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
        // The ids of every currently-expanded section for a list (read synchronously by the page at
        // mount to seed the initial `open` attributes).
        getOpen: function (key) {
            return readSet(key);
        },
        // Marks a section open or closed. Used both by the toggle listener below and by a page to
        // seed a scoped view (e.g. /summa/part/...) as open.
        setOpen: function (key, id, open) {
            var ids = readSet(key);
            var i = ids.indexOf(id);
            if (open && i === -1) {
                ids.push(id);
                writeSet(key, ids);
            } else if (!open && i !== -1) {
                ids.splice(i, 1);
                writeSet(key, ids);
            }
        },
        // Forgets all remembered state (scroll and accordion) for a list. Used when the visitor
        // leaves the browse area. Removing a key that was never written is harmless.
        clear: function (key) {
            try {
                sessionStorage.removeItem(SCROLL_PREFIX + key);
                sessionStorage.removeItem(OPEN_PREFIX + key);
            } catch (e) { }
        }
    };
})();

// Capture the scroll position the instant the visitor clicks a link that navigates away from a
// browse list. Capture phase runs before Blazor's delegated click handler resets scroll to the top,
// so it records the true departure position. The list root carries [data-browse-scroll="<key>"] so
// we know which list to save under.
(function () {
    document.addEventListener('click', function (e) {
        if (e.defaultPrevented || e.button !== 0 || e.metaKey || e.ctrlKey || e.shiftKey || e.altKey) {
            return;
        }
        var anchor = e.target && e.target.closest ? e.target.closest('a[href]') : null;
        if (!anchor) {
            return;
        }
        var browse = document.querySelector('[data-browse-scroll]');
        if (!browse) {
            return;
        }
        var key = browse.getAttribute('data-browse-scroll');
        if (key) {
            window.respondeoBrowseState.save(key);
        }
    }, true);
})();

// The `toggle` event does not bubble, so listen in the capture phase to catch it from any
// <details data-browse-section> on the page. The owning list key is read from the nearest
// [data-browse-scroll] ancestor - the same root the scroll feature uses - so an accordion list
// needs no extra wiring. Recording here keeps persistence purely in the DOM layer; Blazor is never
// notified, so it never re-renders or re-applies `open`.
document.addEventListener('toggle', function (e) {
    var el = e.target;
    if (!el || el.tagName !== 'DETAILS') {
        return;
    }
    var id = el.getAttribute('data-browse-section');
    if (!id) {
        return;
    }
    var root = el.closest ? el.closest('[data-browse-scroll]') : null;
    var key = root ? root.getAttribute('data-browse-scroll') : null;
    if (key) {
        window.respondeoBrowseState.setOpen(key, id, el.open);
    }
}, true);
