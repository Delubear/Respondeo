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
        var breadcrumb = document.querySelector('.breadcrumb');
        var offset = breadcrumb ? breadcrumb.getBoundingClientRect().height : 0;
        var top = el.getBoundingClientRect().top + window.pageYOffset - offset;
        var reduce = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
        window.scrollTo({ top: top, behavior: reduce ? 'auto' : 'smooth' });
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
