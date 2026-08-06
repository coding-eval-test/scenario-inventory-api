using InventoryApi.Data;
using InventoryApi.Dtos;
using Microsoft.EntityFrameworkCore;

namespace InventoryApi.Services;

public class InventoryService : IInventoryService
{
    private readonly AppDbContext _db;

    public InventoryService(AppDbContext db) => _db = db;

    public async Task<PagedResult<StockLevelResponse>> GetStockLevelsAsync(
        StockLevelQuery query, CancellationToken cancellationToken)
    {
        var levels = _db.StockLevels.AsNoTracking();

        if (query.ProductId is { } productId)
        {
            levels = levels.Where(s => s.ProductId == productId);
        }

        if (query.WarehouseId is { } warehouseId)
        {
            levels = levels.Where(s => s.WarehouseId == warehouseId);
        }

        var totalCount = await levels.CountAsync(cancellationToken);

        var items = await levels
            .OrderBy(s => s.ProductId).ThenBy(s => s.WarehouseId)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(s => new StockLevelResponse(
                s.ProductId,
                s.Product.Sku,
                s.WarehouseId,
                s.Warehouse.Code,
                s.OnHand,
                s.Reserved,
                s.OnHand - s.Reserved))
            .ToListAsync(cancellationToken);

        return new PagedResult<StockLevelResponse>(items, query.Page, query.PageSize, totalCount);
    }
}
