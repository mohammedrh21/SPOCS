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

    private static readonly string[] KnownBrands = new[]
    {
        "Apple", "Samsung", "Sony", "Bose", "MSI", "ASUS", "HP", "Dell", "Lenovo", "Razer",
        "Acer", "Google", "Logitech", "SteelSeries", "Garmin", "Microsoft", "Nintendo", "Valve",
        "Keychron", "Anker", "TP-Link", "Sonos", "Sennheiser"
    };

    public async Task<ToolExecutionResult> SearchProductsAsync(
        string query,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        string? categorySlug = null,
        string? brand = null,
        bool? inStockOnly = null,
        int topK = 5,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Executing tool SearchProducts: query='{Query}', minPrice={MinPrice}, maxPrice={MaxPrice}, category='{Category}', brand='{Brand}', inStock={InStock}, topK={TopK}",
            query, minPrice, maxPrice, categorySlug, brand, inStockOnly, topK);

        // 1. Infer Category if not specified
        if (string.IsNullOrWhiteSpace(categorySlug))
        {
            var qLower = query.ToLowerInvariant();
            if (qLower.Contains("laptop") || qLower.Contains("notebook") || qLower.Contains("macbook") || qLower.Contains("ultrabook") || qLower.Contains("thinkpad") || qLower.Contains("chromebook"))
                categorySlug = "laptops";
            else if (qLower.Contains("phone") || qLower.Contains("iphone") || qLower.Contains("smartphone") || qLower.Contains("pixel") || qLower.Contains("galaxy s"))
                categorySlug = "smartphones";
            else if (qLower.Contains("headphone") || qLower.Contains("earbud") || qLower.Contains("headset") || qLower.Contains("earphone") || qLower.Contains("audio"))
                categorySlug = "headphones-audio";
            else if (qLower.Contains("watch") || qLower.Contains("smartwatch") || qLower.Contains("fitness tracker") || qLower.Contains("band"))
                categorySlug = "wearables-smartwatches";
            else if (qLower.Contains("tablet") || qLower.Contains("ipad") || qLower.Contains("ereader") || qLower.Contains("kindle") || qLower.Contains("tab"))
                categorySlug = "tablets-ereaders";
            else if (qLower.Contains("console") || qLower.Contains("playstation") || qLower.Contains("xbox") || qLower.Contains("nintendo") || qLower.Contains("steam deck") || qLower.Contains("vr headset"))
                categorySlug = "gaming-consoles";
            else if (qLower.Contains("keyboard") || qLower.Contains("mouse") || qLower.Contains("monitor") || qLower.Contains("webcam") || qLower.Contains("docking"))
                categorySlug = "computer-accessories";
            else if (qLower.Contains("router") || qLower.Contains("mesh") || qLower.Contains("wifi") || qLower.Contains("wi-fi") || qLower.Contains("smart home") || qLower.Contains("smart bulb") || qLower.Contains("thermostat"))
                categorySlug = "smart-home-networking";
        }

        // 2. Infer Brand if not specified
        if (string.IsNullOrWhiteSpace(brand))
        {
            foreach (var knownBrand in KnownBrands)
            {
                if (System.Text.RegularExpressions.Regex.IsMatch(query, $@"\b{System.Text.RegularExpressions.Regex.Escape(knownBrand)}\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                {
                    brand = knownBrand;
                    break;
                }
            }
        }

        // 3. Hybrid Search: Semantic Vector Search + Keyword Text Search
        var candidateMap = new Dictionary<Guid, Product>();

        // 3a. Vector search with cosine distance threshold (max 0.65 to avoid completely unrelated items)
        try
        {
            var queryVector = await _embeddingService.GenerateEmbeddingAsync(query, cancellationToken);
            var semanticIds = await _embeddingRepository.SemanticSearchAsync(queryVector, topK: 20, maxDistance: 0.65, cancellationToken: cancellationToken);

            foreach (var id in semanticIds)
            {
                var product = await _productRepository.GetByIdAsync(id, cancellationToken);
                if (product != null)
                {
                    candidateMap[product.Id] = product;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Vector search encountered an issue, continuing with text search fallback.");
        }

        // 3b. Keyword text search to guarantee exact brand/name matches
        var searchKeywords = new List<string>();
        if (!string.IsNullOrWhiteSpace(brand))
        {
            searchKeywords.Add(brand);
        }

        var terms = query.Split(new[] { ' ', ',', '-', '/', ';' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(t => t.Length >= 3 && !t.Equals("laptop", StringComparison.OrdinalIgnoreCase) && !t.Equals("phone", StringComparison.OrdinalIgnoreCase) && !t.Equals("under", StringComparison.OrdinalIgnoreCase) && !t.Equals("upper", StringComparison.OrdinalIgnoreCase) && !t.Equals("find", StringComparison.OrdinalIgnoreCase))
            .Take(3);
        searchKeywords.AddRange(terms);

        foreach (var kw in searchKeywords.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var (textMatches, _) = await _productRepository.GetPagedAsync(new Common.Models.ProductFilterParams
            {
                SearchTerm = kw,
                CategorySlug = categorySlug,
                MinPrice = minPrice,
                MaxPrice = maxPrice,
                PageSize = 10
            }, cancellationToken);

            foreach (var p in textMatches)
            {
                candidateMap.TryAdd(p.Id, p);
            }
        }

        var candidateProducts = candidateMap.Values.ToList();

        // 4. Structured Filtering
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
        }).ToList();

        // 5. Brand Re-ranking / Filtering
        // If a specific brand was requested, strictly isolate products of that brand.
        if (!string.IsNullOrWhiteSpace(brand))
        {
            var brandMatches = filteredProducts.Where(p =>
                p.Name.Contains(brand, StringComparison.OrdinalIgnoreCase) ||
                p.Sku.Contains(brand, StringComparison.OrdinalIgnoreCase) ||
                (p.Description != null && p.Description.Contains(brand, StringComparison.OrdinalIgnoreCase))
            ).ToList();

            if (brandMatches.Any())
            {
                // Strict brand match: only return products of the requested brand
                filteredProducts = brandMatches;
            }
            else
            {
                // No products of the requested brand met the price/category constraints.
                // Look for any products of this brand in candidateMap or database to explain why.
                var anyBrandInCatalog = candidateMap.Values.Where(p =>
                    p.Name.Contains(brand, StringComparison.OrdinalIgnoreCase) ||
                    p.Sku.Contains(brand, StringComparison.OrdinalIgnoreCase) ||
                    (p.Description != null && p.Description.Contains(brand, StringComparison.OrdinalIgnoreCase))
                ).ToList();

                if (!anyBrandInCatalog.Any())
                {
                    var (allBrandProducts, _) = await _productRepository.GetPagedAsync(new Common.Models.ProductFilterParams
                    {
                        SearchTerm = brand,
                        CategorySlug = categorySlug,
                        PageSize = 5
                    }, cancellationToken);
                    anyBrandInCatalog = allBrandProducts.ToList();
                }

                if (anyBrandInCatalog.Any())
                {
                    var closestBrandProducts = anyBrandInCatalog.Select(p => new
                    {
                        name = p.Name,
                        price = p.BasePrice,
                        category = p.Category?.Name,
                        reason = maxPrice.HasValue && p.BasePrice > maxPrice.Value
                            ? $"Priced at ${p.BasePrice:F2}, which exceeds your maximum budget of ${maxPrice.Value:F2}"
                            : minPrice.HasValue && p.BasePrice < minPrice.Value
                            ? $"Priced at ${p.BasePrice:F2}, which is below your minimum price of ${minPrice.Value:F2}"
                            : "Does not match current filter constraints"
                    }).ToList();

                    return new ToolExecutionResult
                    {
                        ResultJson = JsonSerializer.Serialize(new
                        {
                            status = "filtered_out",
                            requestedBrand = brand,
                            message = $"No {brand} models were found matching your constraints (e.g. max price ${(maxPrice.HasValue ? maxPrice.Value.ToString("F2") : "N/A")}). The available {brand} models in our catalog are listed below.",
                            availableBrandModels = closestBrandProducts
                        }),
                        Products = anyBrandInCatalog.Take(2).Select(p => new ProductReferenceDto
                        {
                            Id = p.Id,
                            Name = p.Name,
                            Slug = p.Slug,
                            BasePrice = p.BasePrice,
                            PrimaryImageUrl = p.Images.FirstOrDefault(i => i.IsPrimary)?.ImageUrl ?? p.Images.FirstOrDefault()?.ImageUrl,
                            CategoryName = p.Category?.Name ?? string.Empty,
                            InStock = p.StockQuantity > 0
                        }).ToList()
                    };
                }

                return new ToolExecutionResult
                {
                    ResultJson = JsonSerializer.Serialize(new
                    {
                        status = "not_found",
                        requestedBrand = brand,
                        message = $"No products from brand '{brand}' exist in the SPOCS catalog."
                    }),
                    Products = new List<ProductReferenceDto>()
                };
            }
        }

        // Cap to topK
        filteredProducts = filteredProducts.Take(topK).ToList();

        // 6. Build Result
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
                    matchedBrand = brand,
                    appliedCategory = categorySlug,
                    products = resultsDto
                }),
                Products = references
            };
        }

        // Handle case where products were found but failed structured constraints
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
                    message = "Products related to your search were found, but none satisfied all structured filters (e.g. price limits or category).",
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
                message = !string.IsNullOrWhiteSpace(brand)
                    ? $"No products from brand '{brand}' found matching the search criteria in the catalog."
                    : "No products found in the catalog matching the search query."
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
