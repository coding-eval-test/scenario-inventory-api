using System.Net;
using System.Net.Http.Json;
using InventoryApi.Dtos;
using Xunit;

namespace InventoryApi.Tests;

public class ShippingRegressionTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public ShippingRegressionTests(ApiFactory factory) => _factory = factory;

    private static async Task<int> PlaceOrderAsync(HttpClient client, int productId, int quantity)
    {
        var response = await client.PostAsJsonAsync("/api/orders", new CreateOrderRequest
        {
            CustomerId = 503,
            Lines = [new CreateOrderLineRequest { ProductId = productId, Quantity = quantity }]
        });
        response.EnsureSuccessStatusCode();
        var order = await response.Content.ReadFromJsonAsync<OrderResponse>();
        return order!.Id;
    }

    private static async Task<StockLevelResponse> StockAsync(HttpClient client, int productId, int warehouseId)
    {
        var result = await client.GetFromJsonAsync<PagedResult<StockLevelResponse>>(
            $"/api/inventory/stock-levels?productId={productId}&warehouseId={warehouseId}&page=1&pageSize=5");
        return result!.Items.Single();
    }

    [Fact]
    [Trait("Category", "Regression")]
    public async Task Shipping_marks_the_order_shipped()
    {
        var client = _factory.CreateClient();
        var orderId = await PlaceOrderAsync(client, 12, 2);

        var response = await client.PostAsJsonAsync(
            $"/api/orders/{orderId}/shipments", new ShipOrderRequest { WarehouseId = 1 });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var order = await response.Content.ReadFromJsonAsync<OrderResponse>();
        Assert.Equal("Shipped", order!.Status);
    }

    [Fact]
    [Trait("Category", "Regression")]
    public async Task Shipping_reduces_on_hand_stock_by_the_ordered_quantity()
    {
        var client = _factory.CreateClient();
        var before = await StockAsync(client, 13, 1);
        var orderId = await PlaceOrderAsync(client, 13, 2);

        await client.PostAsJsonAsync(
            $"/api/orders/{orderId}/shipments", new ShipOrderRequest { WarehouseId = 1 });

        var after = await StockAsync(client, 13, 1);
        Assert.Equal(before.OnHand - 2, after.OnHand);
    }

    [Fact]
    [Trait("Category", "Regression")]
    public async Task Shipping_twice_conflicts()
    {
        var client = _factory.CreateClient();
        var orderId = await PlaceOrderAsync(client, 14, 1);

        await client.PostAsJsonAsync(
            $"/api/orders/{orderId}/shipments", new ShipOrderRequest { WarehouseId = 1 });
        var second = await client.PostAsJsonAsync(
            $"/api/orders/{orderId}/shipments", new ShipOrderRequest { WarehouseId = 1 });

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        Assert.Equal("application/problem+json", second.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    [Trait("Category", "Regression")]
    public async Task Shipping_a_cancelled_order_conflicts()
    {
        var client = _factory.CreateClient();

        // Order 1004 is seeded as Cancelled.
        var response = await client.PostAsJsonAsync(
            "/api/orders/1004/shipments", new ShipOrderRequest { WarehouseId = 1 });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    [Trait("Category", "Regression")]
    public async Task Shipping_an_unknown_order_returns_404()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/orders/424242/shipments", new ShipOrderRequest { WarehouseId = 1 });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    [Trait("Category", "Regression")]
    public async Task Shipping_from_an_unknown_warehouse_returns_404()
    {
        var client = _factory.CreateClient();
        var orderId = await PlaceOrderAsync(client, 15, 1);

        var response = await client.PostAsJsonAsync(
            $"/api/orders/{orderId}/shipments", new ShipOrderRequest { WarehouseId = 99 });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    [Trait("Category", "Regression")]
    public async Task Shipping_writes_a_ledger_entry()
    {
        var client = _factory.CreateClient();
        var orderId = await PlaceOrderAsync(client, 16, 3);

        await client.PostAsJsonAsync(
            $"/api/orders/{orderId}/shipments", new ShipOrderRequest { WarehouseId = 1 });

        await using var db = _factory.CreateDbContext();
        Assert.Contains(db.StockAdjustments, a => a.OrderId == orderId && a.OnHandDelta == -3);
        Assert.Contains(db.Shipments, s => s.OrderId == orderId && s.WarehouseId == 1);
    }
}
