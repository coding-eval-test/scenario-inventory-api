using System.ComponentModel.DataAnnotations;

namespace InventoryApi.Dtos;

public class CreateOrderRequest
{
    [Range(1, int.MaxValue)]
    public int CustomerId { get; set; }

    [Required]
    [MinLength(1, ErrorMessage = "An order must contain at least one line.")]
    public List<CreateOrderLineRequest> Lines { get; set; } = [];
}

public class CreateOrderLineRequest
{
    [Range(1, int.MaxValue)]
    public int ProductId { get; set; }

    [Range(1, 10_000)]
    public int Quantity { get; set; }

    [Range(0, 100)]
    public decimal DiscountPercent { get; set; }
}

public class ShipOrderRequest
{
    [Range(1, int.MaxValue)]
    public int WarehouseId { get; set; }
}

public record OrderLineResponse(
    int ProductId,
    string Sku,
    int Quantity,
    decimal UnitPrice,
    decimal DiscountPercent,
    decimal LineTotal);

public record OrderResponse(
    int Id,
    int CustomerId,
    string Status,
    DateTime PlacedAtUtc,
    DateTime? CancelledAtUtc,
    IReadOnlyList<OrderLineResponse> Lines,
    decimal Total);
