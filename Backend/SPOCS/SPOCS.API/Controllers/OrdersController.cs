using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SPOCS.Application.Contracts.Services;
using SPOCS.Application.DTOs.Orders;

namespace SPOCS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(IOrderService orderService, ILogger<OrdersController> logger)
    {
        _orderService = orderService;
        _logger = logger;
    }

    private Guid GetCurrentUserId()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("User identity not found.");
        return Guid.Parse(userId);
    }

    /// <summary>
    /// Create a new order from the current cart.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<OrderDto>> CreateOrder([FromBody] CreateOrderDto request, CancellationToken cancellationToken)
    {
        var customerId = GetCurrentUserId();
        var order = await _orderService.CreateOrderFromCartAsync(customerId, request, cancellationToken);
        return CreatedAtAction(nameof(GetOrderById), new { id = order.Id }, order);
    }

    /// <summary>
    /// Get all orders for the current customer.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<OrderSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<OrderSummaryDto>>> GetMyOrders(CancellationToken cancellationToken)
    {
        var customerId = GetCurrentUserId();
        var orders = await _orderService.GetMyOrdersAsync(customerId, cancellationToken);
        return Ok(orders);
    }

    /// <summary>
    /// Get details of a specific order by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderDto>> GetOrderById(Guid id, CancellationToken cancellationToken)
    {
        var customerId = GetCurrentUserId();
        var order = await _orderService.GetOrderByIdAsync(id, customerId, cancellationToken);

        if (order == null)
        {
            return NotFound(new { message = $"Order '{id}' not found." });
        }

        return Ok(order);
    }
}
