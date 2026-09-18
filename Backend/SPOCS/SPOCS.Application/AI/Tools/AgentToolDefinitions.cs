using SPOCS.Application.DTOs.AI;

namespace SPOCS.Application.AI.Tools;

/// <summary>
/// Predefined tool definitions (JSON Schemas) available to the AI Shopping Agent.
/// </summary>
public static class AgentToolDefinitions
{
    public static readonly IReadOnlyList<ToolDefinitionDto> Tools = new List<ToolDefinitionDto>
    {
        new()
        {
            Type = "function",
            Function = new FunctionDefinitionDto
            {
                Name = "SearchProducts",
                Description = "Searches the SPOCS product catalog using semantic vector search combined with structured filters. Use this when the customer is looking for products matching features, budget, or categories.",
                Parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        query = new
                        {
                            type = "string",
                            description = "The natural language search query describing the product features, brand, or use case (e.g., 'gaming laptop', 'noise cancelling earbuds', 'budget phone')"
                        },
                        minPrice = new
                        {
                            type = "number",
                            description = "Optional minimum price constraint in USD (e.g., 500)"
                        },
                        maxPrice = new
                        {
                            type = "number",
                            description = "Optional maximum price constraint in USD (e.g., 1000)"
                        },
                        categorySlug = new
                        {
                            type = "string",
                            description = "Optional category slug to filter by: 'laptops', 'smartphones', 'headphones-audio', 'wearables-smartwatches', 'tablets-ereaders', 'gaming-consoles', 'computer-accessories', 'smart-home-networking'"
                        },
                        brand = new
                        {
                            type = "string",
                            description = "Optional brand name to filter or prioritize (e.g., 'MSI', 'Apple', 'Sony', 'Samsung', 'Dell', 'HP', 'ASUS', 'Lenovo', 'Bose', 'Razer', 'Logitech')"
                        },
                        inStockOnly = new
                        {
                            type = "boolean",
                            description = "Whether to restrict results to in-stock items only. Default is true."
                        }
                    },
                    required = new[] { "query" }
                }
            }
        },
        new()
        {
            Type = "function",
            Function = new FunctionDefinitionDto
            {
                Name = "GetProductDetails",
                Description = "Retrieves complete details, full specifications, variants, and stock information for a single specific product by its GUID or slug.",
                Parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        productId = new
                        {
                            type = "string",
                            description = "The unique GUID of the product"
                        },
                        slug = new
                        {
                            type = "string",
                            description = "The URL slug of the product (e.g., 'macbook-pro-14-m3-pro', 'acer-aspire-5-15')"
                        }
                    }
                }
            }
        },
        new()
        {
            Type = "function",
            Function = new FunctionDefinitionDto
            {
                Name = "FindSimilarProducts",
                Description = "Finds alternative or similar products to a given product in the catalog using vector similarity distance.",
                Parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        productId = new
                        {
                            type = "string",
                            description = "The GUID of the product to find alternatives for"
                        },
                        topK = new
                        {
                            type = "integer",
                            description = "Number of similar products to retrieve (default 3)"
                        }
                    },
                    required = new[] { "productId" }
                }
            }
        }
    };
}
