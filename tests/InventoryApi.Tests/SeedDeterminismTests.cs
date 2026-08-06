using InventoryApi.Data;
using InventoryApi.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InventoryApi.Tests;

public class SeedDeterminismTests
{
    private static AppDbContext NewDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source=file:seed{Guid.NewGuid():N}?mode=memory&cache=shared")
            .Options;
        var db = new AppDbContext(options);
        db.Database.OpenConnection();
        db.Database.EnsureCreated();
        return db;
    }

    [Fact]
    [Trait("Category", "Regression")]
    public void Seed_produces_expected_row_counts()
    {
        using var db = NewDb();
        DbSeeder.Seed(db);

        Assert.Equal(41, db.Products.Count());
        Assert.Equal(3, db.Warehouses.Count());
        Assert.Equal(25, db.Customers.Count());
        Assert.Equal(60, db.Orders.Count());
        Assert.Equal(41 * 3, db.StockLevels.Count());
    }

    [Fact]
    [Trait("Category", "Regression")]
    public void Seed_is_deterministic_across_runs()
    {
        using var first = NewDb();
        DbSeeder.Seed(first);
        var firstSnapshot = first.StockLevels
            .OrderBy(s => s.ProductId).ThenBy(s => s.WarehouseId)
            .Select(s => $"{s.ProductId}:{s.WarehouseId}:{s.OnHand}:{s.Reserved}")
            .ToList();
        var firstOrder1042 = first.OrderLines
            .Where(l => l.OrderId == 1042)
            .OrderBy(l => l.Id)
            .Select(l => $"{l.ProductId}:{l.Quantity}:{l.UnitPrice}:{l.DiscountPercent}")
            .ToList();

        using var second = NewDb();
        DbSeeder.Seed(second);
        var secondSnapshot = second.StockLevels
            .OrderBy(s => s.ProductId).ThenBy(s => s.WarehouseId)
            .Select(s => $"{s.ProductId}:{s.WarehouseId}:{s.OnHand}:{s.Reserved}")
            .ToList();
        var secondOrder1042 = second.OrderLines
            .Where(l => l.OrderId == 1042)
            .OrderBy(l => l.Id)
            .Select(l => $"{l.ProductId}:{l.Quantity}:{l.UnitPrice}:{l.DiscountPercent}")
            .ToList();

        Assert.Equal(firstSnapshot, secondSnapshot);
        Assert.Equal(firstOrder1042, secondOrder1042);
    }

    [Fact]
    [Trait("Category", "Regression")]
    public void Order_1042_matches_the_documented_repro_case()
    {
        using var db = NewDb();
        DbSeeder.Seed(db);

        var line = Assert.Single(db.OrderLines.Where(l => l.OrderId == 1042));
        Assert.Equal(4, line.Quantity);
        Assert.Equal(100.00m, line.UnitPrice);
        Assert.Equal(22m, line.DiscountPercent);
    }

    [Fact]
    [Trait("Category", "Regression")]
    public void Seed_contains_case_mixed_widget_names()
    {
        using var db = NewDb();
        DbSeeder.Seed(db);

        var names = db.Products.AsEnumerable()
            .Where(p => p.Name.Contains("widget", StringComparison.OrdinalIgnoreCase))
            .Select(p => p.Name)
            .OrderBy(n => n)
            .ToList();

        // Ids 1, 11, 21, 31, 41 land on "Widget"; id 21 is lowercased by the id % 7 rule.
        Assert.Equal(5, names.Count);
        Assert.Contains("widget 21", names);
        Assert.Contains("Widget 1", names);
    }

    [Fact]
    [Trait("Category", "Regression")]
    public void Seed_is_idempotent()
    {
        using var db = NewDb();
        DbSeeder.Seed(db);
        DbSeeder.Seed(db);

        Assert.Equal(41, db.Products.Count());
        Assert.Equal(60, db.Orders.Count());
    }
}
