// PWA install prompt handling.
//
// Split out of site.js: this is a self-contained PWA feature. Chromium browsers fire
// 'beforeinstallprompt' when the app is installable; the default mini-infobar is suppressed and the
// event stashed so we can trigger our own "Install" affordance later from a Blazor component. This
// script loads in <head> (before the app boots), so the listener is registered early enough to catch
// the event. iOS Safari does not support this API, so no custom button is shown there (users install
// via the Share sheet).
window.respondeoInstall = (function () {
    var deferredPrompt = null;
    var dotnetRef = null;
    var dismissedKey = 'respondeo.installDismissed';

    function isStandalone() {
        return window.matchMedia('(display-mode: standalone)').matches
            || window.navigator.standalone === true;
    }

    function isDismissed() {
        try { return localStorage.getItem(dismissedKey) === 'true'; } catch (e) { return false; }
    }

    function notify() {
        if (dotnetRef) {
            // Fire-and-forget: tell the component whether an install prompt is currently available.
            dotnetRef.invokeMethodAsync('OnInstallAvailabilityChanged', deferredPrompt !== null);
        }
    }

    window.addEventListener('beforeinstallprompt', function (e) {
        e.preventDefault();
        deferredPrompt = e;
        notify();
    });

    window.addEventListener('appinstalled', function () {
        deferredPrompt = null;
        try { localStorage.removeItem(dismissedKey); } catch (e) { }
        notify();
    });

    return {
        // Registers the Blazor component so it can be told when availability changes, and returns
        // the current availability so the component can render correctly on first load. A prior
        // persistent dismissal (or running as an installed app) keeps the banner hidden.
        register: function (ref) {
            dotnetRef = ref;
            return deferredPrompt !== null && !isStandalone() && !isDismissed();
        },
        unregister: function () {
            dotnetRef = null;
        },
        // Shows the native install prompt. Returns true if the user accepted, false otherwise.
        prompt: async function () {
            if (!deferredPrompt) {
                return false;
            }
            deferredPrompt.prompt();
            var choice = await deferredPrompt.userChoice;
            deferredPrompt = null;
            notify();
            return choice.outcome === 'accepted';
        },
        // Remembers that the user dismissed the banner so it stays hidden on future visits.
        dismiss: function () {
            try { localStorage.setItem(dismissedKey, 'true'); } catch (e) { }
        },
        isStandalone: isStandalone
    };
})();
