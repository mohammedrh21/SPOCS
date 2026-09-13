namespace SPOCS.Domain.Entities;

public class ProductVariantOption
{
    public Guid ProductVariantId { get; set; }
    public ProductVariant ProductVariant { get; set; } = null!;

    public Guid VariantOptionId { get; set; }
    public VariantOption VariantOption { get; set; } = null!;
}
