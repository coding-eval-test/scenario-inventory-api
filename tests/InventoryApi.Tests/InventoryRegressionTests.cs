using System.Net;
using System.Net.Http.Json;
using InventoryApi.Dtos;
using Xunit;

namespace InventoryApi.Tests;

public class InventoryRegressionTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public InventoryRegressionTests(ApiFactory factory) => _factory = factory;

    [Fact]
    [Trait("Category", "Regression")]
    public async Task Stock_levels_filtered_by_product_return_one_row_per_warehouse()
    {
        var client = _factory.CreateClient();

        var result = await client.GetFromJsonAsync<PagedResult<StockLevelResponse>>(
            "/api/inventory/stock-levels?productId=8&page=1&pageSize=50");

        Assert.NotNull(result);
        Assert.Equal(3, result!.TotalCount);
        Assert.All(result.Items, item => Assert.Equal(8, item.ProductId));
        Assert.All(result.Items, item => Assert.Equal("SKU-0008", item.Sku));
    }

    [Fact]
    [Trait("Category", "Regression")]
    public async Task Available_is_on_hand_minus_reserved()
    {
        var client = _factory.CreateClient();

        var result = await client.GetFromJsonAsync<PagedResult<StockLevelResponse>>(
            "/api/inventory/stock-levels?page=1&pageSize=200");

        Assert.NotNull(result);
        Assert.All(result!.Items, item => Assert.Equal(item.OnHand - item.Reserved, item.Available));
    }

    [Fact]
    [Trait("Category", "Regression")]
    public async Task Stock_levels_can_be_filtered_by_warehouse()
    {
        var client = _factory.CreateClient();

        var result = await client.GetFromJsonAsync<PagedResult<StockLevelResponse>>(
            "/api/inventory/stock-levels?warehouseId=2&page=1&pageSize=200");

        Assert.NotNull(result);
        Assert.Equal(41, result!.TotalCount);
        Assert.All(result.Items, item => Assert.Equal("WH-B", item.WarehouseCode));
    }

    [Fact]
    [Trait("Category", "Regression")]
    public async Task Seeded_reservations_exist_in_warehouse_one()
    {
        var client = _factory.CreateClient();

        var result = await client.GetFromJsonAsync<PagedResult<StockLevelResponse>>(
            "/api/inventory/stock-levels?warehouseId=1&page=1&pageSize=200");

        Assert.NotNull(result);
        Assert.Contains(result!.Items, item => item.Reserved > 0);
    }

    [Fact]
    [Trait("Category", "Regression")]
    public async Task Stock_levels_reject_invalid_paging()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/inventory/stock-levels?page=0&pageSize=10");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
