using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SPOCS.Application.Contracts.AI;

namespace SPOCS.Infrastructure.AI;

/// <summary>
/// Calls OpenAI's Embeddings API to generate text-embedding-3-small vectors (1536 dims).
/// </summary>
public class OpenAIEmbeddingService : IEmbeddingService
{
    private readonly HttpClient _httpClient;
    private readonly string _model;
    private readonly string? _apiKey;
    private readonly ILogger<OpenAIEmbeddingService> _logger;

    public int Dimensions => 1536;

    public OpenAIEmbeddingService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<OpenAIEmbeddingService> logger)
    {
        _httpClient = httpClientFactory.CreateClient("OpenAI");
        _model = configuration["AI:OpenAI:EmbeddingModel"] ?? "text-embedding-3-small";
        _logger = logger;
        _apiKey = configuration["AI:OpenAI:ApiKey"];

        var baseUrl = configuration["AI:OpenAI:BaseUrl"];
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
        if (!string.IsNullOrWhiteSpace(_apiKey) && _apiKey != "YOUR_OPENAI_API_KEY")
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _apiKey);

            // OpenRouter attribution headers
            if (baseUrl.Contains("openrouter.ai", StringComparison.OrdinalIgnoreCase))
            {
                _httpClient.DefaultRequestHeaders.Remove("HTTP-Referer");
                _httpClient.DefaultRequestHeaders.Add("HTTP-Referer", "https://localhost:7157");
                _httpClient.DefaultRequestHeaders.Remove("X-Title");
                _httpClient.DefaultRequestHeaders.Add("X-Title", "SPOCS");
            }
        }
    }

    public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_apiKey) || _apiKey == "YOUR_OPENAI_API_KEY")
        {
            throw new InvalidOperationException("API key is not configured. Please set 'AI:OpenAI:ApiKey' in appsettings.json or user secrets.");
        }

        var requestBody = new
        {
            input = text,
            model = _model
        };

        var json = JsonSerializer.Serialize(requestBody);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        _logger.LogDebug("Requesting embedding from API for text length {Length}", text.Length);

        var response = await _httpClient.PostAsync("v1/embeddings", content, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Embedding API call failed with status {StatusCode}: {ErrorBody}", response.StatusCode, errorBody);
            response.EnsureSuccessStatusCode();
        }

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(responseJson);

        var embeddingArray = doc.RootElement
            .GetProperty("data")[0]
            .GetProperty("embedding");

        return embeddingArray.EnumerateArray()
            .Select(e => e.GetSingle())
            .ToArray();
    }
}
