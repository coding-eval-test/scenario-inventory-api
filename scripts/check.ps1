# Runs the same per-category checks the grader runs.
$ErrorActionPreference = 'Continue'

$root = Split-Path -Parent $PSScriptRoot
Push-Location $root
try {
$manifest = Join-Path $root 'scenario.json'

if (-not (Test-Path $manifest)) {
    Write-Host "scenario.json not found at $manifest - cannot tell which work items to check."
    exit 1
}

# scenario.json is the same file the grader reads.
$categories = (Get-Content $manifest -Raw | ConvertFrom-Json).categories.category

if (-not $categories) {
    Write-Host "No work items found in $manifest. Expected 'category' entries - is the file valid JSON?"
    exit 1
}

Write-Host 'Building...'
dotnet build --nologo --verbosity quiet
if ($LASTEXITCODE -ne 0) {
    Write-Host 'BUILD FAILED - fix compilation before running the checks.'
    exit 1
}

$results = [ordered]@{}
$overall = 0

foreach ($category in $categories) {
    Write-Host ''
    Write-Host "=== $category ==="
    $trx = Join-Path $root "TestResults/$category.trx"
    dotnet test --no-build --nologo --verbosity quiet `
        --filter "Category=$category" `
        --logger "trx;LogFileName=$category.trx" `
        --results-directory (Join-Path $root 'TestResults')
    $suitePassed = ($LASTEXITCODE -eq 0)

    # An empty filter exits 0, and a fully skipped suite ([Fact(Skip=...)]) also
    # exits 0, so the exit code alone would report a phantom PASS. A partial skip
    # is the same cheat in smaller doses: skip only the failing tests, keep the
    # shipped-passing ones, and executed > 0 with a green exit — so executed is
    # compared against total too, and any skip zeroes the category.
    $found = 0
    $expected = 0
    if (Test-Path $trx) {
        $match = Select-String -Path $trx -Pattern '<Counters[^>]* executed="(\d+)"' | Select-Object -First 1
        if ($match) { $found = [int]$match.Matches[0].Groups[1].Value }
        $totalMatch = Select-String -Path $trx -Pattern '<Counters[^>]* total="(\d+)"' | Select-Object -First 1
        if ($totalMatch) { $expected = [int]$totalMatch.Matches[0].Groups[1].Value }
    }

    if ($found -eq 0) {
        $results[$category] = 'NO TESTS'
        $overall = 1
    }
    elseif ($found -lt $expected) {
        $results[$category] = 'SKIPPED'
        $overall = 1
    }
    elseif ($suitePassed) {
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
}
finally {
    Pop-Location
}
