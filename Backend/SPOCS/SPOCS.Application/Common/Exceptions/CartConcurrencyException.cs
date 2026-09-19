namespace SPOCS.Application.Common.Exceptions;

/// <summary>
/// Thrown by the cart repository when a database concurrency conflict is detected
/// (i.e. the underlying DbUpdateConcurrencyException from EF Core).  Using a
/// custom exception keeps the Application layer free of any EF Core dependency
/// while still allowing CartService to implement its retry logic.
/// </summary>
public class CartConcurrencyException : Exception
{
    public CartConcurrencyException(string message, Exception inner)
        : base(message, inner)
    {
    }
}
