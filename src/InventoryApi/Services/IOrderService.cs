using InventoryApi.Dtos;

namespace InventoryApi.Services;

public interface IOrderService
{
    Task<OrderResponse> CreateAsync(CreateOrderRequest request, CancellationToken cancellationToken);

    Task<OrderResponse> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<OrderResponse> ShipAsync(int id, ShipOrderRequest request, CancellationToken cancellationToken);
}
