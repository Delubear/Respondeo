// Home "journey reel" behaviour.
//
// The reel renders Stage 1 at the LEFT and the final stage at the RIGHT so the
// journey reads as walking a path forward. This module:
//   1. Starts the reader at the left (Stage 1) on load.
//   2. Tracks the centred step and, from a single notify path, updates the prev/next
//      hint visibility, per-step "centred" classes, and the progress-dot rail.
//   3. Provides a fallback "turning"/emphasis effect via IntersectionObserver for
//      browsers without scroll-driven CSS animations (animation-timeline).
//   4. Adds guided interactions: click the hint buttons, use the keyboard (arrows /
//      page keys / home / end), flick the mouse wheel one card at a time, click a
//      peeking neighbour to centre it, or click a progress dot to jump.

const state = new WeakMap();

function supportsScrollTimeline() {
    return typeof CSS !== 'undefined'
        && typeof CSS.supports === 'function'
        && CSS.supports('animation-timeline', 'view()');
}

function prefersReducedMotion() {
    return window.matchMedia('(prefers-reduced-motion: reduce)').matches;
}

function stepsOf(reel) {
    return Array.from(reel.querySelectorAll('.reel__step'));
}

// Index of the step whose centre is nearest the reel's current viewport centre.
function nearestStepIndex(reel, steps) {
    const centre = reel.scrollLeft + (reel.clientWidth / 2);
    let nearest = 0;
    let bestDist = Infinity;
    steps.forEach((step, i) => {
        const dist = Math.abs((step.offsetLeft + step.offsetWidth / 2) - centre);
        if (dist < bestDist) {
            bestDist = dist;
            nearest = i;
        }
    });
    return nearest;
}

// Scroll position (within the reel's scroll space) that centres a given step.
function centreOf(reel, step) {
    return step.offsetLeft + (step.offsetWidth / 2) - (reel.clientWidth / 2);
}

// Smoothly (or instantly under reduced motion) centre the step at the given index.
// When moveFocus is true (keyboard navigation), move DOM focus onto the newly
// centred card so its highlight follows the active card instead of lingering on
// the previously focused one.
function goToIndex(reel, index, moveFocus = false) {
    const steps = stepsOf(reel);
    if (steps.length === 0) {
        return;
    }
    const clamped = Math.min(steps.length - 1, Math.max(0, index));
    reel.scrollTo({ left: centreOf(reel, steps[clamped]), behavior: prefersReducedMotion() ? 'auto' : 'smooth' });
    if (moveFocus) {
        const link = steps[clamped].querySelector('a');
        // preventScroll: we already control the scroll position above.
        (link || steps[clamped]).focus({ preventScroll: true });
    }
}

// The single source of truth for "which card is centred": updates hints, the
// per-step centred class (used by the JS fallback + emblem glow), and the dot rail.
function notify(reel) {
    const entry = state.get(reel);
    const steps = stepsOf(reel);
    if (steps.length === 0) {
        return;
    }
    const nearest = nearestStepIndex(reel, steps);
    if (entry) {
        entry.centered = nearest;
    }

    // Mark the centred step so CSS can emphasise it (fallback turn + emblem glow).
    steps.forEach((step, i) => step.classList.toggle('is-centered', i === nearest));

    // Hint visibility. The reel renders left-to-right as Stage 1 … final stage, so index 0
    // is the left-most (first) card and the last index is the right-most (final) card.
    const wrap = reel.closest('.reel-wrap');
    if (wrap) {
        const prev = wrap.querySelector('.reel__hint--prev');
        const next = wrap.querySelector('.reel__hint--next');
        if (prev) {
            prev.classList.toggle('is-hidden', nearest === 0);
        }
        if (next) {
            next.classList.toggle('is-hidden', nearest === steps.length - 1);
        }

        // Progress dots: one per step, in DOM order (left → right). Mark the centred one.
        const dots = wrap.querySelectorAll('.reel-dots__dot');
        dots.forEach((dot, i) => {
            dot.classList.toggle('is-active', i === nearest);
            dot.setAttribute('aria-current', i === nearest ? 'true' : 'false');
        });
    }
}

// Build the progress-dot rail: one dot per step, clickable to jump to that stage.
function buildDots(reel) {
    const wrap = reel.closest('.reel-wrap');
    if (!wrap) {
        return () => { };
    }
    const rail = wrap.querySelector('.reel-dots');
    if (!rail) {
        return () => { };
    }
    const steps = stepsOf(reel);
    const listeners = [];
    rail.replaceChildren();
    steps.forEach((step, i) => {
        const dot = document.createElement('button');
        dot.type = 'button';
        dot.className = 'reel-dots__dot';
        // Dot order matches DOM order (left → right). Label with the human stage number.
        const stageNumber = i + 1;
        dot.setAttribute('aria-label', `Go to stage ${stageNumber}`);
        const onClick = () => goToIndex(reel, i);
        dot.addEventListener('click', onClick);
        listeners.push({ dot, onClick });
        rail.appendChild(dot);
    });
    return () => {
        for (const { dot, onClick } of listeners) {
            dot.removeEventListener('click', onClick);
        }
        rail.replaceChildren();
    };
}

export function init(reel) {
    if (!reel) {
        return;
    }

    state.set(reel, { centered: 0 });
    const entry = state.get(reel);

    const disposeDots = buildDots(reel);

    // Land on Stage 1 (the left-most card) without any visible scroll animation.
    // Do it after paint so the reel has its final scrollWidth.
    requestAnimationFrame(() => {
        reel.scrollLeft = 0;
        notify(reel);
        // Reveal the cards with a gentle entrance once positioned (CSS gates on reduced motion).
        reel.classList.add('is-ready');
    });

    // --- Scroll: keep the centred state in sync (throttled to animation frames). ---
    let scrollScheduled = false;
    const onScroll = () => {
        if (scrollScheduled) {
            return;
        }
        scrollScheduled = true;
        requestAnimationFrame(() => {
            scrollScheduled = false;
            notify(reel);
        });
    };
    reel.addEventListener('scroll', onScroll, { passive: true });

    // --- Keyboard: arrows / page keys step one card; Home/End jump to the ends. ---
    const onKeyDown = (e) => {
        const steps = stepsOf(reel);
        if (steps.length === 0) {
            return;
        }
        const current = entry.centered;
        let target = null;
        switch (e.key) {
            case 'ArrowLeft':
            case 'PageUp':
                target = current - 1;
                break;
            case 'ArrowRight':
            case 'PageDown':
                target = current + 1;
                break;
            case 'Home':
                target = 0;
                break;
            case 'End':
                target = steps.length - 1;
                break;
            default:
                return;
        }
        e.preventDefault();
        goToIndex(reel, target, true);
    };
    reel.addEventListener('keydown', onKeyDown);

    // --- Wheel: advance one card per horizontal gesture; leave vertical scrolling to the page. ---
    // The reel is horizontal, so a vertical wheel (the common mouse gesture) should scroll the page
    // as usual — we only claim clearly horizontal gestures (e.g. a trackpad side-swipe) to step the
    // path. Vertical-dominant events fall through untouched, so no manual page passthrough is needed.
    let wheelLock = false;
    const onWheel = (e) => {
        const steps = stepsOf(reel);
        if (steps.length === 0) {
            return;
        }
        // Not a horizontal gesture: let the browser scroll the page normally.
        if (Math.abs(e.deltaX) <= Math.abs(e.deltaY)) {
            return;
        }
        const direction = e.deltaX < 0 ? -1 : 1;
        const target = entry.centered + direction;
        // At an end and still pushing outward: nothing to do — stay put.
        if (target < 0 || target > steps.length - 1) {
            return;
        }
        e.preventDefault();
        if (wheelLock) {
            return;
        }
        wheelLock = true;
        goToIndex(reel, target);
        // Release the lock after the smooth scroll settles so one flick = one card.
        window.setTimeout(() => { wheelLock = false; }, prefersReducedMotion() ? 60 : 380);
    };
    reel.addEventListener('wheel', onWheel, { passive: false });

    // Touch needs no special handling: the reel scrolls only horizontally, so vertical swipes
    // fall through to the page naturally (giving end-of-list page scroll for free) while
    // horizontal swipes drive the path via native scroll-snap.

    // --- Click a far-off-centre peeking neighbour to nudge the reel one stage toward it. ---
    // Navigation is the DEFAULT: a click on the card the user is looking at always follows its
    // link. Only a card that is genuinely near the reel's edge — more than FAR_THRESHOLD of the
    // viewport away from centre — is intercepted. Rather than jumping straight to it, the click
    // mirrors the hint buttons: it advances exactly one stage in that card's direction (back if it
    // sits left of centre, forward if right). The distance is measured synchronously from scroll
    // geometry (not the async `is-centered` class), so the outcome is deterministic and never
    // races the browser auto-scrolling a card into view before the click.
    const FAR_THRESHOLD = 0.4; // Fraction of the reel viewport width.
    const onClick = (e) => {
        const step = e.target.closest('.reel__step');
        if (!step || !reel.contains(step)) {
            return;
        }
        const centre = reel.scrollLeft + (reel.clientWidth / 2);
        const stepCentre = step.offsetLeft + (step.offsetWidth / 2);
        const distance = Math.abs(stepCentre - centre);
        if (distance <= reel.clientWidth * FAR_THRESHOLD) {
            return; // Near centre: follow its link.
        }
        // A far-off-centre peeking neighbour was clicked: nudge one stage toward it, exactly like
        // the hint buttons (left = back toward the start, right = forward along the path).
        e.preventDefault();
        const direction = stepCentre < centre ? -1 : 1;
        scrollByStep(reel, direction);
    };
    // Capture phase so we can intercept before the anchor's default navigation.
    reel.addEventListener('click', onClick, true);

    // --- Turn effect: native scroll-timeline where supported, else IO fallback. ---
    const observers = [];
    if (supportsScrollTimeline()) {
        reel.classList.add('reel--native');
    } else {
        reel.classList.add('reel--js');
        const observer = new IntersectionObserver((entries) => {
            for (const item of entries) {
                item.target.classList.toggle('is-in-view', item.isIntersecting);
            }
        }, {
            root: reel,
            rootMargin: '-45% 0px -45% 0px',
            threshold: 0
        });
        for (const step of stepsOf(reel)) {
            observer.observe(step);
        }
        observers.push(observer);
    }

    entry.onScroll = onScroll;
    entry.onKeyDown = onKeyDown;
    entry.onWheel = onWheel;
    entry.observers = observers;
    entry.disposeDots = disposeDots;
    entry.onClick = onClick;
}

// Scroll by one card. direction: -1 scrolls up (toward God), +1 scrolls down.
export function scrollByStep(reel, direction) {
    if (!reel) {
        return;
    }
    const entry = state.get(reel);
    const current = entry ? entry.centered : nearestStepIndex(reel, stepsOf(reel));
    goToIndex(reel, current + direction);
}

export function dispose(reel) {
    const entry = reel && state.get(reel);
    if (!entry) {
        return;
    }
    reel.removeEventListener('scroll', entry.onScroll);
    reel.removeEventListener('keydown', entry.onKeyDown);
    reel.removeEventListener('wheel', entry.onWheel);
    reel.removeEventListener('click', entry.onClick, true);
    for (const observer of entry.observers || []) {
        observer.disconnect();
    }
    if (entry.disposeDots) {
        entry.disposeDots();
    }
    state.delete(reel);
}
