// Site-wide browser helpers for Respondeo.
//
// These are loaded after the theme anti-flash snippet in index.html (which must stay inline
// so it runs before first paint). Everything here is invoked at runtime (via JS interop from
// Blazor components or a delegated DOM listener), so it has no before-paint timing constraint.

// Updates the address bar without triggering a Blazor navigation (no focus shift or scroll).
// Used to keep accordion state shareable while staying put on the page.
window.respondeoUrl = {
    replace: function (url) {
        history.replaceState(history.state, '', url);
    }
};

// Announces in-app route changes to assistive technology. Blazor's FocusOnNavigate moves focus to
// the main content on navigation, but a SPA route change is silent to screen readers unless we also
// post the new page name to a live region. We own a single visually-hidden polite live region and
// write the new document.title into it after navigation. The short delay lets the routed page's
// <PageTitle> update document.title first so we announce the destination, not the previous page.
window.respondeoAccessibility = {
    announce: function () {
        var region = document.getElementById('route-announcer');
        if (!region) {
            region = document.createElement('div');
            region.id = 'route-announcer';
            region.setAttribute('role', 'status');
            region.setAttribute('aria-live', 'polite');
            region.setAttribute('aria-atomic', 'true');
            // Visually hidden but available to assistive technology.
            region.style.cssText = 'position:absolute;width:1px;height:1px;margin:-1px;padding:0;'
                + 'overflow:hidden;clip:rect(0 0 0 0);clip-path:inset(50%);white-space:nowrap;border:0;';
            document.body.appendChild(region);
        }
        // Let the routed page set its <PageTitle> before we read it, then announce the destination.
        setTimeout(function () {
            region.textContent = document.title;
        }, 100);
    }
};

// Focuses the main content region after navigation (for screen readers) without scrolling it into view.
window.respondeoFocusContent = function () {
    var el = document.getElementById('content');
    if (el) {
        el.focus({ preventScroll: true });
    }
};

// Blazor resets scroll with window.scrollTo(0, 0) on the first render batch after a link navigation. That batch can be
// the layout reacting to LocationChanged while the router is still awaiting OnNavigateAsync, so the OLD page gets painted
// at the top for a frame (a visible flash). ScrollCoordinator owns every landing, so swallow that positional (0, 0) call;
// all app scrolling below uses the options-object form.
(function () {
    var nativeScrollTo = window.scrollTo.bind(window);
    window.scrollTo = function (x, y) {
        if (arguments.length === 2 && x === 0 && y === 0) {
            return;
        }
        nativeScrollTo.apply(window, arguments);
    };
})();

// All window scrolling lives here. Landings measure the target synchronously (no waiting on layout)
// because callers only invoke them after the target has rendered (see ScrollCoordinator.cs).
window.respondeoScroll = (function () {
    // How long a remembered position (restore) wins over a generic top/content landing.
    var RESTORE_PRIORITY_MS = 500;
    // Frame budgets for the rAF loops that chase a moving or not-yet-reachable target.
    var HOLD_STILL_FRAMES = 10;
    var MAX_CHASE_FRAMES = 60;
    var reduced = function () {
        return window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    };

    // Document-relative top of the content region. Uses the layout offset (offsetTop chain), not
    // getBoundingClientRect(), because the sticky breadcrumb's rect reads 0 once it is pinned.
    function contentTop() {
        var content = document.querySelector('.content');
        if (!content) {
            return 0;
        }

        var top = 0;
        for (var node = content; node; node = node.offsetParent) {
            top += node.offsetTop;
        }

        var inner = content.querySelector('.content__inner');
        if (inner && inner.querySelector('.breadcrumb')) {
            top += parseFloat(getComputedStyle(inner).paddingTop) || 0;
        }
        return top;
    }

    function scrollWindow(top, smooth) {
        // Any explicit scroll supersedes a content hold still running from the last navigation.
        if (stopHold) {
            stopHold();
        }
        window.scrollTo({ top: Math.max(top, 0), behavior: smooth && !reduced() ? 'smooth' : 'auto' });
    }

    // Leaving Home, the masthead shrinks from its tall home layout to the slim inner layout over a short
    // transition, which moves the content region up after we've scrolled to it. Follow it until it stops
    // moving (or the reader takes over) so the landing ends past the sub-navigation.
    var stopHold = null;
    var restoredAt = -Infinity;
    function holdContent() {
        if (stopHold) {
            stopHold();
        }

        var still = 0;
        var frames = 0;
        var rafId = 0;
        var events = ['wheel', 'touchstart', 'keydown', 'mousedown'];
        var stop = function () {
            cancelAnimationFrame(rafId);
            events.forEach(function (type) { window.removeEventListener(type, stop, true); });
            stopHold = null;
        };
        // Re-apply whenever we're not at the content top: it can move (masthead resizing) or be
        // unreachable at first (the page still too short, so the browser clamped the scroll).
        var tick = function () {
            var top = contentTop();
            if (Math.abs(window.scrollY - top) > 1) {
                still = 0;
                window.scrollTo({ top: top, behavior: 'auto' });
            } else {
                still++;
            }

            if (still >= HOLD_STILL_FRAMES || ++frames >= MAX_CHASE_FRAMES) {
                stop();
                return;
            }
            rafId = requestAnimationFrame(tick);
        };

        events.forEach(function (type) { window.addEventListener(type, stop, { capture: true, passive: true }); });
        stopHold = stop;
        rafId = requestAnimationFrame(tick);
    }

    return {
        // Lands on `target`: 'top' (page top), 'content' (content region), or an element id.
        // Instant unless `smooth`; `center` centers the element instead of aligning its top
        // (CSS scroll-margin-top keeps aligned targets clear of the sticky breadcrumb).
        land: function (target, smooth, center) {
            // A browse list just restored the reader's remembered position (Back); don't override it
            // with the generic page-top/content landing, whichever of the two runs first.
            if ((target === 'top' || target === 'content') && performance.now() - restoredAt < RESTORE_PRIORITY_MS) {
                return;
            }

            if (target === 'top') {
                scrollWindow(0, smooth);
                return;
            }

            if (target === 'content') {
                scrollWindow(contentTop(), smooth);
                holdContent();
                return;
            }

            var el = document.getElementById(target);
            if (!el) {
                return;
            }

            if (center) {
                if (stopHold) {
                    stopHold();
                }
                el.scrollIntoView({ block: 'center', behavior: smooth && !reduced() ? 'smooth' : 'auto' });
                return;
            }

            // Measure the layout position (offsetTop chain), not scrollIntoView/getBoundingClientRect:
            // an entering page's fade-and-rise transform would otherwise make us overshoot.
            var top = 0;
            for (var node = el; node; node = node.offsetParent) {
                top += node.offsetTop;
            }
            scrollWindow(top - (parseFloat(getComputedStyle(el).scrollMarginTop) || 0), smooth);
        },
        // Smoothly brings element `id` into view after an in-page change (opening an article/section).
        // Runs on the next frame so the just-opened content has laid out. Always glides (scrollWindow
        // still honors reduced motion).
        reveal: function (id) {
            requestAnimationFrame(function () {
                var el = document.getElementById(id);
                if (!el) {
                    return;
                }

                var breadcrumb = document.querySelector('.breadcrumb');
                var offset = breadcrumb ? breadcrumb.getBoundingClientRect().height : 0;
                var top = el.getBoundingClientRect().top + window.pageYOffset - offset;
                scrollWindow(top, true);
            });
        },
        // Centers element `id` within its scrollable parent (e.g. the devotion player's list).
        centreInParent: function (id) {
            var el = document.getElementById(id);
            if (el) {
                el.scrollIntoView({ block: 'center', behavior: reduced() ? 'auto' : 'smooth' });
            }
        },
        top: function () {
            scrollWindow(0, true);
        },
        // Returns to a remembered position (Back to a browse list). Cancels any content hold and
        // retries briefly while the list grows tall enough to reach the position.
        restore: function (target) {
            restoredAt = performance.now();
            if (stopHold) {
                stopHold();
            }

            var frames = 0;
            (function tick() {
                window.scrollTo({ top: target, behavior: 'auto' });
                if (Math.abs(window.scrollY - target) > 1 && ++frames < MAX_CHASE_FRAMES) {
                    requestAnimationFrame(tick);
                }
            })();
        }
    };
})();

// Opens/closes a native <dialog> by element reference. Native dialogs give us focus trapping,
// Esc-to-close and the ::backdrop for free; Blazor just needs to call the methods via interop.
// showModal() does not lock the page behind it, so we also freeze <body> scrolling while open
// (restoring the previous value on close, including when the user dismisses with Esc).
window.respondeoDialog = {
    show: function (el) {
        if (el && typeof el.showModal === 'function' && !el.open) {
            el.showModal();
            this._lockScroll();
            if (!el._respondeoScrollLock) {
                el._respondeoScrollLock = true;
                el.addEventListener('close', () => this._unlockScroll());
            }
        }
    },
    close: function (el) {
        if (el && typeof el.close === 'function' && el.open) {
            el.close();
        }
    },
    _lockScroll: function () {
        if (this._prevOverflow === undefined) {
            this._prevOverflow = document.body.style.overflow;
        }
        document.body.style.overflow = 'hidden';
    },
    _unlockScroll: function () {
        document.body.style.overflow = this._prevOverflow || '';
        this._prevOverflow = undefined;
    }
};

// Toggles a body-level "immersive" class so the layout can hide its chrome (masthead, nav, footer)
// while the reader is actively praying a devotion. Kept as a body class rather than component state so
// the shared MainLayout can react without a cascading parameter. Always paired: enter on begin, leave
// on exit/dispose, so the chrome never stays hidden after leaving the devotion.
window.respondeoImmersive = {
    enter: function () {
        document.body.classList.add('is-immersive');
    },
    leave: function () {
        document.body.classList.remove('is-immersive');
    }
};

// Focuses an element by id, keeping it in view. Used by the devotion player to move keyboard
// focus onto the next step after one is marked prayed, so pressing Space/Enter walks the thread
// forward instead of toggling the same step off and on again. (Scrolling an element into view lives
// in respondeoScroll; this object is purely about focus.)
window.respondeoFocus = {
    byId: function (id) {
        const el = document.getElementById(id);
        if (el && typeof el.focus === 'function') {
            el.focus({ preventScroll: false });
        }
    }
};

// Listens for the embedded Tally feedback form's completion. Tally posts a window message whose
// `data` is a JSON string carrying { event: "Tally.FormSubmitted", ... } once the visitor submits.
// We relay just that moment back to the Feedback component (via its [JSInvokable] OnSubmitted) so it
// can swap the "Back to options" control for a "Close" one. listen() returns a subscription id;
// stopListening(id) removes the sole handler so we don't leak listeners across dialog opens.
window.respondeoTally = {
    _subscribers: new Map(), // id -> handler
    _nextId: 1,
    listen: function (ref) {
        const id = this._nextId++;
        const handler = (e) => {
            if (typeof e.data !== 'string') {
                return;
            }
            let payload;
            try {
                payload = JSON.parse(e.data);
            } catch {
                return;
            }
            if (payload && payload.event === 'Tally.FormSubmitted') {
                ref.invokeMethodAsync('OnSubmitted');
            }
        };
        this._subscribers.set(id, handler);
        window.addEventListener('message', handler);
        return id;
    },
    stopListening: function (id) {
        const handler = this._subscribers.get(id);
        if (handler) {
            window.removeEventListener('message', handler);
            this._subscribers.delete(id);
        }
    }
};

// Click-to-load for YouTube embeds. Author content renders a lightweight "facades" (thumbnail + play button);
// the heavy YouTube player is only injected when the visitor actually clicks play, so opening an article makes no YouTube requests.
// A single delegated listener covers all current and future facades (content is injected as raw HTML, so per-element Blazor handlers wouldn't bind).
document.addEventListener('click', function (e) {
    var facade = e.target.closest ? e.target.closest('.video-facade') : null;
    if (!facade || facade.dataset.loaded) {
        return;
    }
    var id = facade.getAttribute('data-youtube');
    if (!id) {
        return;
    }
    facade.dataset.loaded = 'true';
    var iframe = document.createElement('iframe');
    var src = 'https://www.youtube-nocookie.com/embed/' + encodeURIComponent(id) + '?autoplay=1';
    var start = facade.getAttribute('data-start');
    if (start) {
        src += '&start=' + encodeURIComponent(start);
    }
    iframe.src = src;
    iframe.title = 'Embedded YouTube video';
    iframe.allow = 'accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture; web-share';
    iframe.referrerPolicy = 'strict-origin-when-cross-origin';
    iframe.allowFullscreen = true;
    facade.textContent = '';
    facade.appendChild(iframe);
});

// Dropdown menus built on <details class="liturgy-menu"> close when the reader clicks anywhere outside them.
// Delegated so it covers menus rendered later (e.g. inside a dialog opened after page load).
document.addEventListener('click', function (e) {
    var openMenus = document.querySelectorAll('details.liturgy-menu[open]');
    for (var i = 0; i < openMenus.length; i++) {
        if (!openMenus[i].contains(e.target)) {
            openMenus[i].removeAttribute('open');
        }
    }
});

