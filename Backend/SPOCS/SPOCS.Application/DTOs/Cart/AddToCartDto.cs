using System.ComponentModel.DataAnnotations;

namespace SPOCS.Application.DTOs.Cart;

public class AddToCartDto
{
    [Required]
    public Guid ProductId { get; set; }

    public Guid? ProductVariantId { get; set; }

    [Range(1, 100)]
    public int Quantity { get; set; } = 1;
}
