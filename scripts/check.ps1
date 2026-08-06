# Runs the same per-category checks the grader runs.
$ErrorActionPreference = 'Continue'

Write-Host 'Building...'
dotnet build --nologo --verbosity quiet
if ($LASTEXITCODE -ne 0) {
    Write-Host 'BUILD FAILED - fix compilation before running the checks.'
    exit 1
}

$categories = @('Regression', 'S1', 'S2', 'S3', 'B1', 'B2')
$results = [ordered]@{}
$overall = 0

foreach ($category in $categories) {
    Write-Host ''
    Write-Host "=== $category ==="
    dotnet test --no-build --nologo --verbosity quiet --filter "Category=$category"
    if ($LASTEXITCODE -eq 0) {
        $results[$category] = 'PASS'
    }
    else {
        $results[$category] = 'FAIL'
        $overall = 1
    }
}

Write-Host ''
Write-Host '================ SUMMARY ================'
foreach ($category in $categories) {
    '{0,-12} {1}' -f $category, $results[$category] | Write-Host
}
Write-Host '========================================'

exit $overall
