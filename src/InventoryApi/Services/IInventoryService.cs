using InventoryApi.Dtos;

namespace InventoryApi.Services;

public interface IInventoryService
{
    Task<PagedResult<StockLevelResponse>> GetStockLevelsAsync(
        StockLevelQuery query, CancellationToken cancellationToken);
}
