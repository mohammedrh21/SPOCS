namespace SPOCS.Application.DTOs.AI;

public class ChatResponseDto
{
    /// <summary>
    /// The AI assistant's text reply.
    /// </summary>
    public string Reply { get; set; } = string.Empty;

    /// <summary>
    /// Products retrieved from the catalog that were provided to the AI as context.
    /// </summary>
    public List<ProductReferenceDto> ReferencedProducts { get; set; } = new();

    /// <summary>
    /// The model that generated the response.
    /// </summary>
    public string Model { get; set; } = string.Empty;
}
