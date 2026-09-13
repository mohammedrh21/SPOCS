using System.ComponentModel.DataAnnotations;

namespace SPOCS.Application.DTOs.Orders;

public class CreateOrderDto
{
    [Required, MaxLength(500)]
    public string ShippingAddress { get; set; } = string.Empty;

    [Required, MaxLength(150)]
    public string City { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? PostalCode { get; set; }

    [Required, MaxLength(100)]
    public string Country { get; set; } = string.Empty;
}
