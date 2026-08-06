namespace InventoryApi.Models;

/// <summary>Append-only ledger of every change to a <see cref="StockLevel"/>.</summary>
public class StockAdjustment
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int WarehouseId { get; set; }
    public int OnHandDelta { get; set; }
    public int ReservedDelta { get; set; }
    public StockAdjustmentReason Reason { get; set; }
    public int? OrderId { get; set; }
    public DateTime OccurredAtUtc { get; set; }
}
