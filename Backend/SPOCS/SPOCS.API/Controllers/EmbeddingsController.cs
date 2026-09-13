using Microsoft.AspNetCore.Mvc;
using SPOCS.Application.Contracts.AI;

namespace SPOCS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class EmbeddingsController : ControllerBase
{
    private readonly IProductEmbeddingService _embeddingService;
    private readonly ILogger<EmbeddingsController> _logger;

    public EmbeddingsController(IProductEmbeddingService embeddingService, ILogger<EmbeddingsController> logger)
    {
        _embeddingService = embeddingService;
        _logger = logger;
    }

    /// <summary>
    /// Generate or refresh embeddings for all products that do not yet have one.
    /// </summary>
    [HttpPost("generate-all")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GenerateAll(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Embedding generation triggered via API.");
        await _embeddingService.GenerateAllEmbeddingsAsync(cancellationToken);
        return Ok(new { message = "Product embedding generation completed." });
    }

    /// <summary>
    /// Generate or refresh the embedding for a single product.
    /// </summary>
    [HttpPost("product/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GenerateForProduct(Guid id, CancellationToken cancellationToken)
    {
        await _embeddingService.GenerateAndStoreEmbeddingAsync(id, cancellationToken);
        return Ok(new { message = $"Embedding generated for product {id}." });
    }
}
