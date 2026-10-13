#!/usr/bin/env bash
# Prepares the published site for GitHub Pages-style hosting. Shared by the deploy and PR
# workflows so the acceptance tests exercise exactly what ships.
#
# 1. 404.html: SPA fallback so unknown routes still boot the app (served with HTTP 404).
# 2. Per-route entry pages: every canonical route in sitemap.xml gets its own copy of the app
#    shell (e.g. discover/saints.html), which GitHub Pages serves at /discover/saints with HTTP 200
#    so search engines will index it. <base href="/"> is absolute, so the copies load assets at any
#    depth. Each copy gets its own canonical, og:url, title, and description (plus the Open Graph
#    and Twitter equivalents) so crawlers that don't run JavaScript don't treat every route as a
#    duplicate of the home page. The sitemap generator writes these from the same content sources
#    it uses for sitemap.xml, so the two can't drift; it HTML-encodes values, which sed can't do safely.
#
# Usage: prepare-pages-site.sh <site-dir>
set -euo pipefail

site="$(cd "$1" && pwd)"
repo="$(cd "$(dirname "$0")/../.." && pwd)"

cp "$site/index.html" "$site/404.html"

dotnet run --project "$repo/tools/Respondeo.SitemapGenerator/Respondeo.SitemapGenerator.csproj" -c Release -- "$repo" --entry-pages "$site"
