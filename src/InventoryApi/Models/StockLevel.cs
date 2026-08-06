namespace InventoryApi.Models;

/// <summary>
/// Quantity of one product held in one warehouse.
/// <see cref="Reserved"/> is stock committed to orders but not yet shipped.
/// Available stock is <c>OnHand - Reserved</c>.
/// </summary>
public class StockLevel
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = null!;
    public int OnHand { get; set; }
    public int Reserved { get; set; }
}
