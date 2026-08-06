using InventoryApi.Data;
using InventoryApi.Dtos;
using InventoryApi.Models;
using InventoryApi.Services.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace InventoryApi.Services;

public class OrderService : IOrderService
{
    private readonly AppDbContext _db;
    private readonly TimeProvider _clock;
    private readonly ILogger<OrderService> _logger;

    public OrderService(AppDbContext db, TimeProvider clock, ILogger<OrderService> logger)
    {
        _db = db;
        _clock = clock;
        _logger = logger;
    }

    public async Task<OrderResponse> CreateAsync(CreateOrderRequest request, CancellationToken cancellationToken)
    {
        var customerExists = await _db.Customers
            .AnyAsync(c => c.Id == request.CustomerId, cancellationToken);
        if (!customerExists)
        {
            throw new NotFoundException($"Customer {request.CustomerId} was not found.");
        }

        var productIds = request.Lines.Select(l => l.ProductId).Distinct().ToList();
        var products = await _db.Products
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        var missing = productIds.Where(id => !products.ContainsKey(id)).ToList();
        if (missing.Count > 0)
        {
            throw new NotFoundException($"Product {missing[0]} was not found.");
        }

        var order = new Order
        {
            CustomerId = request.CustomerId,
            Status = OrderStatus.Pending,
            PlacedAtUtc = _clock.GetUtcNow().UtcDateTime,
            Lines = request.Lines.Select(line => new OrderLine
            {
                ProductId = line.ProductId,
                Quantity = line.Quantity,
                UnitPrice = products[line.ProductId].UnitPrice,
                DiscountPercent = line.DiscountPercent
            }).ToList()
        };

        _db.Orders.Add(order);
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Placed order {OrderId} for customer {CustomerId}",
            order.Id, order.CustomerId);

        return await GetByIdAsync(order.Id, cancellationToken);
    }

    public async Task<OrderResponse> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var order = await LoadAsync(id, cancellationToken);
        return Map(order);
    }

    public async Task<OrderResponse> ShipAsync(
        int id, ShipOrderRequest request, CancellationToken cancellationToken)
    {
        var order = await LoadAsync(id, cancellationToken);

        if (order.Status is OrderStatus.Shipped or OrderStatus.Cancelled)
        {
            throw new ConflictException(
                $"Order {id} is {order.Status} and cannot be shipped.");
        }

        var warehouseExists = await _db.Warehouses
            .AnyAsync(w => w.Id == request.WarehouseId, cancellationToken);
        if (!warehouseExists)
        {
            throw new NotFoundException($"Warehouse {request.WarehouseId} was not found.");
        }

        var productIds = order.Lines.Select(l => l.ProductId).ToList();
        var levels = await _db.StockLevels
            .Where(s => s.WarehouseId == request.WarehouseId && productIds.Contains(s.ProductId))
            .ToDictionaryAsync(s => s.ProductId, cancellationToken);

        foreach (var line in order.Lines)
        {
            if (!levels.TryGetValue(line.ProductId, out var level) || level.OnHand < line.Quantity)
            {
                throw new ConflictException(
                    $"Warehouse {request.WarehouseId} holds insufficient stock for product {line.ProductId}.");
            }
        }

        var shippedAtUtc = _clock.GetUtcNow().UtcDateTime;

        foreach (var line in order.Lines)
        {
            var level = levels[line.ProductId];

            // Any reservation this order still holds is consumed by the shipment.
            var releasedFromReservation = Math.Min(level.Reserved, line.Quantity);
            level.Reserved -= releasedFromReservation;
            level.OnHand -= line.Quantity;

            _db.StockAdjustments.Add(new StockAdjustment
            {
                ProductId = line.ProductId,
                WarehouseId = request.WarehouseId,
                OnHandDelta = -line.Quantity,
                ReservedDelta = -releasedFromReservation,
                Reason = StockAdjustmentReason.Shipment,
                OrderId = order.Id,
                OccurredAtUtc = shippedAtUtc
            });
        }

        _db.Shipments.Add(new Shipment
        {
            OrderId = order.Id,
            WarehouseId = request.WarehouseId,
            TrackingNumber = $"TRK-{order.Id:D6}",
            ShippedAtUtc = shippedAtUtc
        });

        order.Status = OrderStatus.Shipped;
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Shipped order {OrderId} from warehouse {WarehouseId}",
            order.Id, request.WarehouseId);

        return await GetByIdAsync(order.Id, cancellationToken);
    }

    private async Task<Order> LoadAsync(int id, CancellationToken cancellationToken)
    {
        var order = await _db.Orders
            .Include(o => o.Lines)
            .ThenInclude(l => l.Product)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

        if (order is null)
        {
            throw new NotFoundException($"Order {id} was not found.");
        }

        return order;
    }

    private static OrderResponse Map(Order order) => new(
        order.Id,
        order.CustomerId,
        order.Status.ToString(),
        order.PlacedAtUtc,
        order.CancelledAtUtc,
        order.Lines
            .OrderBy(l => l.Id)
            .Select(l => new OrderLineResponse(
                l.ProductId,
                l.Product.Sku,
                l.Quantity,
                l.UnitPrice,
                l.DiscountPercent,
                OrderTotalsCalculator.LineTotal(l)))
            .ToList(),
        OrderTotalsCalculator.OrderTotal(order.Lines));
}
