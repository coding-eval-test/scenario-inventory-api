# Review Rubric

Automated tests score correctness. This rubric covers what tests cannot see:
whether your work fits the codebase it lives in. A reviewer applies it to your
submitted changes.

## R1 — Business logic lives in the service layer

**Pass:** new decision-making, validation of domain rules, and database work sit
in a class under `src/InventoryApi/Services`. Controllers only bind input, call a
service, and return a result.
**Fail:** controllers query `AppDbContext` directly or make domain decisions.

## R2 — DTO boundaries are respected

**Pass:** every endpoint accepts and returns types from
`src/InventoryApi/Dtos`. Entity classes never appear in a controller signature or
a serialized response.
**Fail:** an entity is returned to the client, or a DTO leaks EF navigation
properties.

## R3 — Input validation is present on new endpoints

**Pass:** new request models carry DataAnnotations constraints, and invalid input
produces `400 Bad Request` without reaching the database.
**Fail:** unvalidated input flows into a service, or validation is hand-rolled
inside a controller action when an attribute would do.

## R4 — The submission adds its own tests

**Pass:** at least one test written by you beyond the tests that shipped,
covering a case the shipped tests do not — an edge case, a boundary, or an error
path.
**Fail:** only the shipped tests exist.

## R5 — The diff is clean

**Pass:** no commented-out code, no leftover debug logging or `Console.WriteLine`,
no unrelated reformatting, no unused usings introduced.
**Fail:** the diff contains noise a reviewer would ask you to remove.
