using System.Net;
using System.Net.Http.Json;
using InventoryApi.Dtos;
using Xunit;

namespace InventoryApi.Tests;

public class ProductsRegressionTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public ProductsRegressionTests(ApiFactory factory) => _factory = factory;

    [Fact]
    [Trait("Category", "Regression")]
    public async Task Search_reports_the_full_catalog_count()
    {
        var client = _factory.CreateClient();

        var result = await client.GetFromJsonAsync<PagedResult<ProductResponse>>(
            "/api/products?page=1&pageSize=10");

        Assert.NotNull(result);
        Assert.Equal(41, result!.TotalCount);
    }

    [Fact]
    [Trait("Category", "Regression")]
    public async Task Search_honours_the_requested_page_size()
    {
        var client = _factory.CreateClient();

        var result = await client.GetFromJsonAsync<PagedResult<ProductResponse>>(
            "/api/products?page=2&pageSize=10");

        Assert.NotNull(result);
        Assert.Equal(10, result!.Items.Count);
        Assert.Equal(2, result.Page);
        Assert.Equal(10, result.PageSize);
    }

    [Fact]
    [Trait("Category", "Regression")]
    public async Task Search_by_sku_returns_the_single_matching_product()
    {
        var client = _factory.CreateClient();

        var result = await client.GetFromJsonAsync<PagedResult<ProductResponse>>(
            "/api/products?search=SKU-0007&page=1&pageSize=10");

        Assert.NotNull(result);
        Assert.Equal(1, result!.TotalCount);
    }

    [Fact]
    [Trait("Category", "Regression")]
    public async Task Get_by_id_returns_the_product()
    {
        var client = _factory.CreateClient();

        var product = await client.GetFromJsonAsync<ProductResponse>("/api/products/8");

        Assert.NotNull(product);
        Assert.Equal(8, product!.Id);
        Assert.Equal("SKU-0008", product.Sku);
        Assert.Equal(30.00m, product.UnitPrice);
    }

    [Fact]
    [Trait("Category", "Regression")]
    public async Task Get_by_id_returns_404_problem_details_for_unknown_product()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/products/99999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(1, 0)]
    [InlineData(1, 201)]
    [Trait("Category", "Regression")]
    public async Task Search_rejects_out_of_range_paging_parameters(int page, int pageSize)
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"/api/products?page={page}&pageSize={pageSize}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
