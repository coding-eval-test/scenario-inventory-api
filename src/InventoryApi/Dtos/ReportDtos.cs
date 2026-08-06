namespace InventoryApi.Dtos;

public record OrderSummaryResponse(int TotalOrders, IReadOnlyDictionary<string, int> CountsByStatus);
