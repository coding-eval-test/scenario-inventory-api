#!/usr/bin/env bash
# Runs the same per-category checks the grader runs.
set -uo pipefail

categories=("Regression" "S1" "S2" "S3" "B1" "B2")
declare -A results
overall=0

echo "Building..."
if ! dotnet build --nologo --verbosity quiet; then
  echo "BUILD FAILED — fix compilation before running the checks."
  exit 1
fi

for category in "${categories[@]}"; do
  echo ""
  echo "=== ${category} ==="
  if dotnet test --no-build --nologo --verbosity quiet --filter "Category=${category}"; then
    results[$category]="PASS"
  else
    results[$category]="FAIL"
    overall=1
  fi
done

echo ""
echo "================ SUMMARY ================"
for category in "${categories[@]}"; do
  printf '%-12s %s\n' "$category" "${results[$category]}"
done
echo "========================================"

exit $overall
