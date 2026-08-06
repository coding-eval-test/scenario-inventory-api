using InventoryApi.Models;

namespace InventoryApi.Services;

/// <summary>
/// Money math for orders. Amounts are rounded to two decimal places at the line
/// level so a rendered invoice always sums to the order total.
/// </summary>
public static class OrderTotalsCalculator
{
    public static decimal LineTotal(OrderLine line)
    {
        var discountedUnitPrice = decimal.Round(
            line.UnitPrice * (1m - line.DiscountPercent / 100m),
            2,
            MidpointRounding.AwayFromZero);

        return discountedUnitPrice;
    }

    public static decimal OrderTotal(IEnumerable<OrderLine> lines) => lines.Sum(LineTotal);
}
