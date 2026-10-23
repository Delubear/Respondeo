// Production service worker: precaches every published asset for full offline use.
//
// Swapped in for service-worker.js at publish time by the ServiceWorker item in Respondeo.csproj.
// It reads service-worker-assets.js (the SDK-generated integrity manifest of every published
// static file - the app framework, CSS/JS, the Summa corpus JSON, and the Markdown content) and
// precaches all of it on install, so once the app is installed it runs fully offline.

self.importScripts('./service-worker-assets.js');

self.addEventListener('install', event => event.waitUntil(onInstall(event)));
self.addEventListener('activate', event => event.waitUntil(onActivate(event)));
// Cross-origin requests (e.g. the Cloudflare analytics beacon) are never precached, so leave them to
// the browser. Routing them through the worker turns a blocked or offline beacon into an uncaught
// "Failed to fetch" error in the worker.
self.addEventListener('fetch', event => {
    if (new URL(event.request.url).origin !== self.origin) {
        return;
    }
    event.respondWith(onFetch(event));
});

// A freshly installed worker parks in the "waiting" state until every tab using the old worker
// is gone - which on an installed PWA can be a long time. When the page detects this waiting
// worker it shows an "update available" prompt; clicking it posts { type: 'skip-waiting' } here so
// the new worker activates immediately, then the page reloads on the resulting controllerchange.
self.addEventListener('message', event => {
    const data = event.data;
    if (!data) {
        return;
    }
    if (data.type === 'skip-waiting') {
        self.skipWaiting();
    } else if (data.type === 'pack-status') {
        event.waitUntil(getPackStatus().then(status => postToClients({ type: 'pack-status', packs: status })));
    } else if (data.type === 'download-pack' && packs[data.pack]) {
        event.waitUntil(downloadPack(data.pack, true));
    } else if (data.type === 'remove-pack' && packs[data.pack]) {
        event.waitUntil(caches.delete(packCacheName(data.pack))
            .then(getPackStatus)
            .then(status => postToClients({ type: 'pack-status', packs: status })));
    }
});

const cacheNamePrefix = 'offline-cache-';
const cacheName = `${cacheNamePrefix}${self.assetsManifest.version}`;

// Precache the app shell and all bundled content. The display fonts are self-hosted (@font-face
// rules in css/app.css) so they are precached here too and render offline. Analytics (Cloudflare
// Web Analytics) is cross-origin and online-only, so it is excluded and simply no-ops offline.
const offlineAssetsInclude = [/\.dll$/, /\.wasm/, /\.html/, /\.js$/, /\.json$/, /\.css$/, /\.woff2?$/, /\.png$/, /\.jpe?g$/, /\.gif$/, /\.ico$/, /\.blat$/, /\.dat$/, /\.svg$/, /\.md$/, /\.txt$/, /\.webmanifest$/];
const offlineAssetsExclude = [/^service-worker\.js$/];

// The app is served from the domain root (custom apex domain), so the base href is '/'.
const base = '/';
const baseUrl = new URL(base, self.origin);
const manifestUrlList = self.assetsManifest.assets.map(asset => new URL(asset.url, baseUrl).href);

// Large opt-in content packs. They live in their own caches (outside the offline-cache- prefix) so a
// new deploy does not wipe them; onActivate refreshes any installed pack whose version is stale.
const packs = {
    summa: '_content/Respondeo.Content.Summa/summa/',
    bible: '_content/Respondeo.Bible/douay-rheims/'
};
const packVersionKey = '__pack-version';

function packCacheName(pack) {
    return `pack-${pack}`;
}

// The small Summa index stays in the core cache so the question list and title search work offline
// even without the pack; only the question bodies are opt-in.
const corePackExceptions = ['_content/Respondeo.Content.Summa/summa/summa-index.json'];

function isPackAsset(url) {
    return !corePackExceptions.includes(url) && Object.values(packs).some(prefix => url.startsWith(prefix));
}

function packAssets(pack) {
    return self.assetsManifest.assets.filter(asset => asset.url.startsWith(packs[pack]) && !corePackExceptions.includes(asset.url));
}

async function postToClients(message) {
    const all = await self.clients.matchAll({ includeUncontrolled: true });
    all.forEach(client => client.postMessage(message));
}

async function getPackVersion(pack) {
    if (!(await caches.has(packCacheName(pack)))) {
        return null;
    }
    const cache = await caches.open(packCacheName(pack));
    const marker = await cache.match(packVersionKey);
    return marker ? await marker.text() : null;
}

async function getPackStatus() {
    const status = {};
    for (const pack of Object.keys(packs)) {
        const assets = packAssets(pack);
        const version = await getPackVersion(pack);
        status[pack] = {
            installed: version !== null,
            stale: version !== null && version !== self.assetsManifest.version,
            assetCount: assets.length
        };
    }
    return status;
}

const packDownloads = {};

function downloadPack(pack, notify) {
    if (!packDownloads[pack]) {
        packDownloads[pack] = runPackDownload(pack, notify).finally(() => delete packDownloads[pack]);
    }
    return packDownloads[pack];
}

async function runPackDownload(pack, notify) {
    const assets = packAssets(pack);
    const cache = await caches.open(packCacheName(pack));
    const batchSize = 20;
    try {
        // Drop the marker first so an interrupted download reads as not installed.
        await cache.delete(packVersionKey);
        for (let i = 0; i < assets.length; i += batchSize) {
            const batch = assets.slice(i, i + batchSize)
                .map(asset => new Request(asset.url, { integrity: asset.hash, cache: 'no-cache' }));
            await cache.addAll(batch);
            if (notify) {
                await postToClients({ type: 'pack-progress', pack, done: Math.min(i + batchSize, assets.length), total: assets.length });
            }
        }
        await cache.put(packVersionKey, new Response(self.assetsManifest.version));
        if (notify) {
            await postToClients({ type: 'pack-complete', pack });
            await postToClients({ type: 'pack-status', packs: await getPackStatus() });
        }
    } catch (error) {
        console.warn(`Service worker: pack ${pack} download failed`, error);
        if (notify) {
            await postToClients({ type: 'pack-error', pack, message: String(error) });
        }
    }
}

async function onInstall() {
    console.info('Service worker: Install');

    // Fetch and cache all matching items from the assets manifest.
    const assetsRequests = self.assetsManifest.assets
        .filter(asset => offlineAssetsInclude.some(pattern => pattern.test(asset.url)))
        .filter(asset => !offlineAssetsExclude.some(pattern => pattern.test(asset.url)))
        .filter(asset => !isPackAsset(asset.url))
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

    // Take control of already-open pages so offline packs work on the very first visit.
    await self.clients.claim();

    // Refresh installed packs
    for (const pack of Object.keys(packs)) {
        const version = await getPackVersion(pack);
        if (version !== null && version !== self.assetsManifest.version) {
            downloadPack(pack, true);
        }
    }
}

async function onFetch(event) {
    let cachedResponse = null;
    if (event.request.method === 'GET') {
        // For navigation requests (e.g. a deep link like /summa/prima-q001, or a refresh), serve
        // index.html so client-side routing can render the page - this makes deep links work
        // offline and also avoids the 404-fallback problem for in-app routes.
        //
        // EXCEPTION: a navigation whose URL is an actual published file (e.g. /sitemap.xml or
        // /robots.txt - pasted into the address bar, which issues a 'navigate' request) must return
        // the file itself, NOT the SPA shell. Without this, the router would render its in-app 404
        // for those paths. App routes are extensionless and absent from the manifest, so they still
        // fall back to index.html as intended.
        const requestedAssetUrl = new URL(event.request.url).href;
        const isPublishedAsset = manifestUrlList.includes(requestedAssetUrl);
        const shouldServeIndexHtml = event.request.mode === 'navigate' && !isPublishedAsset;

        const request = shouldServeIndexHtml ? 'index.html' : event.request;
        const cache = await caches.open(cacheName);
        cachedResponse = await cache.match(request);

        if (!cachedResponse && !shouldServeIndexHtml) {
            const relative = requestedAssetUrl.startsWith(baseUrl.href) ? requestedAssetUrl.substring(baseUrl.href.length) : '';
            const pack = Object.keys(packs).find(p => relative.startsWith(packs[p]));
            if (pack && await caches.has(packCacheName(pack))) {
                cachedResponse = await (await caches.open(packCacheName(pack))).match(event.request);
            }
        }
    }

    // Fall back to the network for anything not precached (on-demand content not yet cached).
    return cachedResponse || fetch(event.request);
}
