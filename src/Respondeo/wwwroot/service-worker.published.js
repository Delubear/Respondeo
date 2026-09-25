// Production service worker: precaches every published asset for full offline use.
//
// Swapped in for service-worker.js at publish time by the ServiceWorker item in Respondeo.csproj.
// It reads service-worker-assets.js (the SDK-generated integrity manifest of every published
// static file - the app framework, CSS/JS, the Summa corpus JSON, and the Markdown content) and
// precaches all of it on install, so once the app is installed it runs fully offline.

self.importScripts('./service-worker-assets.js');

self.addEventListener('install', event => event.waitUntil(onInstall(event)));
self.addEventListener('activate', event => event.waitUntil(onActivate(event)));
self.addEventListener('fetch', event => event.respondWith(onFetch(event)));

const cacheNamePrefix = 'offline-cache-';
const cacheName = `${cacheNamePrefix}${self.assetsManifest.version}`;

// Precache the app shell and all bundled content. The display fonts are self-hosted (@font-face
// rules in css/app.css) so they are precached here too and render offline. Analytics (GoatCounter)
// is cross-origin and online-only, so it is excluded and simply no-ops offline.
const offlineAssetsInclude = [/\.dll$/, /\.pdb$/, /\.wasm/, /\.html/, /\.js$/, /\.json$/, /\.css$/, /\.woff$/, /\.png$/, /\.jpe?g$/, /\.gif$/, /\.ico$/, /\.blat$/, /\.dat$/, /\.svg$/, /\.md$/, /\.txt$/, /\.webmanifest$/];
const offlineAssetsExclude = [/^service-worker\.js$/];

// The app is served from the domain root (custom apex domain), so the base href is '/'.
const base = '/';
const baseUrl = new URL(base, self.origin);
const manifestUrlList = self.assetsManifest.assets.map(asset => new URL(asset.url, baseUrl).href);

async function onInstall() {
    console.info('Service worker: Install');

    // Fetch and cache all matching items from the assets manifest.
    const assetsRequests = self.assetsManifest.assets
        .filter(asset => offlineAssetsInclude.some(pattern => pattern.test(asset.url)))
        .filter(asset => !offlineAssetsExclude.some(pattern => pattern.test(asset.url)))
        .map(asset => new Request(asset.url, { integrity: asset.hash, cache: 'no-cache' }));
    await caches.open(cacheName).then(cache => cache.addAll(assetsRequests));
}

async function onActivate() {
    console.info('Service worker: Activate');

    // Delete unused caches from previous versions so a new deploy reclaims old storage.
    const cacheKeys = await caches.keys();
    await Promise.all(cacheKeys
        .filter(key => key.startsWith(cacheNamePrefix) && key !== cacheName)
        .map(key => caches.delete(key)));
}

async function onFetch(event) {
    let cachedResponse = null;
    if (event.request.method === 'GET') {
        // For navigation requests (e.g. a deep link like /summa/prima-q001, or a refresh), serve
        // index.html so client-side routing can render the page - this makes deep links work
        // offline and also avoids the 404-fallback problem for in-app routes.
        const shouldServeIndexHtml = event.request.mode === 'navigate';

        const request = shouldServeIndexHtml ? 'index.html' : event.request;
        const cache = await caches.open(cacheName);
        cachedResponse = await cache.match(request);
    }

    // Fall back to the network for anything not precached (fonts, analytics, on-demand content
    // not yet cached). Offline, cross-origin requests simply fail without breaking the app.
    return cachedResponse || fetch(event.request);
}
