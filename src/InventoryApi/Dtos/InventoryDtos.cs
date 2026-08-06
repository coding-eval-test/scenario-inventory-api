using System.ComponentModel.DataAnnotations;

namespace InventoryApi.Dtos;

public record StockLevelResponse(
    int ProductId,
    string Sku,
    int WarehouseId,
    string WarehouseCode,
    int OnHand,
    int Reserved,
    int Available);

public class StockLevelQuery
{
    public int? ProductId { get; set; }

    public int? WarehouseId { get; set; }

    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, 500)]
    public int PageSize { get; set; } = 50;
}
