namespace SPOCS.Domain.Entities;

public class VariantType
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty; // e.g. "RAM", "Storage", "Color"
    public string? DisplayName { get; set; }

    public ICollection<VariantOption> Options { get; set; } = new List<VariantOption>();
}
