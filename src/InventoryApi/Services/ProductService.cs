using InventoryApi.Data;
using InventoryApi.Dtos;
using InventoryApi.Models;
using InventoryApi.Services.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace InventoryApi.Services;

public class ProductService : IProductService
{
    private readonly AppDbContext _db;
    private readonly ILogger<ProductService> _logger;

    public ProductService(AppDbContext db, ILogger<ProductService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<PagedResult<ProductResponse>> SearchAsync(
        ProductSearchRequest request, CancellationToken cancellationToken)
    {
        var query = _db.Products.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(p => p.Name.Contains(term) || p.Sku.Contains(term));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(p => p.Id)
            .Skip(request.Page * request.PageSize)
            .Take(request.PageSize)
            .Select(p => new ProductResponse(
                p.Id, p.Sku, p.Name, p.Description, p.UnitPrice, p.IsActive))
            .ToListAsync(cancellationToken);

        _logger.LogDebug("Product search returned {Count} of {Total}", items.Count, totalCount);

        return new PagedResult<ProductResponse>(items, request.Page, request.PageSize, totalCount);
    }

    public async Task<ProductResponse> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var product = await _db.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (product is null)
        {
            throw new NotFoundException($"Product {id} was not found.");
        }

        return Map(product);
    }

    private static ProductResponse Map(Product product) => new(
        product.Id, product.Sku, product.Name, product.Description, product.UnitPrice, product.IsActive);
}
