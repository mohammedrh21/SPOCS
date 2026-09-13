using SPOCS.Application.Contracts.Persistence;
using SPOCS.Application.Contracts.Services;
using SPOCS.Application.DTOs.Cart;
using SPOCS.Application.Mappings;
using SPOCS.Domain.Entities;

namespace SPOCS.Application.Services;

public class CartService : ICartService
{
    private readonly ICartRepository _cartRepository;
    private readonly IProductRepository _productRepository;

    public CartService(ICartRepository cartRepository, IProductRepository productRepository)
    {
        _cartRepository = cartRepository;
        _productRepository = productRepository;
    }

    public async Task<CartDto?> GetCartAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        var cart = await _cartRepository.GetCartByCustomerIdAsync(customerId, cancellationToken);
        return cart?.ToDto();
    }

    public async Task<CartDto> AddToCartAsync(Guid customerId, AddToCartDto request, CancellationToken cancellationToken = default)
    {
        // Validate product exists
        var product = await _productRepository.GetByIdAsync(request.ProductId, cancellationToken)
            ?? throw new InvalidOperationException($"Product '{request.ProductId}' not found.");

        // Determine price (variant price if variant selected, otherwise base price)
        decimal unitPrice = product.BasePrice;
        if (request.ProductVariantId.HasValue)
        {
            var variant = product.Variants.FirstOrDefault(v => v.Id == request.ProductVariantId.Value)
                ?? throw new InvalidOperationException($"Product variant '{request.ProductVariantId}' not found.");
            unitPrice = variant.Price;
        }

        // Get or create cart
        var cart = await _cartRepository.GetCartByCustomerIdAsync(customerId, cancellationToken);
        if (cart == null)
        {
            cart = new Cart { CustomerId = customerId };
            await _cartRepository.CreateCartAsync(cart, cancellationToken);
        }

        // Check if same product+variant already in cart → increment quantity
        var existingItem = cart.Items.FirstOrDefault(i =>
            i.ProductId == request.ProductId &&
            i.ProductVariantId == request.ProductVariantId);

        if (existingItem != null)
        {
            existingItem.Quantity += request.Quantity;
        }
        else
        {
            cart.Items.Add(new CartItem
            {
                CartId = cart.Id,
                ProductId = request.ProductId,
                ProductVariantId = request.ProductVariantId,
                Quantity = request.Quantity,
                UnitPrice = unitPrice
            });
        }

        cart.UpdatedAt = DateTime.UtcNow;
        await _cartRepository.SaveChangesAsync(cancellationToken);

        // Reload to get full navigation properties
        var updatedCart = await _cartRepository.GetCartByCustomerIdAsync(customerId, cancellationToken);
        return updatedCart!.ToDto();
    }

    public async Task<CartDto> UpdateCartItemAsync(Guid customerId, Guid itemId, UpdateCartItemDto request, CancellationToken cancellationToken = default)
    {
        var cart = await _cartRepository.GetCartByCustomerIdAsync(customerId, cancellationToken)
            ?? throw new InvalidOperationException("Cart not found.");

        var item = cart.Items.FirstOrDefault(i => i.Id == itemId)
            ?? throw new InvalidOperationException($"Cart item '{itemId}' not found.");

        if (request.Quantity <= 0)
        {
            // Remove item when quantity set to 0
            cart.Items.Remove(item);
        }
        else
        {
            item.Quantity = request.Quantity;
        }

        cart.UpdatedAt = DateTime.UtcNow;
        await _cartRepository.SaveChangesAsync(cancellationToken);

        var updatedCart = await _cartRepository.GetCartByCustomerIdAsync(customerId, cancellationToken);
        return updatedCart!.ToDto();
    }

    public async Task<CartDto> RemoveFromCartAsync(Guid customerId, Guid itemId, CancellationToken cancellationToken = default)
    {
        var cart = await _cartRepository.GetCartByCustomerIdAsync(customerId, cancellationToken)
            ?? throw new InvalidOperationException("Cart not found.");

        var item = cart.Items.FirstOrDefault(i => i.Id == itemId)
            ?? throw new InvalidOperationException($"Cart item '{itemId}' not found.");

        cart.Items.Remove(item);
        cart.UpdatedAt = DateTime.UtcNow;
        await _cartRepository.SaveChangesAsync(cancellationToken);

        var updatedCart = await _cartRepository.GetCartByCustomerIdAsync(customerId, cancellationToken);
        return updatedCart!.ToDto();
    }

    public async Task ClearCartAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        var cart = await _cartRepository.GetCartByCustomerIdAsync(customerId, cancellationToken);
        if (cart == null) return;

        cart.Items.Clear();
        cart.UpdatedAt = DateTime.UtcNow;
        await _cartRepository.SaveChangesAsync(cancellationToken);
    }
}
