using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudyCourseAPI.DTOs.Requests;
using StudyCourseAPI.DTOs.Responses;
using StudyCourseAPI.Services;

namespace StudyCourseAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AiController : ControllerBase
{
    private readonly IAiAssistantService _aiService;

    public AiController(IAiAssistantService aiService)
    {
        _aiService = aiService;
    }

    /// <summary>
    /// Generate AI response for a custom prompt
    /// </summary>
    [HttpPost("prompt")]
    public async Task<ActionResult<AiResponseDto>> GenerateResponse([FromBody] AiPromptRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Prompt))
            return BadRequest(new { status = 400, message = "Prompt cannot be empty" });

        return Ok(await _aiService.GenerateAsync(request));
    }

    /// <summary>
    /// Generate lesson explanation using AI
    /// </summary>
    [HttpPost("lesson-explanation")]
    public async Task<ActionResult<LessonExplanationResponseDto>> GenerateLessonExplanation(
        [FromBody] LessonExplanationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.LessonTitle) || string.IsNullOrWhiteSpace(request.LessonContent))
            return BadRequest(new { status = 400, message = "Lesson title and content are required" });

        return Ok(await _aiService.ExplainLessonAsync(request));
    }

    /// <summary>
    /// Generate quiz questions using AI
    /// </summary>
    [HttpPost("generate-quiz")]
    public async Task<ActionResult<QuizGenerationResponseDto>> GenerateQuiz([FromBody] QuizGeneratorRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Topic) || request.NumberOfQuestions <= 0)
            return BadRequest(new { status = 400, message = "Valid topic and number of questions required" });

        return Ok(await _aiService.GenerateQuizAsync(request));
    }

    /// <summary>
    /// Get homework assistance
    /// </summary>
    [HttpPost("homework-assist")]
    public async Task<ActionResult<HomeworkAssistantResponseDto>> GetHomeworkAssistance(
        [FromBody] HomeworkAssistantRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Question))
            return BadRequest(new { status = 400, message = "Question cannot be empty" });

        return Ok(await _aiService.AssistHomeworkAsync(request));
    }

    /// <summary>
    /// Review code using AI
    /// </summary>
    [HttpPost("code-review")]
    public async Task<ActionResult<CodeReviewResponseDto>> ReviewCode([FromBody] CodeReviewRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
            return BadRequest(new { status = 400, message = "Code cannot be empty" });

        return Ok(await _aiService.ReviewCodeAsync(request));
    }
}
