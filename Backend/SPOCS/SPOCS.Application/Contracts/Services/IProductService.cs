using SPOCS.Application.Common.Models;
using SPOCS.Application.DTOs;

namespace SPOCS.Application.Contracts.Services;

public interface IProductService
{
    Task<PaginatedResult<ProductListDto>> GetProductsPagedAsync(ProductFilterParams filterParams, CancellationToken cancellationToken = default);
    Task<ProductDetailDto?> GetProductByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ProductDetailDto?> GetProductBySlugAsync(string slug, CancellationToken cancellationToken = default);
}
