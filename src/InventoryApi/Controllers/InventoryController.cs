using InventoryApi.Dtos;
using InventoryApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace InventoryApi.Controllers;

[ApiController]
[Route("api/inventory")]
[Produces("application/json")]
public class InventoryController : ControllerBase
{
    private readonly IInventoryService _inventory;

    public InventoryController(IInventoryService inventory) => _inventory = inventory;

    /// <summary>Lists stock levels, optionally filtered by product and warehouse.</summary>
    [HttpGet("stock-levels")]
    [ProducesResponseType(typeof(PagedResult<StockLevelResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<StockLevelResponse>>> GetStockLevels(
        [FromQuery] StockLevelQuery query, CancellationToken cancellationToken)
        => Ok(await _inventory.GetStockLevelsAsync(query, cancellationToken));
}
