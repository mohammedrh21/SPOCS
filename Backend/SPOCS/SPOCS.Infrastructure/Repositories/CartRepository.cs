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
                .ThenInclude(i => i.ProductVariant)
                    .ThenInclude(v => v!.ProductVariantOptions)
                        .ThenInclude(pvo => pvo.VariantOption)
                            .ThenInclude(vo => vo.VariantType)
            .FirstOrDefaultAsync(c => c.CustomerId == customerId, cancellationToken);
    }

    public async Task CreateCartAsync(Cart cart, CancellationToken cancellationToken = default)
    {
        await _context.Carts.AddAsync(cart, cancellationToken);
        // Do NOT call SaveChangesAsync here — the caller (CartService) will save
        // everything atomically once cart items are also staged. Calling SaveChangesAsync
        // here and then again in the service caused a DbUpdateConcurrencyException because
        // EF Core tried to UPDATE the already-saved Cart when only an INSERT was expected.
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
