using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SPOCS.Application.Contracts.AI;
using SPOCS.Application.DTOs.AI;

namespace SPOCS.Infrastructure.AI;

/// <summary>
/// Interacts with OpenAI / OpenRouter Chat Completions endpoint.
/// </summary>
public class OpenAiLlmService : ILLMService
{
    private readonly HttpClient _httpClient;
    private readonly string _model;
    private readonly string? _apiKey;
    private readonly ILogger<OpenAiLlmService> _logger;

    public string ModelName => _model;

    public OpenAiLlmService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<OpenAiLlmService> logger)
    {
        _httpClient = httpClientFactory.CreateClient("OpenAI");
        _model = configuration["AI:OpenAI:ChatModel"] ?? "openai/gpt-4o-mini";
        _apiKey = configuration["AI:OpenAI:ApiKey"]?.Trim().Trim('"');
        _logger = logger;

        var baseUrl = configuration["AI:OpenAI:BaseUrl"]?.Trim().Trim('"');
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            baseUrl = _apiKey?.StartsWith("sk-or-v1-", StringComparison.OrdinalIgnoreCase) == true
                ? "https://openrouter.ai/api/"
                : "https://api.openai.com/";
        }

        if (!baseUrl.EndsWith("/"))
        {
            baseUrl += "/";
        }

        _httpClient.BaseAddress = new Uri(baseUrl);

        if (!string.IsNullOrWhiteSpace(_apiKey) && _apiKey != "YOUR_OPENAI_API_KEY" && _apiKey != "your_api_key_here")
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _apiKey);

            if (baseUrl.Contains("openrouter.ai", StringComparison.OrdinalIgnoreCase))
            {
                _httpClient.DefaultRequestHeaders.Remove("HTTP-Referer");
                _httpClient.DefaultRequestHeaders.Add("HTTP-Referer", "https://spocs.netlify.app");
                _httpClient.DefaultRequestHeaders.Remove("X-Title");
                _httpClient.DefaultRequestHeaders.Add("X-Title", "SPOCS");
            }
        }
    }

    public async Task<string> GenerateCompletionAsync(
        IEnumerable<ChatMessageDto> messages,
        float temperature = 0.2f,
        CancellationToken cancellationToken = default)
    {
        var response = await GenerateChatCompletionAsync(messages, tools: null, temperature, cancellationToken);
        return response.Content ?? string.Empty;
    }

    public async Task<LlmChatResponseDto> GenerateChatCompletionAsync(
        IEnumerable<ChatMessageDto> messages,
        IEnumerable<ToolDefinitionDto>? tools = null,
        float temperature = 0.2f,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_apiKey) || _apiKey == "YOUR_OPENAI_API_KEY")
        {
            throw new InvalidOperationException("API key is not configured. Please set 'AI:OpenAI:ApiKey' in appsettings.json or user secrets.");
        }

        // Format messages for OpenAI / OpenRouter schema (handling tool calls & tool responses)
        var formattedMessages = messages.Select<ChatMessageDto, object>(m =>
        {
            if (m.Role == "tool")
            {
                return new
                {
                    role = "tool",
                    tool_call_id = m.ToolCallId,
                    content = m.Content
                };
            }

            if (m.Role == "assistant" && m.ToolCalls != null && m.ToolCalls.Any())
            {
                return new
                {
                    role = "assistant",
                    content = m.Content,
                    tool_calls = m.ToolCalls.Select(tc => new
                    {
                        id = tc.Id,
                        type = tc.Type,
                        function = new
                        {
                            name = tc.Function.Name,
                            arguments = tc.Function.Arguments
                        }
                    }).ToArray()
                };
            }

            return new
            {
                role = m.Role,
                content = m.Content
            };
        }).ToArray();

        object requestBody;
        var toolList = tools?.ToList();
        if (toolList != null && toolList.Any())
        {
            requestBody = new
            {
                model = _model,
                temperature = temperature,
                messages = formattedMessages,
                tools = toolList.Select(t => new
                {
                    type = t.Type,
                    function = new
                    {
                        name = t.Function.Name,
                        description = t.Function.Description,
                        parameters = t.Function.Parameters
                    }
                }).ToArray()
            };
        }
        else
        {
            requestBody = new
            {
                model = _model,
                temperature = temperature,
                messages = formattedMessages
            };
        }

        var json = JsonSerializer.Serialize(requestBody);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        _logger.LogDebug("Sending completion request to model {Model} (hasTools: {HasTools})", _model, toolList?.Any() ?? false);

        var response = await _httpClient.PostAsync("v1/chat/completions", content, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Chat completion API failed with status {StatusCode}: {ErrorBody}", response.StatusCode, errorBody);
            throw new HttpRequestException($"AI Provider error ({response.StatusCode}): {errorBody}");
        }

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(responseJson);

        var messageEl = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message");

        string? replyContent = null;
        if (messageEl.TryGetProperty("content", out var contentProp) && contentProp.ValueKind == JsonValueKind.String)
        {
            replyContent = contentProp.GetString();
        }

        var result = new LlmChatResponseDto
        {
            Content = replyContent
        };

        if (messageEl.TryGetProperty("tool_calls", out var toolCallsProp) && toolCallsProp.ValueKind == JsonValueKind.Array)
        {
            foreach (var callEl in toolCallsProp.EnumerateArray())
            {
                var id = callEl.GetProperty("id").GetString() ?? string.Empty;
                var type = callEl.TryGetProperty("type", out var typeProp) ? typeProp.GetString() ?? "function" : "function";
                var funcEl = callEl.GetProperty("function");
                var name = funcEl.GetProperty("name").GetString() ?? string.Empty;
                var args = funcEl.GetProperty("arguments").GetString() ?? string.Empty;

                result.ToolCalls.Add(new ToolCallDto
                {
                    Id = id,
                    Type = type,
                    Function = new FunctionCallDto
                    {
                        Name = name,
                        Arguments = args
                    }
                });
            }
        }

        return result;
    }
}
