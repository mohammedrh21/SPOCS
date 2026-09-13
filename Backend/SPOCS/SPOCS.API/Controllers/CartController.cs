using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SPOCS.Application.Contracts.Services;
using SPOCS.Application.DTOs.Cart;

namespace SPOCS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize]
public class CartController : ControllerBase
{
    private readonly ICartService _cartService;
    private readonly ILogger<CartController> _logger;

    public CartController(ICartService cartService, ILogger<CartController> logger)
    {
        _cartService = cartService;
        _logger = logger;
    }

    private Guid GetCurrentUserId()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("User identity not found.");
        return Guid.Parse(userId);
    }

    /// <summary>
    /// Get the current customer's cart.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(CartDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CartDto>> GetCart(CancellationToken cancellationToken)
    {
        var customerId = GetCurrentUserId();
        var cart = await _cartService.GetCartAsync(customerId, cancellationToken);

        // Return empty cart structure if no cart exists yet
        if (cart == null)
        {
            return Ok(new CartDto { CustomerId = customerId });
        }

        return Ok(cart);
    }

    /// <summary>
    /// Add a product (or variant) to the cart.
    /// </summary>
    [HttpPost("items")]
    [ProducesResponseType(typeof(CartDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CartDto>> AddToCart([FromBody] AddToCartDto request, CancellationToken cancellationToken)
    {
        var customerId = GetCurrentUserId();
        var cart = await _cartService.AddToCartAsync(customerId, request, cancellationToken);
        return Ok(cart);
    }

    /// <summary>
    /// Update the quantity of a cart item. Set quantity to 0 to remove the item.
    /// </summary>
    [HttpPut("items/{itemId:guid}")]
    [ProducesResponseType(typeof(CartDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CartDto>> UpdateCartItem(
        Guid itemId,
        [FromBody] UpdateCartItemDto request,
        CancellationToken cancellationToken)
    {
        var customerId = GetCurrentUserId();
        var cart = await _cartService.UpdateCartItemAsync(customerId, itemId, request, cancellationToken);
        return Ok(cart);
    }

    /// <summary>
    /// Remove a specific item from the cart.
    /// </summary>
    [HttpDelete("items/{itemId:guid}")]
    [ProducesResponseType(typeof(CartDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CartDto>> RemoveFromCart(Guid itemId, CancellationToken cancellationToken)
    {
        var customerId = GetCurrentUserId();
        var cart = await _cartService.RemoveFromCartAsync(customerId, itemId, cancellationToken);
        return Ok(cart);
    }

    /// <summary>
    /// Clear all items from the cart.
    /// </summary>
    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ClearCart(CancellationToken cancellationToken)
    {
        var customerId = GetCurrentUserId();
        await _cartService.ClearCartAsync(customerId, cancellationToken);
        return NoContent();
    }
}
