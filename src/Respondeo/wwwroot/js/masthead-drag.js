// Phone masthead drag: the slim masthead follows the finger open/closed via the --mp custom property
// (0 = slim, 1 = full) on the drag zone, then eases to whichever state the release implies and reports it
// to Blazor, which owns the persisted state (the page--masthead-open class).
(function () {
    const phone = window.matchMedia('(max-width: 40rem)');
    // Finger travel (CSS px) that maps to a full open/close.
    const travel = 90;
    // Movement before the gesture commits to an axis, so horizontal pillar scrolling and taps are left alone.
    const axisSlop = 8;
    // Fraction of the travel past which a slow release completes the change instead of springing back.
    const commitFraction = 0.4;
    // Release speed (px/ms) that counts as a flick regardless of distance.
    const flickSpeed = 0.4;

    function attach(zone, dotNet) {
        let start = null;

        const isOpen = () => zone.closest('.page')?.classList.contains('page--masthead-open') ?? false;

        // Past either end the masthead resists, moving a fifth as far as the finger.
        const resist = p => p > 1 ? 1 + (p - 1) * 0.2 : p < 0 ? p * 0.2 : p;

        zone.addEventListener('touchstart', e => {
            if (!phone.matches || e.touches.length !== 1) {
                start = null;
                return;
            }

            const t = e.touches[0];
            start = { x: t.clientX, y: t.clientY, from: isOpen() ? 1 : 0, axis: null, lastY: t.clientY, lastT: e.timeStamp, v: 0 };
        }, { passive: true });

        zone.addEventListener('touchmove', e => {
            if (!start || e.touches.length !== 1) {
                return;
            }

            const t = e.touches[0];
            const dx = t.clientX - start.x;
            const dy = t.clientY - start.y;
            if (!start.axis) {
                if (Math.max(Math.abs(dx), Math.abs(dy)) < axisSlop) {
                    return;
                }

                start.axis = Math.abs(dy) > Math.abs(dx) ? 'y' : 'x';
                if (start.axis === 'y') {
                    zone.classList.add('masthead-zone--dragging');
                }
            }

            if (start.axis !== 'y') {
                return;
            }

            const dt = e.timeStamp - start.lastT;
            if (dt > 0) {
                start.v = (t.clientY - start.lastY) / dt;
            }

            start.lastY = t.clientY;
            start.lastT = e.timeStamp;
            zone.style.setProperty('--mp', resist(start.from + dy / travel).toFixed(3));
        }, { passive: true });

        const end = e => {
            const s = start;
            start = null;
            if (!s || s.axis !== 'y') {
                return;
            }

            const dy = (e.changedTouches[0]?.clientY ?? s.lastY) - s.y;
            // A stale velocity (finger held still before lifting) is not a flick.
            const v = e.timeStamp - s.lastT > 100 ? 0 : s.v;
            const open = Math.abs(v) >= flickSpeed
                ? v > 0
                : Math.abs(dy) / travel >= commitFraction ? dy > 0 : s.from === 1;

            // Re-enable the transition and ease from the finger's position to the target, then hand the final
            // state to Blazor; the inline value is dropped once the class it renders agrees with it.
            zone.classList.remove('masthead-zone--dragging');
            zone.style.setProperty('--mp', open ? '1' : '0');
            dotNet.invokeMethodAsync('SetMastheadExpanded', open)
                .finally(() => zone.style.removeProperty('--mp'));
        };

        zone.addEventListener('touchend', end);
        zone.addEventListener('touchcancel', end);
    }

    window.respondeoMasthead = { attach };
})();
