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
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<AiController> _logger;

    public AiController(IGroqService groqService, ICurrentUser currentUser, ILogger<AiController> logger)
    {
        _groqService = groqService;
        _currentUser = currentUser;
        _logger = logger;
    }

    /// <summary>
    /// Generate AI response for a custom prompt
    /// </summary>
    [HttpPost("prompt")]
    public async Task<ActionResult<AiResponseDto>> GenerateResponse([FromBody] AiPromptRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Prompt))
            {
                return BadRequest(new { message = "Prompt cannot be empty" });
            }

            var (response, promptTokens, completionTokens) = await _groqService.GenerateResponseWithTokensAsync(request.Prompt, request.SystemPrompt);

            var result = new AiResponseDto
            {
                Response = response,
                PromptTokens = promptTokens,
                CompletionTokens = completionTokens,
                GeneratedAt = DateTime.UtcNow
            };

            _logger.LogInformation($"AI response generated for user {_currentUser.GetCurrentUserId()}");
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error generating AI response: {ex.Message}");
            return StatusCode(500, new { message = "Error generating response", error = ex.Message });
        }
    }

    /// <summary>
    /// Generate lesson explanation using AI
    /// </summary>
    [HttpPost("lesson-explanation")]
    public async Task<ActionResult<LessonExplanationResponseDto>> GenerateLessonExplanation([FromBody] LessonExplanationRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.LessonTitle) || string.IsNullOrWhiteSpace(request.LessonContent))
            {
                return BadRequest(new { message = "Lesson title and content are required" });
            }

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
                KeyPoints = ExtractKeyPoints(response),
                SummaryForStudents = ExtractSummary(response),
                CommonMisunderstandings = ExtractMisunderstandings(response),
                GeneratedAt = DateTime.UtcNow
            };

            _logger.LogInformation($"Lesson explanation generated for user {_currentUser.GetCurrentUserId()}");
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error generating lesson explanation: {ex.Message}");
            return StatusCode(500, new { message = "Error generating lesson explanation", error = ex.Message });
        }
    }

    /// <summary>
    /// Generate quiz questions using AI
    /// </summary>
    [HttpPost("generate-quiz")]
    public async Task<ActionResult<QuizGenerationResponseDto>> GenerateQuiz([FromBody] QuizGeneratorRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Topic) || request.NumberOfQuestions <= 0)
            {
                return BadRequest(new { message = "Valid topic and number of questions required" });
            }

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

            // Parse JSON response
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
                                _logger.LogWarning($"Error parsing quiz question: {qEx.Message}");
                            }
                        }
                    }
                }
            }
            catch (Exception parseEx)
            {
                _logger.LogWarning($"Could not parse quiz JSON: {parseEx.Message}");
            }

            _logger.LogInformation($"Quiz generated for user {_currentUser.GetCurrentUserId()}");
            return Ok(quizResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error generating quiz: {ex.Message}");
            return StatusCode(500, new { message = "Error generating quiz", error = ex.Message });
        }
    }

    /// <summary>
    /// Get homework assistance
    /// </summary>
    [HttpPost("homework-assist")]
    public async Task<ActionResult<HomeworkAssistantResponseDto>> GetHomeworkAssistance([FromBody] HomeworkAssistantRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Question))
            {
                return BadRequest(new { message = "Question cannot be empty" });
            }

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
                StepByStepExplanation = ExtractSteps(response),
                Hint = ExtractHint(response),
                RelatedConcepts = ExtractConcepts(response),
                GeneratedAt = DateTime.UtcNow
            };

            _logger.LogInformation($"Homework assistance provided to user {_currentUser.GetCurrentUserId()}");
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error providing homework assistance: {ex.Message}");
            return StatusCode(500, new { message = "Error providing assistance", error = ex.Message });
        }
    }

    /// <summary>
    /// Review code using AI
    /// </summary>
    [HttpPost("code-review")]
    public async Task<ActionResult<CodeReviewResponseDto>> ReviewCode([FromBody] CodeReviewRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Code))
            {
                return BadRequest(new { message = "Code cannot be empty" });
            }

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
                Issues = ExtractCodeIssues(response),
                Suggestions = ExtractSuggestions(response),
                BestPractices = ExtractBestPractices(response),
                ImprovedCode = ExtractImprovedCode(response),
                GeneratedAt = DateTime.UtcNow
            };

            _logger.LogInformation($"Code review completed for user {_currentUser.GetCurrentUserId()}");
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error reviewing code: {ex.Message}");
            return StatusCode(500, new { message = "Error reviewing code", error = ex.Message });
        }
    }

    #region Helper Methods

    private List<string> ExtractKeyPoints(string text)
    {
        var lines = text.Split(new[] { "\n", "\r\n" }, StringSplitOptions.None);
        return lines.Where(l => l.Contains("-") || l.Contains("•") || l.Contains("*"))
            .Select(l => l.Trim(' ', '-', '•', '*').Trim())
            .Where(l => !string.IsNullOrEmpty(l))
            .Take(5)
            .ToList();
    }

    private string ExtractSummary(string text)
    {
        var lines = text.Split(new[] { "\n", "\r\n" }, StringSplitOptions.None);
        var summaryLines = lines.Skip(1).Take(3).ToList();
        return string.Join(" ", summaryLines).Trim();
    }

    private string ExtractMisunderstandings(string text)
    {
        return text.Contains("misconception") || text.Contains("misunderstanding")
            ? text.Substring(0, Math.Min(500, text.Length))
            : "None identified";
    }

    private string ExtractSteps(string text)
    {
        return text.Contains("step") ? text : "Follow the solution above carefully";
    }

    private string ExtractHint(string text)
    {
        return "Consider breaking down the problem into smaller parts";
    }

    private List<string> ExtractConcepts(string text)
    {
        return new List<string> { "Review related topics in your course materials" };
    }

    private List<string> ExtractCodeIssues(string text)
    {
        var issues = new List<string>();
        if (text.Contains("bug") || text.Contains("error")) issues.Add("Potential bugs found");
        if (text.Contains("performance")) issues.Add("Performance concerns");
        if (text.Contains("null")) issues.Add("Null reference handling");
        return issues;
    }

    private List<string> ExtractSuggestions(string text)
    {
        return new List<string>
        {
            "Add more comments to explain complex logic",
            "Consider using more descriptive variable names",
            "Add error handling"
        };
    }

    private List<string> ExtractBestPractices(string text)
    {
        return new List<string>
        {
            "Follow SOLID principles",
            "Write unit tests",
            "Use consistent naming conventions"
        };
    }

    private string ExtractImprovedCode(string text)
    {
        var codeStart = text.IndexOf("```");
        if (codeStart >= 0)
        {
            var codeEnd = text.IndexOf("```", codeStart + 3);
            if (codeEnd > codeStart)
            {
                return text.Substring(codeStart + 3, codeEnd - codeStart - 3).Trim();
            }
        }
        return "See review above for improvements";
    }

    #endregion
}

