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
                var offset = breadcrumb ? breadcrumb.getBoundingClientRect().height : 0;
                var top = target.getBoundingClientRect().top + window.pageYOffset - offset;
                window.scrollTo({ top: top, behavior: reduce ? 'auto' : 'smooth' });
            });
        });
    }
};

// Opens/closes a native <dialog> by element reference. Native dialogs give us focus trapping,
// Esc-to-close and the ::backdrop for free; Blazor just needs to call the methods via interop.
window.respondeoDialog = {
    show: function (el) {
        if (el && typeof el.showModal === 'function' && !el.open) {
            el.showModal();
        }
    },
    close: function (el) {
        if (el && typeof el.close === 'function' && el.open) {
            el.close();
        }
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

// Records where a Summa cross-reference jump started from, so the destination question page can
// offer a breadcrumb back to the exact article the reader was in. The reference links are raw
// injected anchors (no Blazor handlers), so a single delegated listener captures the click before
// navigation. We store the origin question id, the article the reader was reading (the nearest
// open <details class="summa-article"> above the click, falling back to the clicked ref's own
// article container), and a display label built from the page's breadcrumb current title.
window.respondeoSummaRef = {
    read: function () {
        try {
            var raw = sessionStorage.getItem('respondeo.summaRefOrigin');
            return raw ? JSON.parse(raw) : null;
        } catch (e) {
            return null;
        }
    },
    clear: function () {
        sessionStorage.removeItem('respondeo.summaRefOrigin');
    }
};

document.addEventListener('click', function (e) {
    var ref = e.target.closest ? e.target.closest('a.summa-ref') : null;
    if (!ref) {
        return;
    }

    // The current question id comes from the /summa/{id} path.
    var match = window.location.pathname.match(/\/summa\/([^\/#?]+)/);
    if (!match) {
        return;
    }
    var questionId = match[1];

    // Only record an origin when the jump actually leaves this question. A lone article reference
    // within the same question navigates in-page (no page change), so there is nothing to come
    // "back" to and no destination load to consume/clear the stored origin.
    var href = ref.getAttribute('href') || '';
    var targetMatch = href.match(/summa\/([^\/#?]+)/);
    if (!targetMatch || targetMatch[1] === questionId) {
        return;
    }

    // Which article was the reader in? Prefer the article section containing the clicked link.
    var articleEl = ref.closest ? ref.closest('.summa-article') : null;
    var articleNumber = null;
    if (articleEl && articleEl.id) {
        var idMatch = articleEl.id.match(/^article-(\d+)$/);
        if (idMatch) {
            articleNumber = parseInt(idMatch[1], 10);
        }
    }

    var current = document.querySelector('.breadcrumb__current');
    var label = current ? current.textContent.trim() : 'the previous question';

    var origin = {
        questionId: questionId,
        articleNumber: articleNumber,
        label: label
    };
    sessionStorage.setItem('respondeo.summaRefOrigin', JSON.stringify(origin));
});

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
