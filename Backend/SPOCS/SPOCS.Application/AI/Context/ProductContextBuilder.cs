using System.Text;
using SPOCS.Application.DTOs;

namespace SPOCS.Application.AI.Context;

/// <summary>
/// Builds a clean, structured product context prompt block from retrieved catalog products.
/// </summary>
public static class ProductContextBuilder
{
    public static string Build(IEnumerable<ProductDetailDto> products)
    {
        var productList = products.ToList();
        if (!productList.Any())
        {
            return "[PRODUCT CATALOG CONTEXT: No matching products found in catalog for this query.]";
        }

        var sb = new StringBuilder();
        sb.AppendLine("[PRODUCT CATALOG CONTEXT]");
        sb.AppendLine("Below are the real products retrieved from the SPOCS store database relevant to the user's inquiry:");
        sb.AppendLine();

        int index = 1;
        foreach (var p in productList)
        {
            sb.AppendLine($"--- Product {index}: {p.Name} ---");
            sb.AppendLine($"- ID: {p.Id}");
            sb.AppendLine($"- Category: {p.CategoryName}");
            sb.AppendLine($"- Base Price: ${p.BasePrice:F2}");
            sb.AppendLine($"- Stock Status: {(p.InStock ? $"In Stock ({p.StockQuantity} units)" : "Out of Stock")}");
            
            if (!string.IsNullOrWhiteSpace(p.ShortDescription))
            {
                sb.AppendLine($"- Summary: {p.ShortDescription}");
            }
            if (!string.IsNullOrWhiteSpace(p.Description))
            {
                sb.AppendLine($"- Description: {p.Description}");
            }

            if (p.Features.Any())
            {
                sb.AppendLine("- Key Features:");
                foreach (var feature in p.Features)
                {
                    sb.AppendLine($"  * {feature}");
                }
            }

            if (p.Specifications.Any())
            {
                sb.AppendLine("- Technical Specifications:");
                foreach (var (specKey, specVal) in p.Specifications)
                {
                    sb.AppendLine($"  * {specKey}: {specVal}");
                }
            }

            if (p.Variants.Any())
            {
                sb.AppendLine("- Available Variants:");
                foreach (var variant in p.Variants)
                {
                    var options = string.Join(", ", variant.Options.Select(o => $"{o.Key}: {o.Value}"));
                    sb.AppendLine($"  * SKU: {variant.Sku} | Price: ${variant.Price:F2} | Options: [{options}]");
                }
            }

            sb.AppendLine();
            index++;
        }

        return sb.ToString().TrimEnd();
    }
}
