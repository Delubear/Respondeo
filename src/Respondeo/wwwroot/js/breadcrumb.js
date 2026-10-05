// Breadcrumb horizontal-scroll behavior.
//
// The trail renders the stage root at the LEFT and the current node at the RIGHT. On
// narrow screens a long trail would wrap onto several rows, so instead we keep it on a
// single line and let it scroll sideways. This module:
//   1. Starts the reader at the RIGHT (the most recent crumb) so "where am I" is visible first.
//   2. Toggles the edge chevron buttons on whenever, and whichever way, the list can scroll.

const state = new WeakMap();

function listOf(root) {
    return root.querySelector('.breadcrumb__list');
}

// Toggle the can-scroll classes (which reveal the edge chevrons) based on how far the list is
// scrolled. A small tolerance absorbs sub-pixel rounding at the extremes (some browsers report a
// fractional scrollLeft that never exactly equals the max), which otherwise left a stray chevron.
function updateChevrons(root) {
    const list = listOf(root);
    if (!list) {
        return;
    }
    const tolerance = 2;
    const max = list.scrollWidth - list.clientWidth;
    const atStart = list.scrollLeft <= tolerance;
    const atEnd = list.scrollLeft >= max - tolerance;
    const canScroll = max > tolerance;

    root.classList.toggle('can-scroll-left', canScroll && !atStart);
    root.classList.toggle('can-scroll-right', canScroll && !atEnd);
}

// Jump to the most recent crumb (the right-most) without a visible animation.
function scrollToCurrent(root) {
    const list = listOf(root);
    if (!list) {
        return;
    }
    list.scrollLeft = list.scrollWidth;
    updateChevrons(root);
}

// Nudge the trail one "page" (most of a viewport) in the given direction (-1 left, 1 right).
// Used by the edge chevron buttons for readers who don't realise the row is swipeable.
export function step(root, direction) {
    const list = listOf(root);
    if (!list) {
        return;
    }
    const amount = Math.max(list.clientWidth * 0.75, 80) * Math.sign(direction);
    list.scrollBy({ left: amount, behavior: 'smooth' });
}

export function init(root) {
    if (!root || state.has(root)) {
        return;
    }

    let scrollScheduled = false;
    const onScroll = () => {
        if (scrollScheduled) {
            return;
        }
        scrollScheduled = true;
        requestAnimationFrame(() => {
            scrollScheduled = false;
            updateChevrons(root);
        });
    };

    const onResize = () => scrollToCurrent(root);

    // Desktop mice emit vertical wheel deltas and the scrollbar is hidden, so without this a
    // mouse user could only move the trail via the chevrons. Translate a (predominantly)
    // vertical wheel over the row into horizontal scrolling. Whenever the trail can scroll at
    // all we fully consume the wheel (preventDefault + stopPropagation) so the gesture never
    // leaks out to scroll/overscroll the page, which felt jarring while reading the trail.
    const onWheel = (event) => {
        const list = listOf(root);
        if (!list) {
            return;
        }
        const max = list.scrollWidth - list.clientWidth;
        if (max <= 1) {
            return;
        }
        const delta = Math.abs(event.deltaY) > Math.abs(event.deltaX) ? event.deltaY : event.deltaX;
        if (delta === 0) {
            return;
        }
        event.preventDefault();
        event.stopPropagation();
        list.scrollLeft = Math.max(0, Math.min(max, list.scrollLeft + delta));
    };

    const list = listOf(root);
    if (list) {
        list.addEventListener('scroll', onScroll, { passive: true });
        list.addEventListener('wheel', onWheel, { passive: false });
    }
    window.addEventListener('resize', onResize, { passive: true });

    // Content can reflow after the first paint (web font swap, late-resolved crumb titles),
    // which changes scrollWidth. Recompute the chevrons against the real width so neither a
    // phantom chevron lingers nor a needed one stays hidden. Because the chevrons reserve their
    // space permanently (CSS toggles visibility, not layout), this never resizes the list.
    let observer = null;
    if (list && typeof ResizeObserver !== 'undefined') {
        observer = new ResizeObserver(() => updateChevrons(root));
        observer.observe(list);
        for (const item of list.children) {
            observer.observe(item);
        }
    }

    state.set(root, { onScroll, onResize, onWheel, list, observer });

    // Position after paint so the list has its final scrollWidth.
    requestAnimationFrame(() => scrollToCurrent(root));
}

// Re-run after the crumbs change (navigation to a new node): land on the current crumb
// again and recompute the chevrons.
export function refresh(root) {
    if (!root) {
        return;
    }
    requestAnimationFrame(() => scrollToCurrent(root));
}

export function dispose(root) {
    const entry = root && state.get(root);
    if (!entry) {
        return;
    }
    if (entry.list) {
        entry.list.removeEventListener('scroll', entry.onScroll);
        entry.list.removeEventListener('wheel', entry.onWheel);
    }
    if (entry.observer) {
        entry.observer.disconnect();
    }
    window.removeEventListener('resize', entry.onResize);
    state.delete(root);
}
