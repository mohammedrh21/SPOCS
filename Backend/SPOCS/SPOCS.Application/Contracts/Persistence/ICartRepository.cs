using SPOCS.Domain.Entities;

namespace SPOCS.Application.Contracts.Persistence;

public interface ICartRepository
{
    Task<Cart?> GetCartByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task CreateCartAsync(Cart cart, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
