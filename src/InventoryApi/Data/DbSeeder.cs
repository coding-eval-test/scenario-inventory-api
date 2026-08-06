using InventoryApi.Models;
using Microsoft.EntityFrameworkCore;

namespace InventoryApi.Data;

/// <summary>
/// Deterministic seed data. Every value derives arithmetically from a row index
/// or from <see cref="SeedBaseUtc"/> so the database is byte-identical on every run.
/// Never introduce DateTime.Now, Guid.NewGuid, or unseeded Random here.
/// </summary>
public static class DbSeeder
{
    public static readonly DateTime SeedBaseUtc = new(2026, 1, 5, 9, 0, 0, DateTimeKind.Utc);

    private const int ProductCount = 41;
    private const int WarehouseCount = 3;
    private const int CustomerCount = 25;
    private const int OrderCount = 60;
    private const int FirstOrderId = 1001;
    private const int FirstCustomerId = 501;

    private static readonly string[] Names =
    [
        "Widget", "Gasket", "Bearing", "Sprocket", "Coupler",
        "Bracket", "Flange", "Valve", "Pump", "Sensor"
    ];

    public static void Seed(AppDbContext db)
    {
        if (db.Products.Any())
        {
            return;
        }

        var products = BuildProducts();
        var warehouses = BuildWarehouses();
        var customers = BuildCustomers();

        db.Products.AddRange(products);
        db.Warehouses.AddRange(warehouses);
        db.Customers.AddRange(customers);
        db.SaveChanges();

        var stock = BuildStockLevels();
        db.StockLevels.AddRange(stock);
        db.SaveChanges();

        var orders = BuildOrders(products);
        db.Orders.AddRange(orders);
        db.SaveChanges();

        ApplyHistoricalStockMovements(db, orders);
        db.SaveChanges();
    }

    private static List<Product> BuildProducts()
    {
        var products = new List<Product>(ProductCount);
        for (var id = 1; id <= ProductCount; id++)
        {
            var name = $"{Names[(id - 1) % Names.Length]} {id}";
            if (id % 7 == 0)
            {
                name = name.ToLowerInvariant();
            }

            products.Add(new Product
            {
                Id = id,
                Sku = $"SKU-{id:D4}",
                Name = name,
                Description = $"Catalog item {id}.",
                UnitPrice = 10.00m + id * 2.50m,
                IsActive = id % 17 != 0
            });
        }

        return products;
    }

    private static List<Warehouse> BuildWarehouses() =>
    [
        new() { Id = 1, Code = "WH-A", Name = "Atlanta" },
        new() { Id = 2, Code = "WH-B", Name = "Boise" },
        new() { Id = 3, Code = "WH-C", Name = "Columbus" }
    ];

    private static List<Customer> BuildCustomers()
    {
        var customers = new List<Customer>(CustomerCount);
        for (var n = 0; n < CustomerCount; n++)
        {
            var id = FirstCustomerId + n;
            customers.Add(new Customer
            {
                Id = id,
                Name = $"Customer {id}",
                Email = $"customer{id}@example.test"
            });
        }

        return customers;
    }

    private static List<StockLevel> BuildStockLevels()
    {
        var levels = new List<StockLevel>(ProductCount * WarehouseCount);
        for (var productId = 1; productId <= ProductCount; productId++)
        {
            for (var warehouseId = 1; warehouseId <= WarehouseCount; warehouseId++)
            {
                levels.Add(new StockLevel
                {
                    ProductId = productId,
                    WarehouseId = warehouseId,
                    OnHand = 5 + (productId * 7 + warehouseId * 13) % 46,
                    Reserved = 0
                });
            }
        }

        return levels;
    }

    private static List<Order> BuildOrders(List<Product> products)
    {
        var orders = new List<Order>(OrderCount);
        for (var index = 0; index < OrderCount; index++)
        {
            var orderId = FirstOrderId + index;
            var order = new Order
            {
                Id = orderId,
                CustomerId = FirstCustomerId + index % CustomerCount,
                Status = (OrderStatus)(index % 4 switch
                {
                    0 => (int)OrderStatus.Shipped,
                    1 => (int)OrderStatus.Reserved,
                    2 => (int)OrderStatus.Pending,
                    _ => (int)OrderStatus.Cancelled
                }),
                PlacedAtUtc = SeedBaseUtc.AddDays(-(OrderCount - index)),
                CancelledAtUtc = index % 4 == 3
                    ? SeedBaseUtc.AddDays(-(OrderCount - index) + 1)
                    : null,
                Lines = BuildLines(orderId, index, products)
            };

            orders.Add(order);
        }

        return orders;
    }

    private static List<OrderLine> BuildLines(int orderId, int index, List<Product> products)
    {
        // Order 1042 is the documented repro case for the order-total defect.
        if (orderId == 1042)
        {
            return
            [
                new OrderLine { OrderId = orderId, ProductId = 8, Quantity = 4, UnitPrice = 100.00m, DiscountPercent = 22m }
            ];
        }

        var lineCount = 1 + index % 3;
        var lines = new List<OrderLine>(lineCount);
        for (var n = 0; n < lineCount; n++)
        {
            var productId = 1 + (index * 3 + n * 5) % ProductCount;
            lines.Add(new OrderLine
            {
                OrderId = orderId,
                ProductId = productId,
                Quantity = 1 + (index + n) % 3,
                UnitPrice = products[productId - 1].UnitPrice,
                DiscountPercent = (index + n) % 5 == 0 ? 10m : 0m
            });
        }

        return lines;
    }

    /// <summary>
    /// Applies the stock consequences of historical orders: shipped orders consumed
    /// on-hand stock, reserved orders still hold reservations in warehouse 1.
    /// </summary>
    private static void ApplyHistoricalStockMovements(AppDbContext db, List<Order> orders)
    {
        var levels = db.StockLevels.ToDictionary(s => (s.ProductId, s.WarehouseId));
        var adjustments = new List<StockAdjustment>();

        foreach (var order in orders.OrderBy(o => o.Id))
        {
            foreach (var line in order.Lines.OrderBy(l => l.ProductId))
            {
                if (!levels.TryGetValue((line.ProductId, 1), out var level))
                {
                    continue;
                }

                switch (order.Status)
                {
                    case OrderStatus.Shipped:
                        var shipped = Math.Min(level.OnHand, line.Quantity);
                        level.OnHand -= shipped;
                        adjustments.Add(new StockAdjustment
                        {
                            ProductId = line.ProductId,
                            WarehouseId = 1,
                            OnHandDelta = -shipped,
                            ReservedDelta = 0,
                            Reason = StockAdjustmentReason.Shipment,
                            OrderId = order.Id,
                            OccurredAtUtc = order.PlacedAtUtc.AddHours(6)
                        });
                        break;

                    case OrderStatus.Reserved:
                        var reservable = Math.Min(level.OnHand - level.Reserved, line.Quantity);
                        if (reservable <= 0)
                        {
                            break;
                        }

                        level.Reserved += reservable;
                        adjustments.Add(new StockAdjustment
                        {
                            ProductId = line.ProductId,
                            WarehouseId = 1,
                            OnHandDelta = 0,
                            ReservedDelta = reservable,
                            Reason = StockAdjustmentReason.Reservation,
                            OrderId = order.Id,
                            OccurredAtUtc = order.PlacedAtUtc.AddHours(1)
                        });
                        break;
                }
            }
        }

        db.StockAdjustments.AddRange(adjustments);

        var shipments = orders
            .Where(o => o.Status == OrderStatus.Shipped)
            .Select(o => new Shipment
            {
                OrderId = o.Id,
                WarehouseId = 1,
                TrackingNumber = $"TRK-{o.Id:D6}",
                ShippedAtUtc = o.PlacedAtUtc.AddHours(6)
            })
            .ToList();

        db.Shipments.AddRange(shipments);
    }
}
