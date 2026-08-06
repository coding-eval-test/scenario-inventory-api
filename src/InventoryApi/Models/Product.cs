namespace InventoryApi.Models;

public class Product
{
    public int Id { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal UnitPrice { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<StockLevel> StockLevels { get; set; } = new List<StockLevel>();
}
