using SPOCS.Application.DTOs.AI;

namespace SPOCS.Application.Contracts.AI;

/// <summary>
/// High-level shopping assistant service orchestrating RAG, few-shot prompting, and grounding.
/// </summary>
public interface IAiChatService
{
    /// <summary>
    /// Processes a customer chat query, retrieves relevant catalog products, and returns a grounded response.
    /// </summary>
    Task<ChatResponseDto> ChatAsync(ChatRequestDto request, CancellationToken cancellationToken = default);
}
