// Summa cross-reference navigation helpers.
//
// Split out of site.js: everything here is specific to the Summa reading experience (the raw
// injected "summa-ref" anchors inside rendered article HTML). Loaded globally alongside the other
// site helpers so the delegated listener is always present when a Summa question page is shown.

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
    },
    // The active /summa/{id} page registers itself so same-question reference clicks can be handled
    // directly in the component (open + scroll), rather than relying on a native fragment jump that
    // wouldn't expand the collapsed target article.
    registerPage: function (ref) {
        this._page = ref;
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

    var href = ref.getAttribute('href') || '';
    var targetMatch = href.match(/summa\/([^\/#?]+)/);

    // A reference that stays within this question navigates in-page. Fragment-only clicks on these raw
    // injected anchors don't reliably expand the (collapsed) target article, so hand the click to the
    // registered component instead of letting the browser do a bare hash jump.
    if (targetMatch && targetMatch[1] === questionId) {
        // The fragment may be a bare article ("#article-3") or a sub-anchor within it
        // ("#article-3-reply-2"); pass the whole fragment so the component opens the article and
        // scrolls to the exact objection/reply.
        var fragmentMatch = href.match(/#(article-\d+[^\s?]*)/);
        if (fragmentMatch && window.respondeoSummaRef._page) {
            e.preventDefault();
            window.respondeoSummaRef._page.invokeMethodAsync('OpenArticleFromReference', fragmentMatch[1]);
        }
        return;
    }

    // Only record an origin when the jump actually leaves this question. A lone article reference
    // within the same question navigates in-page (handled above), so there is nothing to come
    // "back" to and no destination load to consume/clear the stored origin.
    if (!targetMatch) {
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
