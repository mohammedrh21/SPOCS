using System.Text.Json.Serialization;

namespace SPOCS.Application.DTOs.AI;

/// <summary>
/// Tool definition provided to the LLM (OpenAI / OpenRouter function calling format).
/// </summary>
public class ToolDefinitionDto
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "function";

    [JsonPropertyName("function")]
    public FunctionDefinitionDto Function { get; set; } = new();
}

public class FunctionDefinitionDto
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("parameters")]
    public object Parameters { get; set; } = new();
}

/// <summary>
/// Represents a tool call requested by the LLM.
/// </summary>
public class ToolCallDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = "function";

    [JsonPropertyName("function")]
    public FunctionCallDto Function { get; set; } = new();
}

public class FunctionCallDto
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("arguments")]
    public string Arguments { get; set; } = string.Empty;
}

/// <summary>
/// Response from the LLM supporting both standard text content and tool calls.
/// </summary>
public class LlmChatResponseDto
{
    public string? Content { get; set; }
    public List<ToolCallDto> ToolCalls { get; set; } = new();
    public bool HasToolCalls => ToolCalls != null && ToolCalls.Count > 0;
}
