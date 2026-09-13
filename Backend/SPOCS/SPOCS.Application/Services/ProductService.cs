using SPOCS.Application.Common.Models;
using SPOCS.Application.Contracts.Persistence;
using SPOCS.Application.Contracts.Services;
using SPOCS.Application.DTOs;
using SPOCS.Application.Mappings;

namespace SPOCS.Application.Services;

public class ProductService : IProductService
{
    private readonly IProductRepository _productRepository;

    public ProductService(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<PaginatedResult<ProductListDto>> GetProductsPagedAsync(
        ProductFilterParams filterParams,
        CancellationToken cancellationToken = default)
    {
        var (items, totalCount) = await _productRepository.GetPagedAsync(filterParams, cancellationToken);

        var dtos = items.Select(p => p.ToListDto()).ToList();

        return new PaginatedResult<ProductListDto>(dtos, totalCount, filterParams.PageIndex, filterParams.PageSize);
    }

    public async Task<ProductDetailDto?> GetProductByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await _productRepository.GetByIdAsync(id, cancellationToken);
        return product?.ToDetailDto();
    }

    public async Task<ProductDetailDto?> GetProductBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var product = await _productRepository.GetBySlugAsync(slug, cancellationToken);
        return product?.ToDetailDto();
    }
}
