namespace SPOCS.Domain.Entities;

public class Cart
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// The owning customer. Non-nullable — every cart must belong to a customer.
    /// The DB enforces a unique constraint on this column, ensuring one cart per customer.
    /// </summary>
    public Guid CustomerId { get; set; }
    public ApplicationUser? Customer { get; set; }

    public ICollection<CartItem> Items { get; set; } = new List<CartItem>();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
