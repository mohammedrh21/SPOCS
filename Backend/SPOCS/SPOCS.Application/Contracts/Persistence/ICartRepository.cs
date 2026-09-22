using SPOCS.Domain.Entities;

namespace SPOCS.Application.Contracts.Persistence;

public interface ICartRepository
{
    Task<Cart?> GetCartByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task<Cart?> GetCartByCustomerIdNoTrackingAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task CreateCartAsync(Cart cart, CancellationToken cancellationToken = default);
    void AddItem(CartItem item);
    void RemoveItem(CartItem item);
    void RemoveItems(IEnumerable<CartItem> items);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
