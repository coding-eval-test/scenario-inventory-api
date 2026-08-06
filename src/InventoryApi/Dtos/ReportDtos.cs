using System.ComponentModel.DataAnnotations;

namespace InventoryApi.Dtos;

public record OrderSummaryResponse(int TotalOrders, IReadOnlyDictionary<string, int> CountsByStatus);

/// <summary>
/// Row of the low-stock report. This shape is agreed with the fulfilment
/// dashboard team — extend it only through a contract change.
/// </summary>
public record LowStockItemResponse(
    int ProductId,
    string Sku,
    string Name,
    int WarehouseId,
    string WarehouseCode,
    int OnHand,
    int Reserved,
    int Available);

public class LowStockQuery
{
    /// <summary>Rows are returned when Available is at or below this value.</summary>
    [Range(0, int.MaxValue)]
    public int Threshold { get; set; } = 10;

    public int? WarehouseId { get; set; }

    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, 500)]
    public int PageSize { get; set; } = 50;
}
