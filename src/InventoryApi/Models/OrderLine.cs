namespace InventoryApi.Models;

public class OrderLine
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int Quantity { get; set; }

    /// <summary>Price snapshot taken when the order was placed.</summary>
    public decimal UnitPrice { get; set; }

    /// <summary>Whole-percent discount, 0-100.</summary>
    public decimal DiscountPercent { get; set; }
}
