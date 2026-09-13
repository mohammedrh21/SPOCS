using SPOCS.Application.DTOs;

namespace SPOCS.Application.Contracts.AI;

/// <summary>
/// Orchestrates product embedding generation, storage, and semantic search.
/// </summary>
public interface IProductEmbeddingService
{
    /// <summary>Generates and stores the embedding for a single product.</summary>
    Task GenerateAndStoreEmbeddingAsync(Guid productId, CancellationToken cancellationToken = default);

    /// <summary>Generates and stores embeddings for all products that are missing or outdated.</summary>
    Task GenerateAllEmbeddingsAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns products ranked by semantic similarity to the query.</summary>
    Task<List<ProductListDto>> SemanticSearchAsync(string query, int topK = 10, CancellationToken cancellationToken = default);
}
