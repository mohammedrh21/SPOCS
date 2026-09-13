using System.ComponentModel.DataAnnotations;

namespace SPOCS.Application.DTOs.Cart;

public class UpdateCartItemDto
{
    [Range(0, 100)]
    public int Quantity { get; set; }
}
