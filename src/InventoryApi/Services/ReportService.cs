using InventoryApi.Data;
using InventoryApi.Dtos;
using Microsoft.EntityFrameworkCore;

namespace InventoryApi.Services;

public class ReportService : IReportService
{
    private readonly AppDbContext _db;

    public ReportService(AppDbContext db) => _db = db;

    public async Task<OrderSummaryResponse> GetOrderSummaryAsync(
        DateTime? fromUtc, DateTime? toUtc, CancellationToken cancellationToken)
    {
        var orders = _db.Orders.AsNoTracking();

        if (fromUtc is { } from)
        {
            orders = orders.Where(o => o.PlacedAtUtc >= from);
        }

        if (toUtc is { } to)
        {
            orders = orders.Where(o => o.PlacedAtUtc <= to);
        }

        var grouped = await orders
            .GroupBy(o => o.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var counts = grouped.ToDictionary(g => g.Status.ToString(), g => g.Count);

        return new OrderSummaryResponse(counts.Values.Sum(), counts);
    }
}
