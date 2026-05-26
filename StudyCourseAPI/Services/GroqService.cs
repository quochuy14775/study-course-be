using System.Net.Http.Json;
using System.Text.Json;

namespace StudyCourseAPI.Services;

public class GroqService : IGroqService
{
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _model;
    private const string BaseUrl = "https://api.groq.com/openai/v1";

    public GroqService(IConfiguration configuration, HttpClient httpClient)
    {
        _configuration = configuration;
        _httpClient = httpClient;
        _apiKey = _configuration["Groq:ApiKey"] ?? "";
        _model = _configuration["Groq:Model"] ?? "llama-3.3-70b-versatile";

        if (string.IsNullOrEmpty(_apiKey))
            throw new InvalidOperationException("Groq API Key is not configured");

        if (!_httpClient.DefaultRequestHeaders.Contains("Authorization"))
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
    }

    public async Task<string> GenerateResponseAsync(string prompt)
        => await GenerateResponseAsync(prompt, null);

    public async Task<string> GenerateResponseAsync(string prompt, string? systemPrompt)
    {
        var (response, _, _) = await GenerateResponseWithTokensAsync(prompt, systemPrompt);
        return response;
    }

    public async Task<(string response, int promptTokens, int completionTokens)> GenerateResponseWithTokensAsync(
        string prompt, string? systemPrompt = null)
    {
        var messages = new List<object>();

        if (!string.IsNullOrEmpty(systemPrompt))
            messages.Add(new { role = "system", content = systemPrompt });

        messages.Add(new { role = "user", content = prompt });

        var requestBody = new
        {
            model = _model,
            messages,
            max_tokens = 2000,
            temperature = 0.7f
        };

        var httpResponse = await _httpClient.PostAsJsonAsync($"{BaseUrl}/chat/completions", requestBody);

        if (!httpResponse.IsSuccessStatusCode)
        {
            var errorBody = await httpResponse.Content.ReadAsStringAsync();
            throw new InvalidOperationException(
                $"Groq API error {(int)httpResponse.StatusCode}: {errorBody}");
        }

        var jsonContent = await httpResponse.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(jsonContent);
        var root = doc.RootElement;

        var content = "No response";
        var promptTokens = 0;
        var completionTokens = 0;

        if (root.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
        {
            var first = choices[0];
            if (first.TryGetProperty("message", out var msg) &&
                msg.TryGetProperty("content", out var text))
            {
                content = text.GetString() ?? "No response";
            }
        }

        if (root.TryGetProperty("usage", out var usage))
        {
            if (usage.TryGetProperty("prompt_tokens", out var pt)) promptTokens = pt.GetInt32();
            if (usage.TryGetProperty("completion_tokens", out var ct)) completionTokens = ct.GetInt32();
        }

        return (content, promptTokens, completionTokens);
    }
}
