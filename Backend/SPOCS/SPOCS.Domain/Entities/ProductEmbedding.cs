using Pgvector;

namespace SPOCS.Domain.Entities;

/// <summary>
/// Stores the pgvector embedding for a product, used for semantic search.
/// One-to-one with Product.
/// </summary>
public class ProductEmbedding
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;

    /// <summary>
    /// The pgvector Vector column (1536 dims for text-embedding-3-small).
    /// </summary>
    public Vector PgVector { get; set; } = new Vector(Array.Empty<float>());

    /// <summary>Helper to get/set the underlying float array.</summary>
    public float[] Vector
    {
        get => PgVector.ToArray();
        set => PgVector = new Vector(value);
    }

    /// <summary>
    /// The text that was embedded — used for auditing and detecting when re-generation is needed.
    /// </summary>
    public string EmbeddingText { get; set; } = string.Empty;

    /// <summary>
    /// The embedding model used (e.g. "text-embedding-3-small").
    /// </summary>
    public string ModelName { get; set; } = string.Empty;

    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}
