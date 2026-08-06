using System.Net.Http.Json;
using InventoryApi.Dtos;
using Xunit;

namespace InventoryApi.Tests;

public class ReportsRegressionTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public ReportsRegressionTests(ApiFactory factory) => _factory = factory;

    [Fact]
    [Trait("Category", "Regression")]
    public async Task Order_summary_counts_every_seeded_order()
    {
        var client = _factory.CreateClient();

        var summary = await client.GetFromJsonAsync<OrderSummaryResponse>("/api/reports/order-summary");

        Assert.NotNull(summary);
        Assert.Equal(60, summary!.TotalOrders);
        Assert.Equal(60, summary.CountsByStatus.Values.Sum());
    }

    [Fact]
    [Trait("Category", "Regression")]
    public async Task Order_summary_reports_every_status()
    {
        var client = _factory.CreateClient();

        var summary = await client.GetFromJsonAsync<OrderSummaryResponse>("/api/reports/order-summary");

        Assert.NotNull(summary);
        Assert.Equal(15, summary!.CountsByStatus["Shipped"]);
        Assert.Equal(15, summary.CountsByStatus["Reserved"]);
        Assert.Equal(15, summary.CountsByStatus["Pending"]);
        Assert.Equal(15, summary.CountsByStatus["Cancelled"]);
    }

    [Fact]
    [Trait("Category", "Regression")]
    public async Task Order_summary_honours_the_date_window()
    {
        var client = _factory.CreateClient();

        // Seed orders run from SeedBaseUtc-60d to SeedBaseUtc-1d.
        var summary = await client.GetFromJsonAsync<OrderSummaryResponse>(
            "/api/reports/order-summary?fromUtc=2030-01-01T00:00:00Z");

        Assert.NotNull(summary);
        Assert.Equal(0, summary!.TotalOrders);
    }
}
