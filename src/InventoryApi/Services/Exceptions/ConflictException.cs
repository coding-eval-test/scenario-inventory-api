namespace InventoryApi.Services.Exceptions;

/// <summary>Thrown when a request is valid but conflicts with current state. Surfaces as HTTP 409.</summary>
public class ConflictException : Exception
{
    public ConflictException(string message) : base(message)
    {
    }
}
