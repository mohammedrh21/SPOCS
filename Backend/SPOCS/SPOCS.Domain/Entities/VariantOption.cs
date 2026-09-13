namespace SPOCS.Domain.Entities;

public class VariantOption
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid VariantTypeId { get; set; }
    public VariantType VariantType { get; set; } = null!;

    public string Value { get; set; } = string.Empty; // e.g. "16GB", "512GB SSD", "Space Gray"

    public ICollection<ProductVariantOption> ProductVariantOptions { get; set; } = new List<ProductVariantOption>();
}
