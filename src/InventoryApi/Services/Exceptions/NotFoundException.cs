namespace InventoryApi.Services.Exceptions;

/// <summary>Thrown when a requested resource does not exist. Surfaces as HTTP 404.</summary>
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message)
    {
    }
}
