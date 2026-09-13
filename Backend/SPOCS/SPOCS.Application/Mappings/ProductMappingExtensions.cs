using SPOCS.Application.DTOs;
using SPOCS.Domain.Entities;

namespace SPOCS.Application.Mappings;

public static class ProductMappingExtensions
{
    public static ProductListDto ToListDto(this Product product)
    {
        return new ProductListDto
        {
            Id = product.Id,
            Name = product.Name,
            Slug = product.Slug,
            ShortDescription = product.ShortDescription,
            BasePrice = product.BasePrice,
            Sku = product.Sku,
            InStock = product.StockQuantity > 0,
            CategoryId = product.CategoryId,
            CategoryName = product.Category?.Name ?? string.Empty,
            PrimaryImageUrl = product.Images.OrderBy(i => i.DisplayOrder).FirstOrDefault(i => i.IsPrimary)?.ImageUrl
                              ?? product.Images.OrderBy(i => i.DisplayOrder).FirstOrDefault()?.ImageUrl,
            VariantCount = product.Variants.Count
        };
    }

    public static ProductDetailDto ToDetailDto(this Product product)
    {
        return new ProductDetailDto
        {
            Id = product.Id,
            Name = product.Name,
            Slug = product.Slug,
            Description = product.Description,
            ShortDescription = product.ShortDescription,
            BasePrice = product.BasePrice,
            Sku = product.Sku,
            StockQuantity = product.StockQuantity,
            CategoryId = product.CategoryId,
            CategoryName = product.Category?.Name ?? string.Empty,
            CategorySlug = product.Category?.Slug ?? string.Empty,
            Features = product.Features ?? new List<string>(),
            Specifications = product.Specifications ?? new Dictionary<string, string>(),
            Images = product.Images
                .OrderBy(i => i.DisplayOrder)
                .Select(i => new ProductImageDto
                {
                    Id = i.Id,
                    ImageUrl = i.ImageUrl,
                    AltText = i.AltText,
                    IsPrimary = i.IsPrimary,
                    DisplayOrder = i.DisplayOrder
                })
                .ToList(),
            Variants = product.Variants.Select(v => new ProductVariantDto
            {
                Id = v.Id,
                Sku = v.Sku,
                Price = v.Price,
                StockQuantity = v.StockQuantity,
                ImageUrl = v.ImageUrl,
                Options = v.ProductVariantOptions
                    .Where(pvo => pvo.VariantOption != null && pvo.VariantOption.VariantType != null)
                    .ToDictionary(
                        pvo => pvo.VariantOption.VariantType.Name,
                        pvo => pvo.VariantOption.Value)
            }).ToList(),
            CreatedAt = product.CreatedAt
        };
    }

    public static CategoryDto ToDto(this Category category)
    {
        return new CategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            Slug = category.Slug,
            Description = category.Description,
            ImageUrl = category.ImageUrl,
            ProductCount = category.Products?.Count ?? 0
        };
    }
}
