using Microsoft.EntityFrameworkCore;
using SPOCS.Application.Common.Models;
using SPOCS.Application.Contracts.Persistence;
using SPOCS.Domain.Entities;
using SPOCS.Infrastructure.Data;

namespace SPOCS.Infrastructure.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly ApplicationDbContext _context;

    public ProductRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Images)
            .Include(p => p.Variants)
                .ThenInclude(v => v.ProductVariantOptions)
                .ThenInclude(pvo => pvo.VariantOption)
                .ThenInclude(vo => vo.VariantType)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<Product?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var normalizedSlug = slug.Trim().ToLowerInvariant();
        return await _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Images)
            .Include(p => p.Variants)
                .ThenInclude(v => v.ProductVariantOptions)
                .ThenInclude(pvo => pvo.VariantOption)
                .ThenInclude(vo => vo.VariantType)
            .FirstOrDefaultAsync(p => p.Slug == normalizedSlug, cancellationToken);
    }

    public async Task<(IReadOnlyList<Product> Items, int TotalCount)> GetPagedAsync(
        ProductFilterParams filterParams,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Products.AsNoTracking().AsQueryable();

        // 1. Filtering
        if (filterParams.CategoryId.HasValue && filterParams.CategoryId.Value != Guid.Empty)
        {
            query = query.Where(p => p.CategoryId == filterParams.CategoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filterParams.CategorySlug))
        {
            var catSlug = filterParams.CategorySlug.Trim().ToLowerInvariant();
            query = query.Where(p => p.Category.Slug == catSlug);
        }

        if (filterParams.MinPrice.HasValue)
        {
            query = query.Where(p => p.BasePrice >= filterParams.MinPrice.Value);
        }

        if (filterParams.MaxPrice.HasValue)
        {
            query = query.Where(p => p.BasePrice <= filterParams.MaxPrice.Value);
        }

        if (!string.IsNullOrWhiteSpace(filterParams.SearchTerm))
        {
            var search = filterParams.SearchTerm.Trim().ToLowerInvariant();
            query = query.Where(p =>
                EF.Functions.ILike(p.Name, $"%{search}%") ||
                EF.Functions.ILike(p.Description, $"%{search}%") ||
                EF.Functions.ILike(p.Sku, $"%{search}%"));
        }

        // 2. Total Count
        var totalCount = await query.CountAsync(cancellationToken);

        // 3. Sorting
        query = filterParams.SortBy?.ToLowerInvariant() switch
        {
            "price_asc" => query.OrderBy(p => p.BasePrice),
            "price_desc" => query.OrderByDescending(p => p.BasePrice),
            "name_asc" => query.OrderBy(p => p.Name),
            "name_desc" => query.OrderByDescending(p => p.Name),
            "newest" => query.OrderByDescending(p => p.CreatedAt),
            _ => query.OrderByDescending(p => p.CreatedAt)
        };

        // 4. Paging
        var pageIndex = Math.Max(1, filterParams.PageIndex);
        var skip = (pageIndex - 1) * filterParams.PageSize;
        var items = await query
            .Include(p => p.Category)
            .Include(p => p.Images)
            .Include(p => p.Variants)
            .Skip(skip)
            .Take(filterParams.PageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Images)
            .Include(p => p.Variants)
                .ThenInclude(v => v.ProductVariantOptions)
                .ThenInclude(pvo => pvo.VariantOption)
                .ThenInclude(vo => vo.VariantType)
            .ToListAsync(cancellationToken);
    }
}
