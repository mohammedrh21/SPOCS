using Microsoft.EntityFrameworkCore;
using SPOCS.Application.Contracts.Persistence;
using SPOCS.Domain.Entities;
using SPOCS.Infrastructure.Data;

namespace SPOCS.Infrastructure.Repositories;

public class CartRepository : ICartRepository
{
    private readonly ApplicationDbContext _context;

    public CartRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Cart?> GetCartByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        return await _context.Carts
            .Include(c => c.Items)
                .ThenInclude(i => i.Product)
                    .ThenInclude(p => p.Images)
            .Include(c => c.Items)
                .ThenInclude(i => i.ProductVariant!)
                    .ThenInclude(pv => pv.ProductVariantOptions)
                        .ThenInclude(pvo => pvo.VariantOption)
                            .ThenInclude(vo => vo.VariantType)
            .FirstOrDefaultAsync(c => c.CustomerId == customerId, cancellationToken);
    }

    public async Task<Cart?> GetCartByCustomerIdNoTrackingAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        return await _context.Carts
            .AsNoTracking()
            .Include(c => c.Items)
                .ThenInclude(i => i.Product)
                    .ThenInclude(p => p.Images)
            .Include(c => c.Items)
                .ThenInclude(i => i.ProductVariant!)
                    .ThenInclude(pv => pv.ProductVariantOptions)
                        .ThenInclude(pvo => pvo.VariantOption)
                            .ThenInclude(vo => vo.VariantType)
            .FirstOrDefaultAsync(c => c.CustomerId == customerId, cancellationToken);
    }

    public async Task CreateCartAsync(Cart cart, CancellationToken cancellationToken = default)
    {
        await _context.Carts.AddAsync(cart, cancellationToken);
    }

    public void AddItem(CartItem item)
    {
        _context.CartItems.Add(item);
    }

    public void RemoveItem(CartItem item)
    {
        _context.CartItems.Remove(item);
    }

    public void RemoveItems(IEnumerable<CartItem> items)
    {
        _context.CartItems.RemoveRange(items);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
