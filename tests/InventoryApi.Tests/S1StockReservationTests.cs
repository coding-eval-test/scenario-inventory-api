using System.Net;
using System.Net.Http.Json;
using InventoryApi.Dtos;
using InventoryApi.Models;
using Xunit;

namespace InventoryApi.Tests;

/// <summary>
/// Covers user story S1 in docs/user-stories.md.
/// </summary>
public class S1StockReservationTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public S1StockReservationTests(ApiFactory factory) => _factory = factory;

    private static async Task<IReadOnlyList<StockLevelResponse>> StockAsync(HttpClient client, int productId)
    {
        var result = await client.GetFromJsonAsync<PagedResult<StockLevelResponse>>(
            $"/api/inventory/stock-levels?productId={productId}&page=1&pageSize=10");
        return result!.Items;
    }

    private static Task<HttpResponseMessage> PlaceAsync(HttpClient client, int productId, int quantity) =>
        client.PostAsJsonAsync("/api/orders", new CreateOrderRequest
        {
            CustomerId = 504,
            Lines = [new CreateOrderLineRequest { ProductId = productId, Quantity = quantity }]
        });

    [Fact]
    [Trait("Category", "S1")]
    public async Task Placing_an_order_moves_it_to_reserved()
    {
        var client = _factory.CreateClient();

        var response = await PlaceAsync(client, 20, 2);
        var order = await response.Content.ReadFromJsonAsync<OrderResponse>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(nameof(OrderStatus.Reserved), order!.Status);
    }

    [Fact]
    [Trait("Category", "S1")]
    public async Task Placing_an_order_increases_reserved_stock_by_the_ordered_quantity()
    {
        var client = _factory.CreateClient();
        var before = (await StockAsync(client, 22)).Sum(s => s.Reserved);

        await PlaceAsync(client, 22, 3);

        var after = (await StockAsync(client, 22)).Sum(s => s.Reserved);
        Assert.Equal(before + 3, after);
    }

    [Fact]
    [Trait("Category", "S1")]
    public async Task Reserving_does_not_change_on_hand_stock()
    {
        var client = _factory.CreateClient();
        var before = (await StockAsync(client, 23)).Sum(s => s.OnHand);

        await PlaceAsync(client, 23, 2);

        var after = (await StockAsync(client, 23)).Sum(s => s.OnHand);
        Assert.Equal(before, after);
    }

    [Fact]
    [Trait("Category", "S1")]
    public async Task A_small_order_is_reserved_from_a_single_warehouse()
    {
        var client = _factory.CreateClient();
        var before = await StockAsync(client, 24);

        await PlaceAsync(client, 24, 1);

        var after = await StockAsync(client, 24);
        var changed = after
            .Where(a => a.Reserved != before.Single(b => b.WarehouseId == a.WarehouseId).Reserved)
            .ToList();

        Assert.Single(changed);
    }

    [Fact]
    [Trait("Category", "S1")]
    public async Task A_large_order_splits_across_warehouses()
    {
        var client = _factory.CreateClient();
        var before = await StockAsync(client, 25);
        var largestSingle = before.Max(s => s.Available);
        var quantity = largestSingle + 1;

        var response = await PlaceAsync(client, 25, quantity);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var after = await StockAsync(client, 25);
        var changed = after
            .Where(a => a.Reserved != before.Single(b => b.WarehouseId == a.WarehouseId).Reserved)
            .ToList();

        Assert.True(changed.Count >= 2, "Expected the reservation to span more than one warehouse.");
        Assert.Equal(before.Sum(s => s.Reserved) + quantity, after.Sum(s => s.Reserved));
    }

    [Fact]
    [Trait("Category", "S1")]
    public async Task An_unsatisfiable_order_is_rejected_with_409()
    {
        var client = _factory.CreateClient();

        var response = await PlaceAsync(client, 26, 9_999);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    [Trait("Category", "S1")]
    public async Task A_rejected_order_reserves_nothing_and_is_not_persisted()
    {
        var client = _factory.CreateClient();
        var before = await StockAsync(client, 27);
        var orderCountBefore = (await client.GetFromJsonAsync<OrderSummaryResponse>(
            "/api/reports/order-summary"))!.TotalOrders;

        await PlaceAsync(client, 27, 9_999);

        var after = await StockAsync(client, 27);
        var orderCountAfter = (await client.GetFromJsonAsync<OrderSummaryResponse>(
            "/api/reports/order-summary"))!.TotalOrders;

        Assert.Equal(before.Sum(s => s.Reserved), after.Sum(s => s.Reserved));
        Assert.Equal(orderCountBefore, orderCountAfter);
    }

    [Fact]
    [Trait("Category", "S1")]
    public async Task Placement_is_all_or_nothing_across_lines()
    {
        var client = _factory.CreateClient();
        var before = await StockAsync(client, 28);

        var response = await client.PostAsJsonAsync("/api/orders", new CreateOrderRequest
        {
            CustomerId = 504,
            Lines =
            [
                new CreateOrderLineRequest { ProductId = 28, Quantity = 1 },
                new CreateOrderLineRequest { ProductId = 29, Quantity = 9_999 }
            ]
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var after = await StockAsync(client, 28);
        Assert.Equal(before.Sum(s => s.Reserved), after.Sum(s => s.Reserved));
    }

    [Fact]
    [Trait("Category", "S1")]
    public async Task Reservation_writes_ledger_entries()
    {
        var client = _factory.CreateClient();

        var response = await PlaceAsync(client, 30, 2);
        var order = await response.Content.ReadFromJsonAsync<OrderResponse>();

        await using var db = _factory.CreateDbContext();
        var entries = db.StockAdjustments
            .Where(a => a.OrderId == order!.Id && a.Reason == StockAdjustmentReason.Reservation)
            .ToList();

        Assert.NotEmpty(entries);
        Assert.Equal(2, entries.Sum(e => e.ReservedDelta));
        Assert.All(entries, e => Assert.Equal(0, e.OnHandDelta));
    }
}
