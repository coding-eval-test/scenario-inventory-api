using System.Net.Http.Json;
using InventoryApi.Dtos;
using Xunit;

namespace InventoryApi.Tests;

/// <summary>
/// Covers the defect reported in docs/bug-reports.md as B2.
/// </summary>
public class B2ProductSearchTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public B2ProductSearchTests(ApiFactory factory) => _factory = factory;

    [Fact]
    [Trait("Category", "B2")]
    public async Task First_page_returns_the_first_products_in_the_catalog()
    {
        var client = _factory.CreateClient();

        var result = await client.GetFromJsonAsync<PagedResult<ProductResponse>>(
            "/api/products?page=1&pageSize=5");

        Assert.NotNull(result);
        Assert.Equal(new[] { 1, 2, 3, 4, 5 }, result!.Items.Select(p => p.Id).ToArray());
    }

    [Fact]
    [Trait("Category", "B2")]
    public async Task Paging_walks_the_whole_catalog_without_gaps()
    {
        var client = _factory.CreateClient();
        var seen = new List<int>();

        for (var page = 1; page <= 5; page++)
        {
            var result = await client.GetFromJsonAsync<PagedResult<ProductResponse>>(
                $"/api/products?page={page}&pageSize=10");
            Assert.NotNull(result);
            seen.AddRange(result!.Items.Select(p => p.Id));
        }

        Assert.Equal(Enumerable.Range(1, 41), seen);
    }

    [Fact]
    [Trait("Category", "B2")]
    public async Task Last_page_returns_the_remaining_products()
    {
        var client = _factory.CreateClient();

        var result = await client.GetFromJsonAsync<PagedResult<ProductResponse>>(
            "/api/products?page=5&pageSize=10");

        Assert.NotNull(result);
        Assert.Single(result!.Items);
        Assert.Equal(41, result.Items[0].Id);
    }

    [Fact]
    [Trait("Category", "B2")]
    public async Task Name_search_is_case_insensitive()
    {
        var client = _factory.CreateClient();

        var lower = await client.GetFromJsonAsync<PagedResult<ProductResponse>>(
            "/api/products?search=widget&page=1&pageSize=50");
        var upper = await client.GetFromJsonAsync<PagedResult<ProductResponse>>(
            "/api/products?search=WIDGET&page=1&pageSize=50");

        Assert.NotNull(lower);
        Assert.NotNull(upper);
        Assert.Equal(5, lower!.TotalCount);
        Assert.Equal(5, upper!.TotalCount);
        Assert.Contains(lower.Items, p => p.Name == "widget 21");
        Assert.Contains(lower.Items, p => p.Name == "Widget 1");
    }

    [Fact]
    [Trait("Category", "B2")]
    public async Task Sku_search_is_case_insensitive()
    {
        var client = _factory.CreateClient();

        var result = await client.GetFromJsonAsync<PagedResult<ProductResponse>>(
            "/api/products?search=sku-0007&page=1&pageSize=10");

        Assert.NotNull(result);
        Assert.Equal(1, result!.TotalCount);
    }
}
