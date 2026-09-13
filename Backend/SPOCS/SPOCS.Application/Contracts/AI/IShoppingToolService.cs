using SPOCS.Application.DTOs.AI;

namespace SPOCS.Application.Contracts.AI;

public class ToolExecutionResult
{
    /// <summary>
    /// JSON output provided to the LLM as the tool response message.
    /// </summary>
    public string ResultJson { get; set; } = string.Empty;

    /// <summary>
    /// Structured product references retrieved by the tool, for frontend display and citations.
    /// </summary>
    public List<ProductReferenceDto> Products { get; set; } = new();
}

/// <summary>
/// Defines the autonomous tools that the AI Shopping Agent can execute.
/// </summary>
public interface IShoppingToolService
{
    /// <summary>
    /// Performs semantic vector search combined with structured SQL / relational filters (price range, category, stock).
    /// </summary>
    Task<ToolExecutionResult> SearchProductsAsync(
        string query,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        string? categorySlug = null,
        bool? inStockOnly = null,
        int topK = 5,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves comprehensive product details, specifications, and variants by ID or slug.
    /// </summary>
    Task<ToolExecutionResult> GetProductDetailsAsync(
        Guid? productId = null,
        string? slug = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds catalog products similar to a given product using vector distance.
    /// </summary>
    Task<ToolExecutionResult> FindSimilarProductsAsync(
        Guid productId,
        int topK = 3,
        CancellationToken cancellationToken = default);
}
