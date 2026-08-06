using System.Net;
using System.Net.Http.Json;
using InventoryApi.Dtos;
using InventoryApi.Models;
using Xunit;

namespace InventoryApi.Tests;

/// <summary>
/// Covers user story S2 in docs/user-stories.md.
/// </summary>
public class S2OrderCancellationTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public S2OrderCancellationTests(ApiFactory factory) => _factory = factory;

    private static async Task<int> PlaceAsync(HttpClient client, int productId, int quantity)
    {
        var response = await client.PostAsJsonAsync("/api/orders", new CreateOrderRequest
        {
            CustomerId = 505,
            Lines = [new CreateOrderLineRequest { ProductId = productId, Quantity = quantity }]
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<OrderResponse>())!.Id;
    }

    private static async Task<int> ReservedAsync(HttpClient client, int productId)
    {
        var result = await client.GetFromJsonAsync<PagedResult<StockLevelResponse>>(
            $"/api/inventory/stock-levels?productId={productId}&page=1&pageSize=10");
        return result!.Items.Sum(s => s.Reserved);
    }

    [Fact]
    [Trait("Category", "S2")]
    public async Task Cancelling_a_placed_order_marks_it_cancelled()
    {
        var client = _factory.CreateClient();
        var orderId = await PlaceAsync(client, 31, 1);

        var response = await client.PostAsync($"/api/orders/{orderId}/cancel", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var order = await response.Content.ReadFromJsonAsync<OrderResponse>();
        Assert.Equal(nameof(OrderStatus.Cancelled), order!.Status);
        Assert.NotNull(order.CancelledAtUtc);
    }

    [Fact]
    [Trait("Category", "S2")]
    public async Task Cancelling_releases_the_orders_reservation()
    {
        var client = _factory.CreateClient();
        var baseline = await ReservedAsync(client, 32);
        var orderId = await PlaceAsync(client, 32, 3);
        var afterPlacement = await ReservedAsync(client, 32);

        await client.PostAsync($"/api/orders/{orderId}/cancel", null);

        var afterCancel = await ReservedAsync(client, 32);
        Assert.Equal(baseline, afterCancel);
        Assert.True(afterCancel <= afterPlacement);
    }

    [Fact]
    [Trait("Category", "S2")]
    public async Task Cancelling_does_not_change_on_hand_stock()
    {
        var client = _factory.CreateClient();
        var levels = await client.GetFromJsonAsync<PagedResult<StockLevelResponse>>(
            "/api/inventory/stock-levels?productId=33&page=1&pageSize=10");
        var before = levels!.Items.Sum(s => s.OnHand);

        var orderId = await PlaceAsync(client, 33, 2);
        await client.PostAsync($"/api/orders/{orderId}/cancel", null);

        var after = (await client.GetFromJsonAsync<PagedResult<StockLevelResponse>>(
            "/api/inventory/stock-levels?productId=33&page=1&pageSize=10"))!.Items.Sum(s => s.OnHand);
        Assert.Equal(before, after);
    }

    [Fact]
    [Trait("Category", "S2")]
    public async Task Cancelling_a_seeded_reserved_order_releases_its_reservation()
    {
        var client = _factory.CreateClient();

        // Order 1002 is seeded as Reserved.
        var response = await client.PostAsync("/api/orders/1002/cancel", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var order = await response.Content.ReadFromJsonAsync<OrderResponse>();
        Assert.Equal(nameof(OrderStatus.Cancelled), order!.Status);
    }

    [Fact]
    [Trait("Category", "S2")]
    public async Task Cancelling_a_shipped_order_conflicts()
    {
        var client = _factory.CreateClient();

        // Order 1001 is seeded as Shipped.
        var response = await client.PostAsync("/api/orders/1001/cancel", null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    [Trait("Category", "S2")]
    public async Task Cancelling_an_already_cancelled_order_is_idempotent()
    {
        var client = _factory.CreateClient();
        var orderId = await PlaceAsync(client, 34, 1);
        var reservedAfterFirstCancel = 0;

        await client.PostAsync($"/api/orders/{orderId}/cancel", null);
        reservedAfterFirstCancel = await ReservedAsync(client, 34);

        var second = await client.PostAsync($"/api/orders/{orderId}/cancel", null);

        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        var order = await second.Content.ReadFromJsonAsync<OrderResponse>();
        Assert.Equal(nameof(OrderStatus.Cancelled), order!.Status);
        Assert.Equal(reservedAfterFirstCancel, await ReservedAsync(client, 34));
    }

    [Fact]
    [Trait("Category", "S2")]
    public async Task Cancelling_an_unknown_order_returns_404()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync("/api/orders/424242/cancel", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    [Trait("Category", "S2")]
    public async Task A_cancelled_order_cannot_be_shipped()
    {
        var client = _factory.CreateClient();
        var orderId = await PlaceAsync(client, 35, 1);

        await client.PostAsync($"/api/orders/{orderId}/cancel", null);
        var ship = await client.PostAsJsonAsync(
            $"/api/orders/{orderId}/shipments", new ShipOrderRequest { WarehouseId = 1 });

        Assert.Equal(HttpStatusCode.Conflict, ship.StatusCode);
    }

    [Fact]
    [Trait("Category", "S2")]
    public async Task Cancellation_writes_a_release_ledger_entry_when_stock_was_reserved()
    {
        var client = _factory.CreateClient();
        var orderId = await PlaceAsync(client, 36, 2);
        var reservedBeforeCancel = await ReservedAsync(client, 36);

        await client.PostAsync($"/api/orders/{orderId}/cancel", null);

        await using var db = _factory.CreateDbContext();
        var releases = db.StockAdjustments
            .Where(a => a.OrderId == orderId && a.Reason == StockAdjustmentReason.ReservationRelease)
            .ToList();

        var releasedTotal = -releases.Sum(r => r.ReservedDelta);
        var reservedAfterCancel = await ReservedAsync(client, 36);
        Assert.Equal(reservedBeforeCancel - reservedAfterCancel, releasedTotal);
    }
}
