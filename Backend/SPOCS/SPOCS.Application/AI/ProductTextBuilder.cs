using SPOCS.Domain.Entities;

namespace SPOCS.Application.AI;

/// <summary>
/// Builds the searchable text representation of a product for embedding generation.
/// Only semantic content is included — structured fields like price/stock are excluded.
/// </summary>
public static class ProductTextBuilder
{
    public static string Build(Product product)
    {
        var parts = new List<string>();

        parts.Add($"Product: {product.Name}");
        parts.Add($"Category: {product.Category?.Name ?? "Unknown"}");

        if (!string.IsNullOrWhiteSpace(product.Description))
            parts.Add($"Description: {product.Description}");

        if (!string.IsNullOrWhiteSpace(product.ShortDescription))
            parts.Add($"Summary: {product.ShortDescription}");

        if (product.Features?.Count > 0)
            parts.Add($"Features: {string.Join(", ", product.Features)}");

        if (product.Specifications?.Count > 0)
        {
            var specs = string.Join(", ", product.Specifications.Select(kv => $"{kv.Key}: {kv.Value}"));
            parts.Add($"Specifications: {specs}");
        }

        if (product.Variants?.Count > 0)
        {
            var variantSummaries = product.Variants
                .Select(v =>
                {
                    var options = v.ProductVariantOptions?
                        .Where(pvo => pvo.VariantOption != null)
                        .Select(pvo => $"{pvo.VariantOption.VariantType?.Name}: {pvo.VariantOption.Value}");
                    return options != null ? string.Join(", ", options) : null;
                })
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct()
                .Take(10); // cap to avoid overly long embedding text

            if (variantSummaries.Any())
                parts.Add($"Available variants: {string.Join(" | ", variantSummaries)}");
        }

        return string.Join("\n", parts);
    }
}
