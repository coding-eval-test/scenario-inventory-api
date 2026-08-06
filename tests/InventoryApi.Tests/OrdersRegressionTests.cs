using System.Net;
using System.Net.Http.Json;
using InventoryApi.Dtos;
using Xunit;

namespace InventoryApi.Tests;

public class OrdersRegressionTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public OrdersRegressionTests(ApiFactory factory) => _factory = factory;

    private static CreateOrderRequest ValidRequest() => new()
    {
        CustomerId = 501,
        Lines =
        [
            new CreateOrderLineRequest { ProductId = 8, Quantity = 2, DiscountPercent = 0m }
        ]
    };

    [Fact]
    [Trait("Category", "Regression")]
    public async Task Create_returns_201_with_a_location_header()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/orders", ValidRequest());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
    }

    [Fact]
    [Trait("Category", "Regression")]
    public async Task Create_snapshots_the_current_unit_price_onto_the_line()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/orders", ValidRequest());
        var order = await response.Content.ReadFromJsonAsync<OrderResponse>();

        Assert.NotNull(order);
        var line = Assert.Single(order!.Lines);
        Assert.Equal(8, line.ProductId);
        Assert.Equal("SKU-0008", line.Sku);
        Assert.Equal(30.00m, line.UnitPrice);
        Assert.Equal(2, line.Quantity);
    }

    [Fact]
    [Trait("Category", "Regression")]
    public async Task Created_order_can_be_retrieved()
    {
        var client = _factory.CreateClient();

        var created = await (await client.PostAsJsonAsync("/api/orders", ValidRequest()))
            .Content.ReadFromJsonAsync<OrderResponse>();
        Assert.NotNull(created);

        var fetched = await client.GetFromJsonAsync<OrderResponse>($"/api/orders/{created!.Id}");

        Assert.NotNull(fetched);
        Assert.Equal(created.Id, fetched!.Id);
        Assert.Equal(501, fetched.CustomerId);
        Assert.Single(fetched.Lines);
    }

    [Fact]
    [Trait("Category", "Regression")]
    public async Task Create_returns_404_for_an_unknown_customer()
    {
        var client = _factory.CreateClient();
        var request = ValidRequest();
        request.CustomerId = 99999;

        var response = await client.PostAsJsonAsync("/api/orders", request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    [Trait("Category", "Regression")]
    public async Task Create_returns_404_for_an_unknown_product()
    {
        var client = _factory.CreateClient();
        var request = ValidRequest();
        request.Lines[0].ProductId = 99999;

        var response = await client.PostAsJsonAsync("/api/orders", request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    [Trait("Category", "Regression")]
    public async Task Create_rejects_an_order_with_no_lines()
    {
        var client = _factory.CreateClient();
        var request = ValidRequest();
        request.Lines.Clear();

        var response = await client.PostAsJsonAsync("/api/orders", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [Trait("Category", "Regression")]
    public async Task Create_rejects_non_positive_quantities(int quantity)
    {
        var client = _factory.CreateClient();
        var request = ValidRequest();
        request.Lines[0].Quantity = quantity;

        var response = await client.PostAsJsonAsync("/api/orders", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    [Trait("Category", "Regression")]
    public async Task Get_returns_404_problem_details_for_an_unknown_order()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/orders/424242");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    [Trait("Category", "Regression")]
    public async Task Get_returns_the_seeded_order_shape()
    {
        var client = _factory.CreateClient();

        var order = await client.GetFromJsonAsync<OrderResponse>("/api/orders/1042");

        Assert.NotNull(order);
        Assert.Equal(1042, order!.Id);
        var line = Assert.Single(order.Lines);
        Assert.Equal(4, line.Quantity);
        Assert.Equal(100.00m, line.UnitPrice);
        Assert.Equal(22m, line.DiscountPercent);
    }
}
