using SPOCS.Application.DTOs.Cart;

namespace SPOCS.Application.Contracts.Services;

public interface ICartService
{
    Task<CartDto?> GetCartAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task<CartDto> AddToCartAsync(Guid customerId, AddToCartDto request, CancellationToken cancellationToken = default);
    Task<CartDto> UpdateCartItemAsync(Guid customerId, Guid itemId, UpdateCartItemDto request, CancellationToken cancellationToken = default);
    Task<CartDto> RemoveFromCartAsync(Guid customerId, Guid itemId, CancellationToken cancellationToken = default);
    Task ClearCartAsync(Guid customerId, CancellationToken cancellationToken = default);
}
