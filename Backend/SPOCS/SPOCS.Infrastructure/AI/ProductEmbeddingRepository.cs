using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;
using SPOCS.Application.Contracts.AI;
using SPOCS.Domain.Entities;
using SPOCS.Infrastructure.Data;

namespace SPOCS.Infrastructure.AI;

public class ProductEmbeddingRepository : IProductEmbeddingRepository
{
    private readonly ApplicationDbContext _context;

    public ProductEmbeddingRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task UpsertEmbeddingAsync(ProductEmbedding embedding, CancellationToken cancellationToken = default)
    {
        var existing = await _context.ProductEmbeddings
            .FirstOrDefaultAsync(e => e.ProductId == embedding.ProductId, cancellationToken);

        if (existing != null)
        {
            existing.Vector = embedding.Vector;
            existing.EmbeddingText = embedding.EmbeddingText;
            existing.ModelName = embedding.ModelName;
            existing.GeneratedAt = embedding.GeneratedAt;
        }
        else
        {
            await _context.ProductEmbeddings.AddAsync(embedding, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<Guid>> SemanticSearchAsync(float[] queryVector, int topK, CancellationToken cancellationToken = default)
    {
        var pgVector = new Vector(queryVector);

        // Use pgvector cosine distance operator (<=>), ordered ascending (closer = lower distance)
        var results = await _context.ProductEmbeddings
            .OrderBy(e => e.PgVector.CosineDistance(pgVector))
            .Take(topK)
            .Select(e => e.ProductId)
            .ToListAsync(cancellationToken);

        return results;
    }

    public async Task<ProductEmbedding?> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        return await _context.ProductEmbeddings
            .FirstOrDefaultAsync(e => e.ProductId == productId, cancellationToken);
    }

    public async Task<List<Guid>> GetAllEmbeddedProductIdsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.ProductEmbeddings
            .Select(e => e.ProductId)
            .ToListAsync(cancellationToken);
    }
}
