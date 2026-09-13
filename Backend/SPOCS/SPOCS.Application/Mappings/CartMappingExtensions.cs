using SPOCS.Application.DTOs.Cart;
using SPOCS.Domain.Entities;

namespace SPOCS.Application.Mappings;

public static class CartMappingExtensions
{
    public static CartDto ToDto(this Cart cart)
    {
        return new CartDto
        {
            Id = cart.Id,
            CustomerId = cart.CustomerId,
            CreatedAt = cart.CreatedAt,
            UpdatedAt = cart.UpdatedAt,
            Items = cart.Items.Select(i => i.ToDto()).ToList()
        };
    }

    public static CartItemDto ToDto(this CartItem item)
    {
        // Build a human-readable variant description from variant options
        var variantDescription = item.ProductVariant is not null
            ? string.Join(", ", item.ProductVariant.ProductVariantOptions
                .Where(pvo => pvo.VariantOption != null)
                .Select(pvo => $"{pvo.VariantOption.VariantType?.Name}: {pvo.VariantOption.Value}"))
            : null;

        return new CartItemDto
        {
            Id = item.Id,
            ProductId = item.ProductId,
            ProductName = item.Product?.Name ?? string.Empty,
            ProductImageUrl = item.Product?.Images
                .OrderBy(i => i.DisplayOrder)
                .FirstOrDefault(i => i.IsPrimary)?.ImageUrl
                ?? item.Product?.Images.OrderBy(i => i.DisplayOrder).FirstOrDefault()?.ImageUrl,
            ProductVariantId = item.ProductVariantId,
            VariantDescription = string.IsNullOrWhiteSpace(variantDescription) ? null : variantDescription,
            Quantity = item.Quantity,
            UnitPrice = item.UnitPrice
        };
    }
}
