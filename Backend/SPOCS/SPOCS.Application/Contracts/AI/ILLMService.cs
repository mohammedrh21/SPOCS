using SPOCS.Application.DTOs.AI;

namespace SPOCS.Application.Contracts.AI;

/// <summary>
/// Core abstraction for interacting with an LLM provider (OpenAI, OpenRouter, etc.).
/// </summary>
public interface ILLMService
{
    /// <summary>
    /// Model name configured for chat completions.
    /// </summary>
    string ModelName { get; }

    /// <summary>
    /// Generates a text completion given a collection of chat messages.
    /// </summary>
    Task<string> GenerateCompletionAsync(
        IEnumerable<ChatMessageDto> messages,
        float temperature = 0.2f,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a chat completion with optional tool definitions, supporting function calls.
    /// </summary>
    Task<LlmChatResponseDto> GenerateChatCompletionAsync(
        IEnumerable<ChatMessageDto> messages,
        IEnumerable<ToolDefinitionDto>? tools = null,
        float temperature = 0.2f,
        CancellationToken cancellationToken = default);
}
