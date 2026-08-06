using System.Net;
using InventoryApi.Models;
using Xunit;

namespace InventoryApi.Tests;

public class HealthTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public HealthTests(ApiFactory factory) => _factory = factory;

    [Fact]
    [Trait("Category", "Regression")]
    public async Task Health_endpoint_returns_ok()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    [Trait("Category", "Regression")]
    public async Task Startup_migrates_and_seeds_the_database()
    {
        // Force the host to start before inspecting the database.
        _ = _factory.CreateClient();

        await using var db = _factory.CreateDbContext();

        Assert.Equal(41, db.Products.Count());
        Assert.Equal(60, db.Orders.Count());
        Assert.Contains(db.Orders, o => o.Status == OrderStatus.Reserved);
    }
}
