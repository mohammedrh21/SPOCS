using Microsoft.AspNetCore.Mvc;
using SPOCS.Application.Contracts.AI;
using SPOCS.Application.DTOs.AI;

namespace SPOCS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ChatController : ControllerBase
{
    private readonly IAiChatService _chatService;
    private readonly ILogger<ChatController> _logger;

    public ChatController(IAiChatService chatService, ILogger<ChatController> logger)
    {
        _chatService = chatService;
        _logger = logger;
    }

    /// <summary>
    /// Ask the AI Shopping Assistant a question about products, recommendations, or comparisons.
    /// Responses are grounded strictly in the retrieved product catalog.
    /// </summary>
    /// <param name="request">Customer message, optional conversation history, and retrieval count.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Grounded AI reply along with the referenced catalog products.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ChatResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ChatResponseDto>> Chat(
        [FromBody] ChatRequestDto request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest(new { message = "Message cannot be empty." });
        }

        _logger.LogInformation("Received customer chat request: '{Query}'", request.Message);

        var response = await _chatService.ChatAsync(request, cancellationToken);
        return Ok(response);
    }
}
