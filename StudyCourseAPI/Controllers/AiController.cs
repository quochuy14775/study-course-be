using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudyCourseAPI.DTOs.Requests;
using StudyCourseAPI.DTOs.Responses;
using StudyCourseAPI.Services;
using StudyCourseAPI.Repositories;
using System.Text.Json;

namespace StudyCourseAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AiController : ControllerBase
{
    private readonly IGroqService _groqService;
    private readonly IAiResponseParser _parser;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<AiController> _logger;

    public AiController(
        IGroqService groqService,
        IAiResponseParser parser,
        ICurrentUser currentUser,
        ILogger<AiController> logger)
    {
        _groqService = groqService;
        _parser = parser;
        _currentUser = currentUser;
        _logger = logger;
    }

    /// <summary>
    /// Generate AI response for a custom prompt
    /// </summary>
    [HttpPost("prompt")]
    public async Task<ActionResult<AiResponseDto>> GenerateResponse([FromBody] AiPromptRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Prompt))
            return BadRequest(new { status = 400, message = "Prompt cannot be empty" });

        var (response, promptTokens, completionTokens) = await _groqService.GenerateResponseWithTokensAsync(request.Prompt, request.SystemPrompt);

        var result = new AiResponseDto
        {
            Response = response,
            PromptTokens = promptTokens,
            CompletionTokens = completionTokens,
            GeneratedAt = DateTime.UtcNow
        };

        _logger.LogInformation("AI response generated for user {UserId}", _currentUser.GetCurrentUserId());
        return Ok(result);
    }

    /// <summary>
    /// Generate lesson explanation using AI
    /// </summary>
    [HttpPost("lesson-explanation")]
    public async Task<ActionResult<LessonExplanationResponseDto>> GenerateLessonExplanation([FromBody] LessonExplanationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.LessonTitle) || string.IsNullOrWhiteSpace(request.LessonContent))
            return BadRequest(new { status = 400, message = "Lesson title and content are required" });

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

        var response = await _groqService.GenerateResponseAsync(prompt, "You are an expert educator. Provide clear, easy-to-understand explanations for students.");

        var result = new LessonExplanationResponseDto
        {
            Explanation = response,
            KeyPoints = _parser.ExtractKeyPoints(response),
            SummaryForStudents = _parser.ExtractSummary(response),
            CommonMisunderstandings = _parser.ExtractMisunderstandings(response),
            GeneratedAt = DateTime.UtcNow
        };

        _logger.LogInformation("Lesson explanation generated for user {UserId}", _currentUser.GetCurrentUserId());
        return Ok(result);
    }

    /// <summary>
    /// Generate quiz questions using AI
    /// </summary>
    [HttpPost("generate-quiz")]
    public async Task<ActionResult<QuizGenerationResponseDto>> GenerateQuiz([FromBody] QuizGeneratorRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Topic) || request.NumberOfQuestions <= 0)
            return BadRequest(new { status = 400, message = "Valid topic and number of questions required" });

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

        var response = await _groqService.GenerateResponseAsync(prompt, "You are an expert quiz generator. Create clear, fair quiz questions.");

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

        // The model doesn't always return clean JSON — tolerate malformed/partial output
        // rather than failing the whole request over one bad question.
        try
        {
            var jsonStart = response.IndexOf("{");
            var jsonEnd = response.LastIndexOf("}");
            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var jsonStr = response.Substring(jsonStart, jsonEnd - jsonStart + 1);
                using var doc = JsonDocument.Parse(jsonStr);
                var root = doc.RootElement;

                if (root.TryGetProperty("questions", out var questionsElement))
                {
                    int questionNumber = 1;
                    foreach (var q in questionsElement.EnumerateArray())
                    {
                        try
                        {
                            var question = new QuizQuestionDto
                            {
                                Number = questionNumber++,
                                Question = q.GetProperty("question").GetString() ?? "",
                                Options = q.GetProperty("options").EnumerateArray()
                                    .Select(o => o.GetString() ?? "").ToList(),
                                CorrectAnswer = q.GetProperty("correct_answer").GetString() ?? "",
                                Explanation = q.TryGetProperty("explanation", out var exp) ? exp.GetString() : null
                            };
                            quizResponse.Questions.Add(question);
                        }
                        catch (Exception qEx)
                        {
                            _logger.LogWarning(qEx, "Error parsing quiz question");
                        }
                    }
                }
            }
        }
        catch (Exception parseEx)
        {
            _logger.LogWarning(parseEx, "Could not parse quiz JSON");
        }

        _logger.LogInformation("Quiz generated for user {UserId}", _currentUser.GetCurrentUserId());
        return Ok(quizResponse);
    }

    /// <summary>
    /// Get homework assistance
    /// </summary>
    [HttpPost("homework-assist")]
    public async Task<ActionResult<HomeworkAssistantResponseDto>> GetHomeworkAssistance([FromBody] HomeworkAssistantRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Question))
            return BadRequest(new { status = 400, message = "Question cannot be empty" });

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

        var response = await _groqService.GenerateResponseAsync(prompt, "You are a helpful tutor. Guide students toward understanding without just giving answers.");

        var result = new HomeworkAssistantResponseDto
        {
            Solution = response,
            StepByStepExplanation = _parser.ExtractSteps(response),
            Hint = _parser.ExtractHint(response),
            RelatedConcepts = _parser.ExtractConcepts(response),
            GeneratedAt = DateTime.UtcNow
        };

        _logger.LogInformation("Homework assistance provided to user {UserId}", _currentUser.GetCurrentUserId());
        return Ok(result);
    }

    /// <summary>
    /// Review code using AI
    /// </summary>
    [HttpPost("code-review")]
    public async Task<ActionResult<CodeReviewResponseDto>> ReviewCode([FromBody] CodeReviewRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
            return BadRequest(new { status = 400, message = "Code cannot be empty" });

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

        var response = await _groqService.GenerateResponseAsync(prompt, "You are an expert code reviewer. Provide constructive feedback and improvements.");

        var result = new CodeReviewResponseDto
        {
            OverallAssessment = response,
            Issues = _parser.ExtractCodeIssues(response),
            Suggestions = _parser.ExtractSuggestions(response),
            BestPractices = _parser.ExtractBestPractices(response),
            ImprovedCode = _parser.ExtractImprovedCode(response),
            GeneratedAt = DateTime.UtcNow
        };

        _logger.LogInformation("Code review completed for user {UserId}", _currentUser.GetCurrentUserId());
        return Ok(result);
    }
}
