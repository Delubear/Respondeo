// Development service worker: intentionally empty (no caching).
//
// During development we want every request to hit the dev server so code and content changes are
// picked up immediately without a stale cache getting in the way. The real, caching service worker
// lives in service-worker.published.js and is swapped in automatically at publish time by the
// ServiceWorker item in Respondeo.csproj.
self.addEventListener('fetch', () => { });
