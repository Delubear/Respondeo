// Site-wide browser helpers for Respondeo.
//
// These are loaded after the theme anti-flash snippet in index.html (which must stay inline
// so it runs before first paint). Everything here is invoked at runtime — via JS interop from
// Blazor components or a delegated DOM listener — so it has no before-paint timing constraint.

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

// Brings an element into view. Both helpers wait for layout to settle before measuring: on long
// pages a freshly opened (or deep-linked) section has its body injected as raw HTML, so its final
// height/position isn't known on the first frame - a double rAF lets the browser finish laying it
// out first. Both also respect prefers-reduced-motion.
window.respondeoScroll = {
    // Run `apply(el, reduce)` for element `id` once layout has settled.
    _afterLayout: function (id, apply) {
        requestAnimationFrame(function () {
            requestAnimationFrame(function () {
                var el = document.getElementById(id);
                if (el) {
                    apply(el, window.matchMedia('(prefers-reduced-motion: reduce)').matches);
                }
            });
        });
    },
    // Smoothly scrolls the WINDOW to reveal a section, offsetting the sticky breadcrumb so the
    // header isn't left hidden underneath it. Used to reveal a freshly opened accordion section (or
    // a deep-linked one on load) when it sits below the fold.
    intoView: function (id) {
        this._afterLayout(id, function (el, reduce) {
            var breadcrumb = document.querySelector('.breadcrumb');
            // Leave the sticky breadcrumb's height above the target so its header (and the first
            // line or two of content) stays visible rather than landing flush against - or tucked
            // just under - the sticky bar.
            var offset = breadcrumb ? breadcrumb.getBoundingClientRect().height : 0;
            var top = el.getBoundingClientRect().top + window.pageYOffset - offset;
            window.scrollTo({ top: top, behavior: reduce ? 'auto' : 'smooth' });
        });
    },
    // Centres an element within its nearest scrollable ancestor via native scrollIntoView. Unlike
    // intoView (which moves the window), this also works inside an inner scroll container such as
    // the devotion player's .devotion__list in immersive mode.
    centreInParent: function (id) {
        this._afterLayout(id, function (el, reduce) {
            if (typeof el.scrollIntoView === 'function') {
                el.scrollIntoView({ block: 'center', behavior: reduce ? 'auto' : 'smooth' });
            }
        });
    }
};

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

// Click-to-load for YouTube embeds. Author content renders a lightweight "façade" (thumbnail + play button);
// the heavy YouTube player is only injected when the visitor actually clicks play, so opening an article makes no YouTube requests.
// A single delegated listener covers all current and future façades (content is injected as raw HTML, so per-element Blazor handlers wouldn't bind).
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

