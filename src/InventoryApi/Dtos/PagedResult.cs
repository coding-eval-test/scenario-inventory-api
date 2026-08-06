namespace InventoryApi.Dtos;

/// <param name="Page">1-based page number.</param>
public record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);
