#!/usr/bin/env bash
# Prepares the published site for GitHub Pages-style hosting. Shared by the deploy and PR
# workflows so the acceptance tests exercise exactly what ships.
#
# 1. 404.html: SPA fallback so unknown routes still boot the app (served with HTTP 404).
# 2. Per-route entry pages: every canonical route in sitemap.xml gets its own copy of the app
#    shell (e.g. discover/saints.html), which GitHub Pages serves at /discover/saints with HTTP 200
#    so search engines will index it. <base href="/"> is absolute, so the copies load assets at any
#    depth. Each copy gets its own canonical and og:url so crawlers that don't run JavaScript
#    don't treat every route as a duplicate of the home page.
#
# Usage: prepare-pages-site.sh <site-dir>
set -euo pipefail

site="$1"
origin=https://respondeo.faith

cp "$site/index.html" "$site/404.html"

grep -qF "<link rel=\"canonical\" href=\"$origin/\" />" "$site/index.html" || { echo "::error::index.html canonical tag not found"; exit 1; }
grep -qF "<meta property=\"og:url\" content=\"$origin/\" />" "$site/index.html" || { echo "::error::index.html og:url tag not found"; exit 1; }

count=0
for path in $(grep -oP '(?<=<loc>https://respondeo\.faith/)[^<]+(?=</loc>)' "$site/sitemap.xml"); do
  target="$site/$path.html"
  url="$origin/$path"
  mkdir -p "$(dirname "$target")"
  sed -e "s|<link rel=\"canonical\" href=\"$origin/\" />|<link rel=\"canonical\" href=\"$url\" />|" \
	  -e "s|<meta property=\"og:url\" content=\"$origin/\" />|<meta property=\"og:url\" content=\"$url\" />|" \
	  "$site/index.html" > "$target"
  grep -qF "<link rel=\"canonical\" href=\"$url\" />" "$target" || { echo "::error::canonical not rewritten for $path"; exit 1; }
  grep -qF "<meta property=\"og:url\" content=\"$url\" />" "$target" || { echo "::error::og:url not rewritten for $path"; exit 1; }
  count=$((count + 1))
done

echo "Wrote $count route entry pages"
test "$count" -gt 0
