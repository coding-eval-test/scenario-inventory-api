namespace InventoryApi.Models;

public class Order
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public DateTime PlacedAtUtc { get; set; }
    public DateTime? CancelledAtUtc { get; set; }

    public ICollection<OrderLine> Lines { get; set; } = new List<OrderLine>();
    public ICollection<Shipment> Shipments { get; set; } = new List<Shipment>();
}
