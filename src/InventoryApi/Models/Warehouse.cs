namespace InventoryApi.Models;

public class Warehouse
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    public ICollection<StockLevel> StockLevels { get; set; } = new List<StockLevel>();
}
