using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudyCourseAPI.DTOs.Requests;
using StudyCourseAPI.Extensions;
using StudyCourseAPI.Services;

namespace StudyCourseAPI.Controllers
{
    [Route("api/lessons/{lessonId:long}/comments")]
    [ApiController]
    [Authorize]
    public class CommentsController : ControllerBase
    {
        private readonly ICommentService _commentService;

        public CommentsController(ICommentService commentService)
        {
            _commentService = commentService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(long lessonId)
        {
            return Ok(await _commentService.GetByLessonAsync(lessonId));
        }

        [HttpPost]
        public async Task<IActionResult> Post(long lessonId, [FromBody] CommentRequest model)
        {
            var result = await _commentService.CreateAsync(lessonId, model);

            if (result.IsNotFound) return NotFound(new { message = result.NotFoundMessage });
            if (!result.IsSuccess) return this.ValidationFailed(result.Errors);

            return CreatedAtAction(nameof(GetAll), new { lessonId }, result.Data);
        }

        [HttpDelete("{commentId:long}")]
        public async Task<IActionResult> Delete(long lessonId, long commentId)
        {
            var deleted = await _commentService.SoftDeleteAsync(lessonId, commentId);

            if (!deleted) return NotFound();
            return Ok(new { success = true });
        }

        [HttpPost("{commentId:long}/like")]
        public async Task<IActionResult> ToggleLike(long lessonId, long commentId)
        {
            var result = await _commentService.ToggleLikeAsync(lessonId, commentId);

            if (result == null) return NotFound();
            return Ok(new { liked = result.Value.Liked, likeCount = result.Value.LikeCount });
        }
    }
}
