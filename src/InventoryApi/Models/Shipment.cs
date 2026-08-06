namespace InventoryApi.Models;

public class Shipment
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public int WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = null!;
    public string TrackingNumber { get; set; } = string.Empty;
    public DateTime ShippedAtUtc { get; set; }
}
