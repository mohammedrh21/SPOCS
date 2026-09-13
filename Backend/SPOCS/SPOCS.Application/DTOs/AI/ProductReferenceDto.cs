namespace SPOCS.Application.DTOs.AI;

/// <summary>
/// Structured reference to a catalog product cited in an AI response.
/// </summary>
public class ProductReferenceDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public decimal BasePrice { get; set; }
    public string? PrimaryImageUrl { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public bool InStock { get; set; }
}
