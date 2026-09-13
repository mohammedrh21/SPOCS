using Microsoft.Extensions.Logging;
using SPOCS.Application.AI;
using SPOCS.Application.Contracts.AI;
using SPOCS.Application.Contracts.Persistence;
using SPOCS.Application.DTOs;
using SPOCS.Application.Mappings;
using SPOCS.Domain.Entities;

namespace SPOCS.Application.Services;

public class ProductEmbeddingService : IProductEmbeddingService
{
    private readonly IEmbeddingService _embeddingService;
    private readonly IProductEmbeddingRepository _embeddingRepository;
    private readonly IProductRepository _productRepository;
    private readonly ILogger<ProductEmbeddingService> _logger;

    public ProductEmbeddingService(
        IEmbeddingService embeddingService,
        IProductEmbeddingRepository embeddingRepository,
        IProductRepository productRepository,
        ILogger<ProductEmbeddingService> logger)
    {
        _embeddingService = embeddingService;
        _embeddingRepository = embeddingRepository;
        _productRepository = productRepository;
        _logger = logger;
    }

    public async Task GenerateAndStoreEmbeddingAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        var product = await _productRepository.GetByIdAsync(productId, cancellationToken)
            ?? throw new InvalidOperationException($"Product '{productId}' not found.");

        var text = ProductTextBuilder.Build(product);
        var vector = await _embeddingService.GenerateEmbeddingAsync(text, cancellationToken);

        var embedding = new ProductEmbedding
        {
            ProductId = productId,
            Vector = vector,
            EmbeddingText = text,
            ModelName = $"text-embedding-3-small (dim={_embeddingService.Dimensions})",
            GeneratedAt = DateTime.UtcNow
        };

        await _embeddingRepository.UpsertEmbeddingAsync(embedding, cancellationToken);
        _logger.LogInformation("Embedding generated for product {ProductId} ({ProductName})", productId, product.Name);
    }

    public async Task GenerateAllEmbeddingsAsync(CancellationToken cancellationToken = default)
    {
        // Get all products
        var products = await _productRepository.GetAllAsync(cancellationToken);

        var embeddedIds = await _embeddingRepository.GetAllEmbeddedProductIdsAsync(cancellationToken);
        var embeddedSet = new HashSet<Guid>(embeddedIds);

        int generated = 0;
        int skipped = 0;

        foreach (var product in products)
        {
            if (cancellationToken.IsCancellationRequested) break;

            if (embeddedSet.Contains(product.Id))
            {
                skipped++;
                continue;
            }

            try
            {
                await GenerateAndStoreEmbeddingAsync(product.Id, cancellationToken);
                generated++;

                // Small delay to respect API rate limits
                await Task.Delay(200, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to generate embedding for product {ProductId}", product.Id);
            }
        }

        _logger.LogInformation(
            "Embedding generation complete. Generated: {Generated}, Skipped (already embedded): {Skipped}",
            generated, skipped);
    }

    public async Task<List<ProductListDto>> SemanticSearchAsync(string query, int topK = 10, CancellationToken cancellationToken = default)
    {
        // Generate embedding for the query
        var queryVector = await _embeddingService.GenerateEmbeddingAsync(query, cancellationToken);

        // Find most similar products by vector cosine distance
        var productIds = await _embeddingRepository.SemanticSearchAsync(queryVector, topK, cancellationToken);

        if (!productIds.Any())
            return new List<ProductListDto>();

        // Fetch the actual product data (preserving ranking order)
        var results = new List<ProductListDto>();
        foreach (var id in productIds)
        {
            var product = await _productRepository.GetByIdAsync(id, cancellationToken);
            if (product != null)
                results.Add(product.ToListDto());
        }

        return results;
    }
}
