using System.ComponentModel.DataAnnotations;

namespace SPOCS.Application.DTOs.AI;

public class ChatRequestDto
{
    /// <summary>
    /// The customer's current question or prompt.
    /// </summary>
    [Required(ErrorMessage = "Message is required.")]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Optional conversation history for multi-turn conversations.
    /// </summary>
    public List<ChatMessageDto>? History { get; set; }

    /// <summary>
    /// Maximum number of products to retrieve for context (default 5, max 10).
    /// </summary>
    public int TopK { get; set; } = 5;
}
