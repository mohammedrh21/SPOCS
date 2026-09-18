using SPOCS.Domain.Entities;

namespace SPOCS.Application.Contracts.AI;

/// <summary>
/// Repository for storing and querying product embeddings via pgvector.
/// </summary>
public interface IProductEmbeddingRepository
{
    Task UpsertEmbeddingAsync(ProductEmbedding embedding, CancellationToken cancellationToken = default);
    Task<List<Guid>> SemanticSearchAsync(float[] queryVector, int topK, CancellationToken cancellationToken = default);
    Task<List<Guid>> SemanticSearchAsync(float[] queryVector, int topK, double? maxDistance, CancellationToken cancellationToken = default);
    Task<ProductEmbedding?> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken = default);
    Task<List<Guid>> GetAllEmbeddedProductIdsAsync(CancellationToken cancellationToken = default);
}
