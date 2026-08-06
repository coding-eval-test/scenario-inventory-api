using System.Net;
using System.Net.Http.Json;
using InventoryApi.Dtos;
using Xunit;

namespace InventoryApi.Tests;

/// <summary>
/// Covers user story S3 in docs/user-stories.md.
/// </summary>
public class S3LowStockReportTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public S3LowStockReportTests(ApiFactory factory) => _factory = factory;

    private static Task<PagedResult<LowStockItemResponse>?> ReportAsync(HttpClient client, string query) =>
        client.GetFromJsonAsync<PagedResult<LowStockItemResponse>>($"/api/reports/low-stock?{query}");

    [Fact]
    [Trait("Category", "S3")]
    public async Task Report_returns_only_rows_at_or_below_the_threshold()
    {
        var client = _factory.CreateClient();

        var result = await ReportAsync(client, "threshold=12&page=1&pageSize=500");

        Assert.NotNull(result);
        Assert.NotEmpty(result!.Items);
        Assert.All(result.Items, item => Assert.True(item.Available <= 12));
    }

    [Fact]
    [Trait("Category", "S3")]
    public async Task Available_equals_on_hand_minus_reserved()
    {
        var client = _factory.CreateClient();

        var result = await ReportAsync(client, "threshold=20&page=1&pageSize=500");

        Assert.NotNull(result);
        Assert.All(result!.Items, item => Assert.Equal(item.OnHand - item.Reserved, item.Available));
    }

    [Fact]
    [Trait("Category", "S3")]
    public async Task Report_can_be_filtered_to_one_warehouse()
    {
        var client = _factory.CreateClient();

        var result = await ReportAsync(client, "threshold=20&warehouseId=3&page=1&pageSize=500");

        Assert.NotNull(result);
        Assert.NotEmpty(result!.Items);
        Assert.All(result.Items, item => Assert.Equal(3, item.WarehouseId));
        Assert.All(result.Items, item => Assert.Equal("WH-C", item.WarehouseCode));
    }

    [Fact]
    [Trait("Category", "S3")]
    public async Task Rows_are_ordered_by_scarcest_first()
    {
        var client = _factory.CreateClient();

        var result = await ReportAsync(client, "threshold=20&page=1&pageSize=500");

        Assert.NotNull(result);
        var available = result!.Items.Select(i => i.Available).ToList();
        Assert.Equal(available.OrderBy(a => a).ToList(), available);
    }

    [Fact]
    [Trait("Category", "S3")]
    public async Task Report_pages_without_gaps_or_duplicates()
    {
        var client = _factory.CreateClient();

        var all = await ReportAsync(client, "threshold=20&page=1&pageSize=500");
        Assert.NotNull(all);

        var firstPage = await ReportAsync(client, "threshold=20&page=1&pageSize=5");
        var secondPage = await ReportAsync(client, "threshold=20&page=2&pageSize=5");

        Assert.NotNull(firstPage);
        Assert.NotNull(secondPage);
        Assert.Equal(all!.TotalCount, firstPage!.TotalCount);
        Assert.Equal(5, firstPage.Items.Count);

        var keys = firstPage.Items.Concat(secondPage!.Items)
            .Select(i => (i.ProductId, i.WarehouseId))
            .ToList();
        Assert.Equal(keys.Count, keys.Distinct().Count());
    }

    [Fact]
    [Trait("Category", "S3")]
    public async Task Report_includes_product_identity()
    {
        var client = _factory.CreateClient();

        var result = await ReportAsync(client, "threshold=20&page=1&pageSize=500");

        Assert.NotNull(result);
        Assert.All(result!.Items, item =>
        {
            Assert.False(string.IsNullOrWhiteSpace(item.Sku));
            Assert.False(string.IsNullOrWhiteSpace(item.Name));
        });
    }

    [Fact]
    [Trait("Category", "S3")]
    public async Task A_zero_threshold_returns_only_fully_committed_stock()
    {
        var client = _factory.CreateClient();

        var result = await ReportAsync(client, "threshold=0&page=1&pageSize=500");

        Assert.NotNull(result);
        Assert.All(result!.Items, item => Assert.True(item.Available <= 0));
    }

    [Theory]
    [InlineData("threshold=-1&page=1&pageSize=10")]
    [InlineData("threshold=5&page=0&pageSize=10")]
    [InlineData("threshold=5&page=1&pageSize=0")]
    [Trait("Category", "S3")]
    public async Task Invalid_parameters_are_rejected(string query)
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"/api/reports/low-stock?{query}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
