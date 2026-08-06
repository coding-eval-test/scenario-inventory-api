# Bug Reports

## B1 — Order totals are too low on discounted multi-unit lines (15 points)

**Reported by:** Finance
**Severity:** High — customers are being undercharged.

**Steps to reproduce**

1. `GET /api/orders/1042`
2. The order has one line: 4 units of SKU-0008 at $100.00 with a 22% discount.

**Expected:** line total $312.00, order total $312.00.
**Actual:** line total $78.00, order total $78.00.

**Notes from the reporter:** "Single-unit orders look right, which is why this
went unnoticed. Anything with a quantity above one and a discount is wrong. The
undercharge scales with quantity."

**Tests:** `dotnet test --filter Category=B1`

---

## B2 — Product search skips results and misses lowercase names (15 points)

**Reported by:** Support
**Severity:** Medium — the catalogue looks incomplete to users.

**Steps to reproduce, part one**

1. `GET /api/products?page=1&pageSize=5`
2. **Expected:** the first five products in the catalogue.
3. **Actual:** products six through ten. The first page is missing entirely, and
   `totalCount` says the products exist.

**Steps to reproduce, part two**

1. `GET /api/products?search=widget`
2. **Expected:** every product whose name contains "widget" in any casing — there
   are five.
3. **Actual:** one result. Searching `WIDGET` returns none.

**Notes from the reporter:** "Searching by SKU works fine. It's names that go
missing, and only some of them."

**Tests:** `dotnet test --filter Category=B2`
