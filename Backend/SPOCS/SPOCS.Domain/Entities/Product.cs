namespace SPOCS.Domain.Entities;

public class Product
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ShortDescription { get; set; }
    public decimal BasePrice { get; set; }
    public string Sku { get; set; } = string.Empty;
    public int StockQuantity { get; set; } = 0;

    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    // Features and specifications are essential for AI semantic search representation & RAG grounding
    public List<string> Features { get; set; } = new();
    public Dictionary<string, string> Specifications { get; set; } = new();

    public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
    public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
