using System.Net.Http.Json;
using InventoryApi.Dtos;
using InventoryApi.Models;
using InventoryApi.Services;
using Xunit;

namespace InventoryApi.Tests;

/// <summary>
/// Covers the defect reported in docs/bug-reports.md as B1.
/// </summary>
public class B1OrderTotalsTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public B1OrderTotalsTests(ApiFactory factory) => _factory = factory;

    [Fact]
    [Trait("Category", "B1")]
    public void Line_total_multiplies_the_discounted_price_by_quantity()
    {
        var line = new OrderLine { Quantity = 4, UnitPrice = 100.00m, DiscountPercent = 22m };

        var total = OrderTotalsCalculator.LineTotal(line);

        Assert.Equal(312.00m, total);
    }

    [Fact]
    [Trait("Category", "B1")]
    public void Line_total_without_a_discount_is_price_times_quantity()
    {
        var line = new OrderLine { Quantity = 3, UnitPrice = 19.99m, DiscountPercent = 0m };

        var total = OrderTotalsCalculator.LineTotal(line);

        Assert.Equal(59.97m, total);
    }

    [Fact]
    [Trait("Category", "B1")]
    public void Line_total_of_a_single_unit_is_unchanged()
    {
        var line = new OrderLine { Quantity = 1, UnitPrice = 45.00m, DiscountPercent = 10m };

        var total = OrderTotalsCalculator.LineTotal(line);

        Assert.Equal(40.50m, total);
    }

    [Fact]
    [Trait("Category", "B1")]
    public void Order_total_sums_every_line()
    {
        var lines = new List<OrderLine>
        {
            new() { Quantity = 4, UnitPrice = 100.00m, DiscountPercent = 22m },
            new() { Quantity = 2, UnitPrice = 25.00m, DiscountPercent = 0m }
        };

        var total = OrderTotalsCalculator.OrderTotal(lines);

        Assert.Equal(362.00m, total);
    }

    [Fact]
    [Trait("Category", "B1")]
    public async Task Reported_order_1042_totals_312()
    {
        var client = _factory.CreateClient();

        var order = await client.GetFromJsonAsync<OrderResponse>("/api/orders/1042");

        Assert.NotNull(order);
        Assert.Equal(312.00m, order!.Total);
        Assert.Equal(312.00m, Assert.Single(order.Lines).LineTotal);
    }

    [Fact]
    [Trait("Category", "B1")]
    public async Task Newly_placed_order_totals_price_times_quantity()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/orders", new CreateOrderRequest
        {
            CustomerId = 502,
            Lines =
            [
                new CreateOrderLineRequest { ProductId = 8, Quantity = 5, DiscountPercent = 0m }
            ]
        });
        var order = await response.Content.ReadFromJsonAsync<OrderResponse>();

        Assert.NotNull(order);
        // Product 8 seeds at 30.00.
        Assert.Equal(150.00m, order!.Total);
    }
}
