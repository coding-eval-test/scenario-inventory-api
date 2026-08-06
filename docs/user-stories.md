# User Stories

## S1 — Reserve stock when an order is placed (20 points)

**As a** fulfilment lead
**I want** stock committed the moment an order is accepted
**So that** we never promise the same unit to two customers.

Today `POST /api/orders` records an order without touching inventory.

**Acceptance criteria**

- Placing an order reserves the ordered quantity of each line and leaves the
  order in `Reserved` status.
- Reserving increases `StockLevel.Reserved` and never changes `OnHand`.
- A line is filled from a single warehouse when one warehouse has enough
  available stock. Otherwise it splits across warehouses, largest available
  first.
- If the total available across all warehouses cannot cover any line, the whole
  order is rejected with `409 Conflict`, nothing is reserved, and no order is
  persisted. Placement is all-or-nothing.
- Every reservation writes a `StockAdjustment` with reason `Reservation`,
  a positive `ReservedDelta`, and a zero `OnHandDelta`.

**Out of scope:** backorders. An order that cannot be filled is rejected, not
queued.

**Tests:** `dotnet test --filter Category=S1`

---

## S2 — Cancel an order (15 points)

**As a** support agent
**I want** to cancel an order that has not shipped
**So that** committed stock returns to the available pool.

**Acceptance criteria**

- `POST /api/orders/{id}/cancel` returns the updated order.
- Permitted from `Pending` and `Reserved`. The order moves to `Cancelled` and
  `CancelledAtUtc` is set.
- Any reservation the order holds is released: `Reserved` falls, `OnHand` is
  untouched.
- Cancelling a `Shipped` order returns `409 Conflict`.
- Cancelling an already-`Cancelled` order returns `200 OK` and changes nothing —
  the operation is idempotent, not an error.
- An unknown order returns `404 Not Found`.
- Releases write a `StockAdjustment` with reason `ReservationRelease` and a
  negative `ReservedDelta`.

**Tests:** `dotnet test --filter Category=S2`

---

## S3 — Low-stock report (15 points)

**As a** purchasing manager
**I want** a list of stock positions running low
**So that** I can raise purchase orders before we run out.

**Acceptance criteria**

- `GET /api/reports/low-stock` returns a paged list of product/warehouse
  positions whose available quantity is at or below a `threshold` parameter
  (default 10).
- Available is `OnHand - Reserved`.
- Optional `warehouseId` narrows the report to one warehouse.
- Results are ordered scarcest first, and paging is stable — no gaps, no
  duplicates.
- Each row carries product identity (SKU and name) and warehouse identity (id and
  code) so the report is readable without further lookups.
- Invalid parameters return `400 Bad Request`.

The response contract `LowStockItemResponse` is already agreed with the
fulfilment dashboard team and defined in `src/InventoryApi/Dtos/ReportDtos.cs`.
Do not change its shape.

**Tests:** `dotnet test --filter Category=S3`
