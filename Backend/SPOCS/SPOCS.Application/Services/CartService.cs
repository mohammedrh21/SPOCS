using SPOCS.Application.Common.Exceptions;
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

    // Maximum number of times we will retry after a CartConcurrencyException.
    // The exception wraps EF Core's DbUpdateConcurrencyException, which occurs
    // when the change-tracker holds a stale IdentityUser.ConcurrencyStamp loaded
    // by the auth middleware on the same scoped DbContext.
    // Detaching all tracked entities and re-fetching the cart gives EF fresh
    // tokens so the next SaveChanges succeeds.
    private const int MaxRetries = 3;

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
        // Validate product exists (AsNoTracking in ProductRepository — safe to call once)
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

        for (int attempt = 1; attempt <= MaxRetries; attempt++)
        {
            try
            {
                // Detach stale tracked entities on retry
                if (attempt > 1) _cartRepository.DetachAll();

                var cart = await _cartRepository.GetCartByCustomerIdAsync(customerId, cancellationToken);
                if (cart == null)
                {
                    cart = new Cart { CustomerId = customerId };
                    await _cartRepository.CreateCartAsync(cart, cancellationToken);
                }

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

                var updatedCart = await _cartRepository.GetCartByCustomerIdAsync(customerId, cancellationToken);
                return updatedCart!.ToDto();
            }
            catch (CartConcurrencyException) when (attempt < MaxRetries)
            {
                // Transient concurrency conflict — detach and retry on next iteration
            }
        }

        // Final attempt — let any exception propagate
        _cartRepository.DetachAll();
        var freshCart = await _cartRepository.GetCartByCustomerIdAsync(customerId, cancellationToken);
        if (freshCart == null)
        {
            freshCart = new Cart { CustomerId = customerId };
            await _cartRepository.CreateCartAsync(freshCart, cancellationToken);
        }

        var finalItem = freshCart.Items.FirstOrDefault(i =>
            i.ProductId == request.ProductId &&
            i.ProductVariantId == request.ProductVariantId);

        if (finalItem != null) finalItem.Quantity += request.Quantity;
        else
        {
            freshCart.Items.Add(new CartItem
            {
                CartId = freshCart.Id,
                ProductId = request.ProductId,
                ProductVariantId = request.ProductVariantId,
                Quantity = request.Quantity,
                UnitPrice = unitPrice
            });
        }

        freshCart.UpdatedAt = DateTime.UtcNow;
        await _cartRepository.SaveChangesAsync(cancellationToken);
        var result = await _cartRepository.GetCartByCustomerIdAsync(customerId, cancellationToken);
        return result!.ToDto();
    }

    public async Task<CartDto> UpdateCartItemAsync(Guid customerId, Guid itemId, UpdateCartItemDto request, CancellationToken cancellationToken = default)
    {
        for (int attempt = 1; attempt <= MaxRetries; attempt++)
        {
            try
            {
                if (attempt > 1) _cartRepository.DetachAll();

                var cart = await _cartRepository.GetCartByCustomerIdAsync(customerId, cancellationToken)
                    ?? throw new InvalidOperationException("Cart not found.");

                var item = cart.Items.FirstOrDefault(i => i.Id == itemId)
                    ?? throw new InvalidOperationException($"Cart item '{itemId}' not found.");

                if (request.Quantity <= 0) cart.Items.Remove(item);
                else item.Quantity = request.Quantity;

                cart.UpdatedAt = DateTime.UtcNow;
                await _cartRepository.SaveChangesAsync(cancellationToken);

                var updatedCart = await _cartRepository.GetCartByCustomerIdAsync(customerId, cancellationToken);
                return updatedCart!.ToDto();
            }
            catch (CartConcurrencyException) when (attempt < MaxRetries) { }
        }

        _cartRepository.DetachAll();
        var freshCart = await _cartRepository.GetCartByCustomerIdAsync(customerId, cancellationToken)
            ?? throw new InvalidOperationException("Cart not found.");
        var freshItem = freshCart.Items.FirstOrDefault(i => i.Id == itemId)
            ?? throw new InvalidOperationException($"Cart item '{itemId}' not found.");

        if (request.Quantity <= 0) freshCart.Items.Remove(freshItem);
        else freshItem.Quantity = request.Quantity;

        freshCart.UpdatedAt = DateTime.UtcNow;
        await _cartRepository.SaveChangesAsync(cancellationToken);
        var finalCart = await _cartRepository.GetCartByCustomerIdAsync(customerId, cancellationToken);
        return finalCart!.ToDto();
    }

    public async Task<CartDto> RemoveFromCartAsync(Guid customerId, Guid itemId, CancellationToken cancellationToken = default)
    {
        for (int attempt = 1; attempt <= MaxRetries; attempt++)
        {
            try
            {
                if (attempt > 1) _cartRepository.DetachAll();

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
            catch (CartConcurrencyException) when (attempt < MaxRetries) { }
        }

        _cartRepository.DetachAll();
        var freshCart = await _cartRepository.GetCartByCustomerIdAsync(customerId, cancellationToken)
            ?? throw new InvalidOperationException("Cart not found.");
        var freshItem = freshCart.Items.FirstOrDefault(i => i.Id == itemId)
            ?? throw new InvalidOperationException($"Cart item '{itemId}' not found.");

        freshCart.Items.Remove(freshItem);
        freshCart.UpdatedAt = DateTime.UtcNow;
        await _cartRepository.SaveChangesAsync(cancellationToken);
        var finalCart = await _cartRepository.GetCartByCustomerIdAsync(customerId, cancellationToken);
        return finalCart!.ToDto();
    }

    public async Task ClearCartAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        for (int attempt = 1; attempt <= MaxRetries; attempt++)
        {
            try
            {
                if (attempt > 1) _cartRepository.DetachAll();

                var cart = await _cartRepository.GetCartByCustomerIdAsync(customerId, cancellationToken);
                if (cart == null) return;

                cart.Items.Clear();
                cart.UpdatedAt = DateTime.UtcNow;
                await _cartRepository.SaveChangesAsync(cancellationToken);
                return;
            }
            catch (CartConcurrencyException) when (attempt < MaxRetries) { }
        }

        _cartRepository.DetachAll();
        var freshCart = await _cartRepository.GetCartByCustomerIdAsync(customerId, cancellationToken);
        if (freshCart == null) return;

        freshCart.Items.Clear();
        freshCart.UpdatedAt = DateTime.UtcNow;
        await _cartRepository.SaveChangesAsync(cancellationToken);
    }
}
