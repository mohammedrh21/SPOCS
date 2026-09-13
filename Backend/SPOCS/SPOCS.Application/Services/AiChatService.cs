using System.Text.Json;
using Microsoft.Extensions.Logging;
using SPOCS.Application.AI.Prompts;
using SPOCS.Application.AI.Tools;
using SPOCS.Application.Contracts.AI;
using SPOCS.Application.DTOs.AI;

namespace SPOCS.Application.Services;

/// <summary>
/// Autonomous AI Shopping Agent orchestrating tool calling, structured filtering, and RAG.
/// </summary>
public class AiChatService : IAiChatService
{
    private readonly ILLMService _llmService;
    private readonly IShoppingToolService _toolService;
    private readonly ILogger<AiChatService> _logger;

    public AiChatService(
        ILLMService llmService,
        IShoppingToolService toolService,
        ILogger<AiChatService> logger)
    {
        _llmService = llmService;
        _toolService = toolService;
        _logger = logger;
    }

    public async Task<ChatResponseDto> ChatAsync(ChatRequestDto request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            throw new ArgumentException("Message cannot be empty.", nameof(request.Message));
        }

        _logger.LogInformation("Agent received customer chat query: '{Query}'", request.Message);

        var referencedProducts = new Dictionary<Guid, ProductReferenceDto>();

        // 1. Build Agent System Prompt with tool instructions
        var systemInstruction = SystemPrompts.ShoppingAssistant + "\n\n" +
            "AGENT AUTONOMY & TOOL INSTRUCTIONS:\n" +
            "1. You are equipped with real-time catalog tools: 'SearchProducts', 'GetProductDetails', and 'FindSimilarProducts'.\n" +
            "2. Whenever a customer asks for product recommendations, prices, technical specifications, or comparisons, call the appropriate tool to retrieve grounded catalog data before answering.\n" +
            "3. When a customer specifies budget or price criteria (e.g., 'under $1000', 'between $300 and $700'), supply the numeric 'maxPrice' and/or 'minPrice' parameters to SearchProducts.\n" +
            "4. If a tool returns 'filtered_out', explain to the customer that matching items were found but exceeded their budget/filters, citing the closest options.\n" +
            "5. If a customer query is purely conversational (e.g., 'Hello', 'What can you do?'), answer directly without calling tools.\n" +
            "6. Always ground final answers strictly in the tool results. Do not invent products or specs.";

        var messages = new List<ChatMessageDto>
        {
            new()
            {
                Role = "system",
                Content = systemInstruction
            }
        };

        // Add few-shot examples
        messages.AddRange(FewShotExamples.Examples);

        // Add prior conversation history if provided (take at most last 6 for relevance)
        if (request.History != null && request.History.Any())
        {
            var recentHistory = request.History
                .Where(m => m.Role is "user" or "assistant")
                .TakeLast(6);

            messages.AddRange(recentHistory);
        }

        // Add the current user query
        messages.Add(new ChatMessageDto
        {
            Role = "user",
            Content = request.Message
        });

        // 2. Agentic loop: execute tool calls iteratively until final text is synthesized
        int iteration = 0;
        const int maxIterations = 4;
        string finalReply = string.Empty;

        while (iteration < maxIterations)
        {
            iteration++;
            _logger.LogDebug("Agent loop iteration {Iteration} of {MaxIterations}", iteration, maxIterations);

            var llmResponse = await _llmService.GenerateChatCompletionAsync(
                messages,
                tools: AgentToolDefinitions.Tools,
                temperature: 0.2f,
                cancellationToken: cancellationToken);

            if (llmResponse.HasToolCalls)
            {
                _logger.LogInformation("Agent requested {Count} tool call(s): {Tools}",
                    llmResponse.ToolCalls.Count,
                    string.Join(", ", llmResponse.ToolCalls.Select(t => t.Function.Name)));

                // Add assistant message containing the tool calls
                messages.Add(new ChatMessageDto
                {
                    Role = "assistant",
                    Content = llmResponse.Content,
                    ToolCalls = llmResponse.ToolCalls
                });

                // Execute each tool call requested by the agent
                foreach (var toolCall in llmResponse.ToolCalls)
                {
                    var toolResult = await ExecuteToolAsync(toolCall, cancellationToken);

                    // Track referenced products for client display
                    foreach (var prod in toolResult.Products)
                    {
                        referencedProducts[prod.Id] = prod;
                    }

                    // Append tool result message
                    messages.Add(new ChatMessageDto
                    {
                        Role = "tool",
                        ToolCallId = toolCall.Id,
                        Name = toolCall.Function.Name,
                        Content = toolResult.ResultJson
                    });
                }
            }
            else
            {
                // Final answer synthesized by the agent
                finalReply = llmResponse.Content ?? string.Empty;
                break;
            }
        }

        // Fallback if loop ended without final text
        if (string.IsNullOrWhiteSpace(finalReply))
        {
            finalReply = await _llmService.GenerateCompletionAsync(messages, temperature: 0.2f, cancellationToken);
        }

        return new ChatResponseDto
        {
            Reply = finalReply,
            ReferencedProducts = referencedProducts.Values.ToList(),
            Model = _llmService.ModelName
        };
    }

    private async Task<ToolExecutionResult> ExecuteToolAsync(ToolCallDto toolCall, CancellationToken cancellationToken)
    {
        try
        {
            using var doc = JsonDocument.Parse(
                string.IsNullOrWhiteSpace(toolCall.Function.Arguments) ? "{}" : toolCall.Function.Arguments);
            var root = doc.RootElement;

            switch (toolCall.Function.Name)
            {
                case "SearchProducts":
                {
                    string query = root.TryGetProperty("query", out var q) ? q.GetString() ?? "" : "";
                    decimal? minPrice = root.TryGetProperty("minPrice", out var minP) && minP.TryGetDecimal(out var minVal) ? minVal : null;
                    decimal? maxPrice = root.TryGetProperty("maxPrice", out var maxP) && maxP.TryGetDecimal(out var maxVal) ? maxVal : null;
                    string? category = root.TryGetProperty("categorySlug", out var cat) ? cat.GetString() : null;
                    bool? inStock = root.TryGetProperty("inStockOnly", out var st) ? st.GetBoolean() : null;

                    return await _toolService.SearchProductsAsync(
                        query: query,
                        minPrice: minPrice,
                        maxPrice: maxPrice,
                        categorySlug: category,
                        inStockOnly: inStock,
                        topK: 5,
                        cancellationToken: cancellationToken);
                }

                case "GetProductDetails":
                {
                    Guid? prodId = root.TryGetProperty("productId", out var pid) && Guid.TryParse(pid.GetString(), out var gid) ? gid : null;
                    string? slug = root.TryGetProperty("slug", out var s) ? s.GetString() : null;

                    return await _toolService.GetProductDetailsAsync(prodId, slug, cancellationToken);
                }

                case "FindSimilarProducts":
                {
                    Guid targetId = root.TryGetProperty("productId", out var tid) && Guid.TryParse(tid.GetString(), out var tgid) ? tgid : Guid.Empty;
                    int topK = root.TryGetProperty("topK", out var tk) && tk.TryGetInt32(out var kval) ? kval : 3;

                    return await _toolService.FindSimilarProductsAsync(targetId, topK, cancellationToken);
                }

                default:
                    _logger.LogWarning("Unknown tool name requested by LLM: {ToolName}", toolCall.Function.Name);
                    return new ToolExecutionResult
                    {
                        ResultJson = JsonSerializer.Serialize(new { error = $"Unknown tool '{toolCall.Function.Name}'." })
                    };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing tool {ToolName} with arguments {Arguments}",
                toolCall.Function.Name, toolCall.Function.Arguments);

            return new ToolExecutionResult
            {
                ResultJson = JsonSerializer.Serialize(new { error = "Failed to execute tool.", details = ex.Message })
            };
        }
    }
}
