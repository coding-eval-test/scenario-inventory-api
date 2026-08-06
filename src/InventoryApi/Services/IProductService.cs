using InventoryApi.Dtos;

namespace InventoryApi.Services;

public interface IProductService
{
    Task<PagedResult<ProductResponse>> SearchAsync(ProductSearchRequest request, CancellationToken cancellationToken);

    Task<ProductResponse> GetByIdAsync(int id, CancellationToken cancellationToken);
}
