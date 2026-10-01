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
window.respondeoA11y = {
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

// Smoothly brings an element into view by its id.
// Used to reveal a freshly opened accordion section (or a deep-linked one on load) when it sits below the fold.
// We offset by the sticky breadcrumb's height so the section header isn't left hidden underneath it (scrollIntoView block:'start' would tuck it behind the bar).
// Respects prefers - reduced - motion.
window.respondeoScroll = {
    intoView: function (id) {
        var el = document.getElementById(id);
        if (!el) {
            return;
        }
        var reduce = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
        // Defer until after layout has settled. On long pages the target section may not have its
        // final height/position on the first frame (its body is injected as raw HTML), so measuring
        // immediately can scroll short of the anchor. A double rAF waits for the browser to finish
        // laying out the content before we measure and scroll.
        requestAnimationFrame(function () {
            requestAnimationFrame(function () {
                var target = document.getElementById(id);
                if (!target) {
                    return;
                }
                var breadcrumb = document.querySelector('.breadcrumb');
                // Leave the sticky breadcrumb's height plus a small breathing gap above the target so
                // its header (and the first line or two of content) stays visible rather than landing
                // flush against - or tucked just under - the sticky bar.
                var offset = breadcrumb ? breadcrumb.getBoundingClientRect().height : 0;
                var top = target.getBoundingClientRect().top + window.pageYOffset - offset;
                window.scrollTo({ top: top, behavior: reduce ? 'auto' : 'smooth' });
            });
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
// forward instead of toggling the same step off and on again.
window.respondeoFocus = {
    byId: function (id) {
        const el = document.getElementById(id);
        if (el && typeof el.focus === 'function') {
            el.focus({ preventScroll: false });
        }
    },
    // Scrolls an element by id to the centre of its nearest scrollable ancestor. Unlike
    // respondeoScroll.intoView (which moves the window), this uses the native scrollIntoView so it
    // also works inside the devotion player's inner scroll container (.devotion__list) in immersive
    // mode. Deferred with a double rAF so the immersive layout has settled before we measure.
    centreInScrollParent: function (id) {
        requestAnimationFrame(function () {
            requestAnimationFrame(function () {
                const el = document.getElementById(id);
                if (el && typeof el.scrollIntoView === 'function') {
                    const reduce = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
                    el.scrollIntoView({ block: 'center', behavior: reduce ? 'auto' : 'smooth' });
                }
            });
        });
    }
};

// Reports scroll position to a .NET component so a floating control can mirror the back-to-top
// button's reveal threshold (kept in sync with js/scroll.js: shows once scrolled past 400px).
window.respondeoScrollWatch = {
    register: function (ref) {
        const handler = () => {
            const top = document.documentElement.scrollTop || document.body.scrollTop || 0;
            ref.invokeMethodAsync('OnScrolled', top > 400);
        };
        this._handler = handler;
        this._ref = ref;
        window.addEventListener('scroll', handler, { passive: true });
        handler();
    },
    unregister: function () {
        if (this._handler) {
            window.removeEventListener('scroll', this._handler);
            this._handler = null;
        }
        this._ref = null;
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
    iframe.src = 'https://www.youtube-nocookie.com/embed/' + encodeURIComponent(id) + '?autoplay=1';
    iframe.title = 'Embedded YouTube video';
    iframe.allow = 'accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture; web-share';
    iframe.referrerPolicy = 'strict-origin-when-cross-origin';
    iframe.allowFullscreen = true;
    facade.textContent = '';
    facade.appendChild(iframe);
});

