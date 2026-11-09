// Offline content packs (the Summa and the Bible).
//
// The published service worker keeps these large packs out of the core precache and only downloads them
// when the reader opts in. This module is the page side of that conversation: it relays status, progress
// and errors from the worker to a registered Blazor component, and forwards download/remove requests.
// In development (no controlling worker) every call reports packs as unsupported.
window.respondeoPacks = (function () {
    var listeners = [];
    var promptedKey = 'respondeo.packsPrompted';

    function supported() {
        return !!(navigator.serviceWorker && navigator.serviceWorker.controller);
    }

    function post(message) {
        if (supported()) {
            navigator.serviceWorker.controller.postMessage(message);
        }
    }

    function broadcast(method, arg) {
        listeners.forEach(function (ref) { ref.invokeMethodAsync(method, arg); });
    }

    if (navigator.serviceWorker) {
        navigator.serviceWorker.addEventListener('message', function (e) {
            var data = e.data;
            if (!data || typeof data.type !== 'string') return;
            if (data.type === 'pack-status') broadcast('OnPackStatus', data.packs);
            else if (data.type === 'pack-progress') broadcast('OnPackProgress', { pack: data.pack, done: data.done, total: data.total });
            else if (data.type === 'pack-error') broadcast('OnPackError', { pack: data.pack, message: data.message });
        });
        // On a first visit the worker claims the page after load; packs become manageable then.
        navigator.serviceWorker.addEventListener('controllerchange', function () {
            broadcast('OnSupportedChanged', supported());
            post({ type: 'pack-status' });
        });
    }

    // Ask the browser not to evict saved packs under storage pressure (best effort).
    function requestPersistence() {
        try {
            if (navigator.storage && navigator.storage.persist) {
                navigator.storage.persist().catch(function () { });
            }
        } catch (e) { }
    }

    function isStandalone() {
        return window.matchMedia('(display-mode: standalone)').matches || window.navigator.standalone === true;
    }

    return {
        // Registers a component for callbacks and returns whether packs can be managed at all.
        register: function (ref) {
            listeners.push(ref);
            post({ type: 'pack-status' });
            return supported();
        },
        unregister: function (ref) {
            listeners = listeners.filter(function (r) { return r !== ref; });
        },
        refresh: function () { post({ type: 'pack-status' }); },
        download: function (pack) {
            requestPersistence();
            post({ type: 'download-pack', pack: pack });
        },
        remove: function (pack) { post({ type: 'remove-pack', pack: pack }); },
        isOnline: function () { return navigator.onLine; },
        // The one-time "save for offline?" prompt shows only in the installed app, once.
        shouldPrompt: function () {
            if (!supported() || !isStandalone()) return false;
            try { return localStorage.getItem(promptedKey) !== 'true'; } catch (e) { return false; }
        },
        markPrompted: function () {
            try { localStorage.setItem(promptedKey, 'true'); } catch (e) { }
        }
    };
})();
