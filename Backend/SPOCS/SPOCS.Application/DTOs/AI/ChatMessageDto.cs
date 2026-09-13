namespace SPOCS.Application.DTOs.AI;

public class ChatMessageDto
{
    /// <summary>
    /// Role of the message sender: "system", "user", "assistant", or "tool".
    /// </summary>
    public string Role { get; set; } = "user";

    /// <summary>
    /// The text content of the message.
    /// </summary>
    public string? Content { get; set; } = string.Empty;

    /// <summary>
    /// Optional tool calls initiated by the assistant.
    /// </summary>
    public List<ToolCallDto>? ToolCalls { get; set; }

    /// <summary>
    /// Tool call ID matching the assistant's request (used when Role == "tool").
    /// </summary>
    public string? ToolCallId { get; set; }

    /// <summary>
    /// Optional name of the tool or author.
    /// </summary>
    public string? Name { get; set; }
}
