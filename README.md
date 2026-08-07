# Brownfield Scenario: Order & Inventory API

You have joined a team that maintains a small internal API for order placement,
inventory, and fulfilment. The codebase is healthy and conventional. Your job is
the ordinary work of a team member: deliver three user stories and fix two
reported defects, without breaking anything that already works.

## Stack

.NET 10 · ASP.NET Core (controllers) · EF Core 10 · SQLite · xUnit

## Getting started

**Codespaces** — open this repository in a Codespace. Everything is installed.

**Locally** — install the [.NET 10 SDK](https://dotnet.microsoft.com/download),
then:

```bash
dotnet restore
dotnet run --project src/InventoryApi
```

The database is created, migrated, and seeded on first run. Browse
<http://localhost:5000/swagger> for the API surface.

## Your work

- `docs/user-stories.md` — three stories to implement
- `docs/bug-reports.md` — two defects to fix
- `docs/domain.md` — how the domain works
- `docs/rubric.md` — how your work is reviewed beyond the tests

## Running the tests

```bash
dotnet test                       # everything
./scripts/check.sh                # per work item, matching the grader
```

On Windows: `./scripts/check.ps1`

The suite ships red for every work item and green for existing behaviour. Turning
the red ones green without turning any green one red is the assignment.

## Grading

<!-- grading-table:start -->
| Work item | Points |
| --- | --- |
| S1 reserve stock on placement | 20 |
| S2 cancel an order | 15 |
| S3 low-stock report | 15 |
| B1 order totals | 15 |
| B2 product search | 15 |
| Existing behaviour (regression) | 20 |
<!-- grading-table:end -->

Every push runs the same per-item checks in GitHub Actions and writes a scorecard
to the run summary — open the **Grade** workflow run to see which work items
scored and which did not. A partial score is expected while you are working; the
run is not "broken" because items are still red.

Regression carries real weight — breaking working code costs more than leaving a
story unfinished.
