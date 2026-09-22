// Home "journey reel" behaviour.
//
// The reel renders Stage 1 at the BOTTOM and Stage 5 at the TOP so the journey
// visually climbs upward toward God. This module:
//   1. Starts the reader at the bottom (Stage 1) on load.
//   2. Provides a fallback "turning"/emphasis effect via IntersectionObserver
//      for browsers without scroll-driven CSS animations (animation-timeline).
//   3. Lets the up/down hint buttons scroll one card at a time, and hides the
//      hint for whichever end the reader has already reached.
//
// The CSS handles the turn natively where supported; the fallback simply toggles
// an `is-centered` class on whichever step is nearest the middle of the reel.

const state = new WeakMap();

function supportsScrollTimeline() {
    return typeof CSS !== 'undefined'
        && typeof CSS.supports === 'function'
        && CSS.supports('animation-timeline', 'view()');
}

// Index of the step whose centre is nearest the reel's current viewport centre.
function nearestStepIndex(reel, steps) {
    const centre = reel.scrollTop + (reel.clientHeight / 2);
    let nearest = 0;
    let bestDist = Infinity;
    steps.forEach((step, i) => {
        const dist = Math.abs((step.offsetTop + step.offsetHeight / 2) - centre);
        if (dist < bestDist) {
            bestDist = dist;
            nearest = i;
        }
    });
    return nearest;
}

// Toggle the top/bottom hint visibility based on which card is currently centred.
// We use the nearest step (not raw scrollTop) because scroll-snap centring stops
// short of scrollTop 0 / max, so the raw extremes are never actually reached.
function updateHints(reel) {
    const wrap = reel.closest('.reel-wrap');
    if (!wrap) {
        return;
    }
    const up = wrap.querySelector('.reel__hint--up');
    const down = wrap.querySelector('.reel__hint--down');
    const steps = Array.from(reel.querySelectorAll('.reel__step'));
    if (steps.length === 0) {
        return;
    }
    const nearest = nearestStepIndex(reel, steps);

    // The reel renders top-to-bottom as Stage 5 … Stage 1, so index 0 is the top
    // card and the last index is the bottom card.
    // "Keep climbing" (up) advances toward the top; hide it on the top card.
    if (up) {
        up.classList.toggle('is-hidden', nearest === 0);
    }
    // "Earlier steps" (down) advances toward the bottom; hide it on the bottom card.
    if (down) {
        down.classList.toggle('is-hidden', nearest === steps.length - 1);
    }
}

export function init(reel) {
    if (!reel) {
        return;
    }

    // Land on Stage 1 (the bottom-most card) without any visible scroll animation.
    // Do it after paint so the reel has its final scrollHeight.
    requestAnimationFrame(() => {
        reel.scrollTop = reel.scrollHeight;
        updateHints(reel);
    });

    const onScroll = () => updateHints(reel);
    reel.addEventListener('scroll', onScroll, { passive: true });

    const observers = [];

    if (supportsScrollTimeline()) {
        // Native scroll-driven animations cover the turn effect.
        reel.classList.add('reel--native');
    } else {
        // Fallback: mark the most-centred step so CSS can emphasise it.
        reel.classList.add('reel--js');
        const observer = new IntersectionObserver((entries) => {
            for (const entry of entries) {
                entry.target.classList.toggle('is-centered', entry.isIntersecting);
            }
        }, {
            root: reel,
            // A thin band across the middle of the reel: the step overlapping it is "centred".
            rootMargin: '-45% 0px -45% 0px',
            threshold: 0
        });
        for (const step of reel.querySelectorAll('.reel__step')) {
            observer.observe(step);
        }
        observers.push(observer);
    }

    state.set(reel, { onScroll, observers });
}

// Scroll by one card. direction: -1 scrolls up (toward God), +1 scrolls down.
// We snap to the actual neighbouring step rather than nudging by a fixed amount,
// so a click always advances a full card even from an unsnapped position.
export function scrollByStep(reel, direction) {
    if (!reel) {
        return;
    }
    const steps = Array.from(reel.querySelectorAll('.reel__step'));
    if (steps.length === 0) {
        return;
    }

    // Position (within the reel's scroll space) that centres a given step.
    const centreOf = (step) => step.offsetTop + (step.offsetHeight / 2) - (reel.clientHeight / 2);
    const nearest = nearestStepIndex(reel, steps);

    const target = Math.min(steps.length - 1, Math.max(0, nearest + direction));
    if (target === nearest) {
        return;
    }

    const reduce = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    reel.scrollTo({ top: centreOf(steps[target]), behavior: reduce ? 'auto' : 'smooth' });
}

export function dispose(reel) {
    const entry = reel && state.get(reel);
    if (!entry) {
        return;
    }
    reel.removeEventListener('scroll', entry.onScroll);
    for (const observer of entry.observers) {
        observer.disconnect();
    }
    state.delete(reel);
}
