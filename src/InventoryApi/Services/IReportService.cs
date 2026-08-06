using InventoryApi.Dtos;

namespace InventoryApi.Services;

public interface IReportService
{
    Task<OrderSummaryResponse> GetOrderSummaryAsync(
        DateTime? fromUtc, DateTime? toUtc, CancellationToken cancellationToken);
}
