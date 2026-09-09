using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudyCourseAPI.DTOs.Requests;
using StudyCourseAPI.Extensions;
using StudyCourseAPI.Services;

namespace StudyCourseAPI.Controllers
{
    [Route("api/lessons/{lessonId:long}/questions")]
    [ApiController]
    [Authorize]
    public class QuestionsController : ControllerBase
    {
        private readonly IQuestionService _questionService;

        public QuestionsController(IQuestionService questionService)
        {
            _questionService = questionService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(long lessonId)
        {
            return Ok(await _questionService.GetByLessonAsync(lessonId));
        }

        [HttpPost]
        public async Task<IActionResult> Post(long lessonId, [FromBody] QuestionRequest model)
        {
            var result = await _questionService.CreateAsync(lessonId, model);

            if (result.IsNotFound) return NotFound(new { message = result.NotFoundMessage });
            if (!result.IsSuccess) return this.ValidationFailed(result.Errors);

            return CreatedAtAction(nameof(GetAll), new { lessonId }, result.Data);
        }

        [HttpDelete("{questionId:long}")]
        public async Task<IActionResult> Delete(long lessonId, long questionId)
        {
            var deleted = await _questionService.SoftDeleteAsync(lessonId, questionId);

            if (!deleted) return NotFound();
            return Ok(new { success = true });
        }

        [HttpPost("{questionId:long}/resolve")]
        public async Task<IActionResult> Resolve(long lessonId, long questionId)
        {
            var isResolved = await _questionService.ToggleResolvedAsync(lessonId, questionId);

            if (isResolved == null) return NotFound();
            return Ok(new { isResolved = isResolved.Value });
        }

        [HttpPost("{questionId:long}/answers")]
        public async Task<IActionResult> AddAnswer(long lessonId, long questionId, [FromBody] AnswerRequest model)
        {
            var result = await _questionService.AddAnswerAsync(lessonId, questionId, model);

            if (result.IsNotFound) return NotFound();
            if (!result.IsSuccess) return this.ValidationFailed(result.Errors);

            return CreatedAtAction(nameof(GetAll), new { lessonId }, result.Data);
        }
    }

    [Route("api/answers")]
    [ApiController]
    [Authorize]
    public class AnswersController : ControllerBase
    {
        private readonly IAnswerService _answerService;

        public AnswersController(IAnswerService answerService)
        {
            _answerService = answerService;
        }

        [HttpPost("{answerId:long}/like")]
        public async Task<IActionResult> ToggleLike(long answerId)
        {
            var result = await _answerService.ToggleLikeAsync(answerId);

            if (result == null) return NotFound();
            return Ok(new { liked = result.Value.Liked, likeCount = result.Value.LikeCount });
        }

        [HttpPost("{answerId:long}/accept")]
        public async Task<IActionResult> Accept(long answerId)
        {
            var result = await _answerService.ToggleAcceptedAsync(answerId);

            if (result.IsNotFound) return NotFound();
            if (result.IsForbidden) return Forbid();

            return Ok(new { isAccepted = result.Data });
        }

        [HttpDelete("{answerId:long}")]
        public async Task<IActionResult> Delete(long answerId)
        {
            var deleted = await _answerService.SoftDeleteAsync(answerId);

            if (!deleted) return NotFound();
            return Ok(new { success = true });
        }
    }
}
