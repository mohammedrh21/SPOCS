using SPOCS.Application.Contracts.Persistence;
using SPOCS.Application.Contracts.Services;
using SPOCS.Application.DTOs.Orders;
using SPOCS.Application.Mappings;
using SPOCS.Domain.Entities;

namespace SPOCS.Application.Services;

public class OrderService : IOrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly ICartRepository _cartRepository;

    public OrderService(IOrderRepository orderRepository, ICartRepository cartRepository)
    {
        _orderRepository = orderRepository;
        _cartRepository = cartRepository;
    }

    public async Task<OrderDto> CreateOrderFromCartAsync(Guid customerId, CreateOrderDto request, CancellationToken cancellationToken = default)
    {
        var cart = await _cartRepository.GetCartByCustomerIdAsync(customerId, cancellationToken)
            ?? throw new InvalidOperationException("Cart not found.");

        if (!cart.Items.Any())
            throw new InvalidOperationException("Cannot create an order from an empty cart.");

        // Build order number: ORD-YYYYMMDD-XXXXX
        var orderNumber = $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..5].ToUpper()}";

        var order = new Order
        {
            OrderNumber = orderNumber,
            CustomerId = customerId,
            OrderDate = DateTime.UtcNow,
            Status = OrderStatus.Pending,
            ShippingAddress = request.ShippingAddress,
            City = request.City,
            PostalCode = request.PostalCode,
            Country = request.Country
        };

        // Snapshot items from cart
        foreach (var cartItem in cart.Items)
        {
            var variantDescription = cartItem.ProductVariant is not null
                ? string.Join(", ", cartItem.ProductVariant.ProductVariantOptions
                    .Where(pvo => pvo.VariantOption != null)
                    .Select(pvo => $"{pvo.VariantOption.VariantType?.Name}: {pvo.VariantOption.Value}"))
                : null;

            order.Items.Add(new OrderItem
            {
                OrderId = order.Id,
                ProductId = cartItem.ProductId,
                ProductVariantId = cartItem.ProductVariantId,
                ProductName = cartItem.Product?.Name ?? string.Empty,
                VariantDescription = string.IsNullOrWhiteSpace(variantDescription) ? null : variantDescription,
                UnitPrice = cartItem.UnitPrice,
                Quantity = cartItem.Quantity,
                TotalPrice = cartItem.UnitPrice * cartItem.Quantity
            });
        }

        order.TotalAmount = order.Items.Sum(i => i.TotalPrice);

        await _orderRepository.CreateOrderAsync(order, cancellationToken);

        // Clear cart after successful order
        if (cart.Items.Any())
        {
            _cartRepository.RemoveItems(cart.Items);
            cart.UpdatedAt = DateTime.UtcNow;
            await _cartRepository.SaveChangesAsync(cancellationToken);
        }

        // Reload with items
        var createdOrder = await _orderRepository.GetOrderByIdAsync(order.Id, customerId, cancellationToken);
        return createdOrder!.ToDto();
    }

    public async Task<OrderDto?> GetOrderByIdAsync(Guid orderId, Guid customerId, CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.GetOrderByIdAsync(orderId, customerId, cancellationToken);
        return order?.ToDto();
    }

    public async Task<List<OrderSummaryDto>> GetMyOrdersAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        var orders = await _orderRepository.GetOrdersByCustomerIdAsync(customerId, cancellationToken);
        return orders.Select(o => o.ToSummaryDto()).ToList();
    }
}
