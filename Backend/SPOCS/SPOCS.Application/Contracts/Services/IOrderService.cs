using SPOCS.Application.DTOs.Orders;

namespace SPOCS.Application.Contracts.Services;

public interface IOrderService
{
    Task<OrderDto> CreateOrderFromCartAsync(Guid customerId, CreateOrderDto request, CancellationToken cancellationToken = default);
    Task<OrderDto?> GetOrderByIdAsync(Guid orderId, Guid customerId, CancellationToken cancellationToken = default);
    Task<List<OrderSummaryDto>> GetMyOrdersAsync(Guid customerId, CancellationToken cancellationToken = default);
}
