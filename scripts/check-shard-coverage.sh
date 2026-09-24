#!/usr/bin/env bash
# Fails if any acceptance-test feature is not assigned to an e2e shard.
#
# Sharding splits the Playwright suite across parallel jobs by matching each
# scenario's Reqnroll `FeatureTitle` trait. The shard filters live in the
# workflow YAML and are maintained by hand, so a newly added .feature file can
# silently go unrun. This guard compares the set of declared feature titles
# against the set covered by the shard filters and fails on any gap.
#
# Usage: check-shard-coverage.sh <workflow-file>
set -euo pipefail

WORKFLOW="${1:?usage: check-shard-coverage.sh <workflow-file>}"
FEATURES_DIR="tests/Respondeo.AcceptanceTests/Features"

# Titles declared by the .feature files (text after "Feature:").
declared=$(grep -rhoP '^\s*Feature:\s*\K.+' "$FEATURES_DIR"/*.feature | sed 's/[[:space:]]*$//' | sort -u)

# Titles referenced by the shard filters (every FeatureTitle=... up to | or ').
covered=$(grep -oP "FeatureTitle=\K[^|']+" "$WORKFLOW" | sed 's/[[:space:]]*$//' | sort -u)

missing=$(comm -23 <(printf '%s\n' "$declared") <(printf '%s\n' "$covered") || true)

if [ -n "$missing" ]; then
  echo "::error::These acceptance features are not assigned to any e2e shard in $WORKFLOW:"
  printf '%s\n' "$missing" | sed 's/^/  - /'
  echo "Add each missing FeatureTitle to a shard filter so its scenarios run in CI."
  exit 1
fi

echo "All acceptance features are covered by the e2e shards in $WORKFLOW."
