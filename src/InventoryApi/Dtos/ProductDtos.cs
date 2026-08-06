using System.ComponentModel.DataAnnotations;

namespace InventoryApi.Dtos;

public record ProductResponse(
    int Id,
    string Sku,
    string Name,
    string? Description,
    decimal UnitPrice,
    bool IsActive);

/// <summary>Query string parameters for the product catalog search.</summary>
public class ProductSearchRequest
{
    /// <summary>Matches product name or SKU. Omit to list everything.</summary>
    public string? Search { get; set; }

    /// <summary>1-based page number.</summary>
    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, 200)]
    public int PageSize { get; set; } = 20;
}
