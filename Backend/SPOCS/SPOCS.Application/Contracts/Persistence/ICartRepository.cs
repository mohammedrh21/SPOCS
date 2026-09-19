using SPOCS.Domain.Entities;

namespace SPOCS.Application.Contracts.Persistence;

public interface ICartRepository
{
    Task<Cart?> GetCartByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task CreateCartAsync(Cart cart, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Detaches all tracked entities so that a fresh query can be executed
    /// without stale concurrency tokens after a DbUpdateConcurrencyException.
    /// </summary>
    void DetachAll();
}
