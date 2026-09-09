namespace StudyCourseAPI.Services;

public interface IGroqService
{
    Task<string> GenerateResponseAsync(string prompt);
    Task<string> GenerateResponseAsync(string prompt, string? systemPrompt);
    Task<(string response, int promptTokens, int completionTokens)> GenerateResponseWithTokensAsync(string prompt, string? systemPrompt = null);
}

