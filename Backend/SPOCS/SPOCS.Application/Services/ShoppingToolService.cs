using System.Text.Json;
using Microsoft.Extensions.Logging;
using SPOCS.Application.Contracts.AI;
using SPOCS.Application.Contracts.Persistence;
using SPOCS.Application.DTOs.AI;
using SPOCS.Application.Mappings;
using SPOCS.Domain.Entities;

namespace SPOCS.Application.Services;

public class ShoppingToolService : IShoppingToolService
{
    private readonly IEmbeddingService _embeddingService;
    private readonly IProductEmbeddingRepository _embeddingRepository;
    private readonly IProductRepository _productRepository;
    private readonly ILogger<ShoppingToolService> _logger;

    public ShoppingToolService(
        IEmbeddingService embeddingService,
        IProductEmbeddingRepository embeddingRepository,
        IProductRepository productRepository,
        ILogger<ShoppingToolService> logger)
    {
        _embeddingService = embeddingService;
        _embeddingRepository = embeddingRepository;
        _productRepository = productRepository;
        _logger = logger;
    }

    public async Task<ToolExecutionResult> SearchProductsAsync(
        string query,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        string? categorySlug = null,
        bool? inStockOnly = null,
        int topK = 5,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Executing tool SearchProducts: query='{Query}', minPrice={MinPrice}, maxPrice={MaxPrice}, category='{Category}', inStock={InStock}, topK={TopK}",
            query, minPrice, maxPrice, categorySlug, inStockOnly, topK);

        // 1. Semantic Search
        var queryVector = await _embeddingService.GenerateEmbeddingAsync(query, cancellationToken);
        var candidateIds = await _embeddingRepository.SemanticSearchAsync(queryVector, topK: 15, cancellationToken);

        var candidateProducts = new List<Product>();
        foreach (var id in candidateIds)
        {
            var product = await _productRepository.GetByIdAsync(id, cancellationToken);
            if (product != null)
            {
                candidateProducts.Add(product);
            }
        }

        // 2. Structured Filtering
        var filteredProducts = candidateProducts.Where(p =>
        {
            if (minPrice.HasValue && p.BasePrice < minPrice.Value)
                return false;

            if (maxPrice.HasValue && p.BasePrice > maxPrice.Value)
                return false;

            if (!string.IsNullOrWhiteSpace(categorySlug) &&
                !string.Equals(p.Category?.Slug, categorySlug.Trim(), StringComparison.OrdinalIgnoreCase))
                return false;

            if ((inStockOnly ?? true) && p.StockQuantity <= 0)
                return false;

            return true;
        }).Take(topK).ToList();

        // 3. Build Result
        if (filteredProducts.Any())
        {
            var resultsDto = filteredProducts.Select(p => new
            {
                id = p.Id,
                name = p.Name,
                slug = p.Slug,
                category = p.Category?.Name,
                price = p.BasePrice,
                inStock = p.StockQuantity > 0,
                stockQuantity = p.StockQuantity,
                shortSummary = p.ShortDescription,
                features = p.Features,
                specifications = p.Specifications,
                variants = p.Variants.Select(v => new
                {
                    sku = v.Sku,
                    price = v.Price,
                    stock = v.StockQuantity
                })
            }).ToList();

            var references = filteredProducts.Select(p => new ProductReferenceDto
            {
                Id = p.Id,
                Name = p.Name,
                Slug = p.Slug,
                BasePrice = p.BasePrice,
                PrimaryImageUrl = p.Images.FirstOrDefault(i => i.IsPrimary)?.ImageUrl ?? p.Images.FirstOrDefault()?.ImageUrl,
                CategoryName = p.Category?.Name ?? string.Empty,
                InStock = p.StockQuantity > 0
            }).ToList();

            return new ToolExecutionResult
            {
                ResultJson = JsonSerializer.Serialize(new
                {
                    status = "success",
                    matchedCount = filteredProducts.Count,
                    products = resultsDto
                }),
                Products = references
            };
        }

        // Handle case where semantic matches were found but failed structured constraints
        if (candidateProducts.Any())
        {
            var closestFilteredOut = candidateProducts.Take(3).Select(p => new
            {
                name = p.Name,
                price = p.BasePrice,
                category = p.Category?.Name,
                reason = maxPrice.HasValue && p.BasePrice > maxPrice.Value ? $"Exceeds max budget of ${maxPrice.Value:F2}" : "Does not match structured filter"
            }).ToList();

            return new ToolExecutionResult
            {
                ResultJson = JsonSerializer.Serialize(new
                {
                    status = "filtered_out",
                    message = "Products semantically related to your search were found, but none satisfied all structured filters (e.g. price limits or category).",
                    closestMatches = closestFilteredOut
                }),
                Products = new List<ProductReferenceDto>()
            };
        }

        return new ToolExecutionResult
        {
            ResultJson = JsonSerializer.Serialize(new
            {
                status = "not_found",
                message = "No products found in the catalog matching the search query."
            }),
            Products = new List<ProductReferenceDto>()
        };
    }

    public async Task<ToolExecutionResult> GetProductDetailsAsync(
        Guid? productId = null,
        string? slug = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Executing tool GetProductDetails: id={Id}, slug='{Slug}'", productId, slug);

        Product? product = null;
        if (productId.HasValue && productId.Value != Guid.Empty)
        {
            product = await _productRepository.GetByIdAsync(productId.Value, cancellationToken);
        }
        else if (!string.IsNullOrWhiteSpace(slug))
        {
            product = await _productRepository.GetBySlugAsync(slug, cancellationToken);
        }

        if (product == null)
        {
            return new ToolExecutionResult
            {
                ResultJson = JsonSerializer.Serialize(new { status = "not_found", message = "Product not found." })
            };
        }

        var detail = product.ToDetailDto();
        var references = new List<ProductReferenceDto>
        {
            new()
            {
                Id = detail.Id,
                Name = detail.Name,
                Slug = detail.Slug,
                BasePrice = detail.BasePrice,
                PrimaryImageUrl = detail.Images.FirstOrDefault(i => i.IsPrimary)?.ImageUrl ?? detail.Images.FirstOrDefault()?.ImageUrl,
                CategoryName = detail.CategoryName,
                InStock = detail.InStock
            }
        };

        return new ToolExecutionResult
        {
            ResultJson = JsonSerializer.Serialize(new
            {
                status = "success",
                product = detail
            }),
            Products = references
        };
    }

    public async Task<ToolExecutionResult> FindSimilarProductsAsync(
        Guid productId,
        int topK = 3,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Executing tool FindSimilarProducts: productId={ProductId}, topK={TopK}", productId, topK);

        var embedding = await _embeddingRepository.GetByProductIdAsync(productId, cancellationToken);
        if (embedding == null)
        {
            return new ToolExecutionResult
            {
                ResultJson = JsonSerializer.Serialize(new { status = "not_found", message = "No embedding found for product." })
            };
        }

        // Retrieve topK + 1 to exclude the product itself
        var similarIds = await _embeddingRepository.SemanticSearchAsync(embedding.Vector, topK + 1, cancellationToken);
        var filteredIds = similarIds.Where(id => id != productId).Take(topK).ToList();

        var products = new List<Product>();
        foreach (var id in filteredIds)
        {
            var p = await _productRepository.GetByIdAsync(id, cancellationToken);
            if (p != null) products.Add(p);
        }

        var references = products.Select(p => new ProductReferenceDto
        {
            Id = p.Id,
            Name = p.Name,
            Slug = p.Slug,
            BasePrice = p.BasePrice,
            PrimaryImageUrl = p.Images.FirstOrDefault(i => i.IsPrimary)?.ImageUrl ?? p.Images.FirstOrDefault()?.ImageUrl,
            CategoryName = p.Category?.Name ?? string.Empty,
            InStock = p.StockQuantity > 0
        }).ToList();

        return new ToolExecutionResult
        {
            ResultJson = JsonSerializer.Serialize(new
            {
                status = "success",
                similarProducts = products.Select(p => new
                {
                    id = p.Id,
                    name = p.Name,
                    slug = p.Slug,
                    price = p.BasePrice,
                    category = p.Category?.Name,
                    summary = p.ShortDescription
                })
            }),
            Products = references
        };
    }
}
