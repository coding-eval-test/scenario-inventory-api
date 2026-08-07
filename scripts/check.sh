#!/usr/bin/env bash
# Runs the same per-category checks the grader runs.
set -uo pipefail

root="$(cd "$(dirname "$0")/.." && pwd)"
cd "$root" || exit 1
manifest="$root/scenario.json"

if [ ! -f "$manifest" ]; then
  echo "scenario.json not found at ${manifest} — cannot tell which work items to check."
  exit 1
fi

# scenario.json is the same file the grader reads. Parsed with sed rather than jq
# because jq is not installed everywhere this runs; CI keeps the file in the
# one-key-per-line shape this depends on.
categories=$(sed -n 's/.*"category"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/p' "$manifest")

if [ -z "$categories" ]; then
  echo "No work items found in ${manifest}. Expected \"category\" entries — is the file valid JSON?"
  exit 1
fi

declare -A results
overall=0

echo "Building..."
if ! dotnet build --nologo --verbosity quiet; then
  echo "BUILD FAILED — fix compilation before running the checks."
  exit 1
fi

for category in $categories; do
  echo ""
  echo "=== ${category} ==="
  if dotnet test --no-build --nologo --verbosity quiet \
       --filter "Category=${category}" \
       --logger "trx;LogFileName=${category}.trx" \
       --results-directory "$root/TestResults" < /dev/null; then
    suite_passed=1
  else
    suite_passed=0
  fi

  # An empty filter exits 0, so the exit code alone would report a phantom PASS.
  found=0
  if [ -f "$root/TestResults/${category}.trx" ]; then
    found=$(sed -n 's/.*<Counters[^>]*total="\([0-9]*\)".*/\1/p' \
              "$root/TestResults/${category}.trx" | head -1)
    found=${found:-0}
  fi

  if [ "$found" -eq 0 ]; then
    results[$category]="NO TESTS"
    overall=1
  elif [ "$suite_passed" -eq 1 ]; then
    results[$category]="PASS"
  else
    results[$category]="FAIL"
    overall=1
  fi
done

echo ""
echo "================ SUMMARY ================"
for category in $categories; do
  printf '%-12s %s\n' "$category" "${results[$category]}"
done
echo "========================================"

exit $overall
