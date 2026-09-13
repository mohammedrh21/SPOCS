using Microsoft.AspNetCore.Mvc;
using SPOCS.Application.Common.Models;
using SPOCS.Application.Contracts.AI;
using SPOCS.Application.Contracts.Services;
using SPOCS.Application.DTOs;

namespace SPOCS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;
    private readonly IProductEmbeddingService _embeddingService;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(
        IProductService productService,
        IProductEmbeddingService embeddingService,
        ILogger<ProductsController> logger)
    {
        _productService = productService;
        _embeddingService = embeddingService;
        _logger = logger;
    }

    /// <summary>
    /// Get paginated list of products with optional category, price, and search term filters.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResult<ProductListDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResult<ProductListDto>>> GetProducts(
        [FromQuery] ProductFilterParams filterParams,
        CancellationToken cancellationToken)
    {
        var result = await _productService.GetProductsPagedAsync(filterParams, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get detailed product information by its unique GUID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ProductDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDetailDto>> GetProductById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var product = await _productService.GetProductByIdAsync(id, cancellationToken);
        if (product == null)
        {
            return NotFound(new { message = $"Product with ID '{id}' was not found." });
        }

        return Ok(product);
    }

    /// <summary>
    /// Get detailed product information by its SEO-friendly URL slug.
    /// </summary>
    [HttpGet("slug/{slug}")]
    [ProducesResponseType(typeof(ProductDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDetailDto>> GetProductBySlug(
        string slug,
        CancellationToken cancellationToken)
    {
        var product = await _productService.GetProductBySlugAsync(slug, cancellationToken);
        if (product == null)
        {
            return NotFound(new { message = $"Product with slug '{slug}' was not found." });
        }

        return Ok(product);
    }

    /// <summary>
    /// Search products using natural language semantic search powered by AI embeddings.
    /// Returns the most semantically relevant products for the given query.
    /// </summary>
    [HttpGet("search/semantic")]
    [ProducesResponseType(typeof(List<ProductListDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ProductListDto>>> SemanticSearch(
        [FromQuery] string query,
        [FromQuery] int topK = 10,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return BadRequest(new { message = "Query cannot be empty." });

        if (topK < 1 || topK > 50)
            topK = 10;

        var results = await _embeddingService.SemanticSearchAsync(query, topK, cancellationToken);
        return Ok(results);
    }
}
