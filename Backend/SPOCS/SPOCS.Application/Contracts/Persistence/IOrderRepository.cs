using SPOCS.Domain.Entities;

namespace SPOCS.Application.Contracts.Persistence;

public interface IOrderRepository
{
    Task CreateOrderAsync(Order order, CancellationToken cancellationToken = default);
    Task<Order?> GetOrderByIdAsync(Guid orderId, Guid customerId, CancellationToken cancellationToken = default);
    Task<List<Order>> GetOrdersByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
