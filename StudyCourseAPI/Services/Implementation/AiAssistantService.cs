using System.Text.Json;
using StudyCourseAPI.DTOs.Requests;
using StudyCourseAPI.DTOs.Responses;
using StudyCourseAPI.Repositories;

namespace StudyCourseAPI.Services;

public class AiAssistantService : IAiAssistantService
{
    private readonly IGroqService _groqService;
    private readonly IAiResponseParser _parser;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<AiAssistantService> _logger;

    public AiAssistantService(
        IGroqService groqService,
        IAiResponseParser parser,
        ICurrentUser currentUser,
        ILogger<AiAssistantService> logger)
    {
        _groqService = groqService;
        _parser = parser;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<AiResponseDto> GenerateAsync(AiPromptRequest request)
    {
        var (response, promptTokens, completionTokens) =
            await _groqService.GenerateResponseWithTokensAsync(request.Prompt, request.SystemPrompt);

        _logger.LogInformation("AI response generated for user {UserId}", _currentUser.GetCurrentUserId());

        return new AiResponseDto
        {
            Response = response,
            PromptTokens = promptTokens,
            CompletionTokens = completionTokens,
            GeneratedAt = DateTime.UtcNow
        };
    }

    public async Task<LessonExplanationResponseDto> ExplainLessonAsync(LessonExplanationRequest request)
    {
        var prompt = $@"
Please provide a clear explanation for the following lesson:

Lesson Title: {request.LessonTitle}
Lesson Content: {request.LessonContent}
{(string.IsNullOrEmpty(request.AdditionalContext) ? "" : $"Additional Context: {request.AdditionalContext}")}

Please provide:
1. A comprehensive explanation
2. Key points (as a list)
3. A summary for students
4. Common misunderstandings
";

        var response = await _groqService.GenerateResponseAsync(
            prompt,
            "You are an expert educator. Provide clear, easy-to-understand explanations for students.");

        _logger.LogInformation("Lesson explanation generated for user {UserId}", _currentUser.GetCurrentUserId());

        return new LessonExplanationResponseDto
        {
            Explanation = response,
            KeyPoints = _parser.ExtractKeyPoints(response),
            SummaryForStudents = _parser.ExtractSummary(response),
            CommonMisunderstandings = _parser.ExtractMisunderstandings(response),
            GeneratedAt = DateTime.UtcNow
        };
    }

    public async Task<QuizGenerationResponseDto> GenerateQuizAsync(QuizGeneratorRequest request)
    {
        var difficultyLevel = request.Difficulty?.ToLower() ?? "medium";

        var prompt = $@"
Generate {request.NumberOfQuestions} multiple-choice questions about {request.Topic} at {difficultyLevel} difficulty level.

For each question, provide:
1. The question
2. Four possible answers (A, B, C, D)
3. The correct answer
4. A brief explanation

Format as JSON with this structure:
{{
  ""questions"": [
    {{
      ""question"": ""..."",
      ""options"": [""..."", ""..."", ""..."", ""...""],
      ""correct_answer"": ""..."",
      ""explanation"": ""...""
    }}
  ]
}}
";

        var response = await _groqService.GenerateResponseAsync(
            prompt,
            "You are an expert quiz generator. Create clear, fair quiz questions.");

        var quizResponse = new QuizGenerationResponseDto
        {
            Topic = request.Topic,
            Difficulty = difficultyLevel switch
            {
                "easy" => 1,
                "medium" => 2,
                "hard" => 3,
                _ => 2
            },
            GeneratedAt = DateTime.UtcNow
        };

        ParseQuizQuestions(response, quizResponse);

        _logger.LogInformation("Quiz generated for user {UserId}", _currentUser.GetCurrentUserId());

        return quizResponse;
    }

    public async Task<HomeworkAssistantResponseDto> AssistHomeworkAsync(HomeworkAssistantRequest request)
    {
        var prompt = $@"
Help me with this question:
{request.Question}

{(string.IsNullOrEmpty(request.CourseContext) ? "" : $"Course Context: {request.CourseContext}")}

Please provide:
1. A solution
2. Step-by-step explanation
3. A helpful hint for understanding
4. Related concepts to study
";

        var response = await _groqService.GenerateResponseAsync(
            prompt,
            "You are a helpful tutor. Guide students toward understanding without just giving answers.");

        _logger.LogInformation("Homework assistance provided to user {UserId}", _currentUser.GetCurrentUserId());

        return new HomeworkAssistantResponseDto
        {
            Solution = response,
            StepByStepExplanation = _parser.ExtractSteps(response),
            Hint = _parser.ExtractHint(response),
            RelatedConcepts = _parser.ExtractConcepts(response),
            GeneratedAt = DateTime.UtcNow
        };
    }

    public async Task<CodeReviewResponseDto> ReviewCodeAsync(CodeReviewRequest request)
    {
        var prompt = $@"
Please review the following {request.Language} code:

```{request.Language}
{request.Code}
```

Provide:
1. Overall assessment
2. Issues or bugs
3. Suggestions for improvement
4. Best practices to follow
5. Improved version of the code
";

        var response = await _groqService.GenerateResponseAsync(
            prompt,
            "You are an expert code reviewer. Provide constructive feedback and improvements.");

        _logger.LogInformation("Code review completed for user {UserId}", _currentUser.GetCurrentUserId());

        return new CodeReviewResponseDto
        {
            OverallAssessment = response,
            Issues = _parser.ExtractCodeIssues(response),
            Suggestions = _parser.ExtractSuggestions(response),
            BestPractices = _parser.ExtractBestPractices(response),
            ImprovedCode = _parser.ExtractImprovedCode(response),
            GeneratedAt = DateTime.UtcNow
        };
    }

    // ─────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────

    /// <summary>
    /// Model không phải lúc nào cũng trả JSON sạch — chấp nhận output méo/thiếu thay vì
    /// làm hỏng cả request chỉ vì một câu hỏi lỗi.
    /// </summary>
    private void ParseQuizQuestions(string response, QuizGenerationResponseDto target)
    {
        try
        {
            var jsonStart = response.IndexOf("{");
            var jsonEnd = response.LastIndexOf("}");
            if (jsonStart < 0 || jsonEnd <= jsonStart) return;

            var jsonStr = response.Substring(jsonStart, jsonEnd - jsonStart + 1);
            using var doc = JsonDocument.Parse(jsonStr);

            if (!doc.RootElement.TryGetProperty("questions", out var questionsElement)) return;

            var questionNumber = 1;
            foreach (var q in questionsElement.EnumerateArray())
            {
                try
                {
                    target.Questions.Add(new QuizQuestionDto
                    {
                        Number = questionNumber++,
                        Question = q.GetProperty("question").GetString() ?? "",
                        Options = q.GetProperty("options").EnumerateArray()
                            .Select(o => o.GetString() ?? "").ToList(),
                        CorrectAnswer = q.GetProperty("correct_answer").GetString() ?? "",
                        Explanation = q.TryGetProperty("explanation", out var exp) ? exp.GetString() : null
                    });
                }
                catch (Exception qEx)
                {
                    _logger.LogWarning(qEx, "Error parsing quiz question");
                }
            }
        }
        catch (Exception parseEx)
        {
            _logger.LogWarning(parseEx, "Could not parse quiz JSON");
        }
    }
}
