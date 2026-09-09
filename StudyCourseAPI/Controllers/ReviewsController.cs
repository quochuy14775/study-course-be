using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudyCourseAPI.DTOs.Requests;
using StudyCourseAPI.Extensions;
using StudyCourseAPI.Services;

namespace StudyCourseAPI.Controllers
{
    [Route("api/courses/{courseId:long}/reviews")]
    [ApiController]
    [Authorize]
    public class ReviewsController : ControllerBase
    {
        private readonly IReviewService _reviewService;

        public ReviewsController(IReviewService reviewService)
        {
            _reviewService = reviewService;
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetAll(long courseId)
        {
            return Ok(await _reviewService.GetByCourseAsync(courseId));
        }

        [AllowAnonymous]
        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary(long courseId)
        {
            return Ok(await _reviewService.GetSummaryAsync(courseId));
        }

        [HttpPost]
        public async Task<IActionResult> Post(long courseId, [FromBody] ReviewRequest model)
        {
            var result = await _reviewService.CreateAsync(courseId, model);

            if (result.IsNotFound) return NotFound(new { message = result.NotFoundMessage });
            if (!result.IsSuccess) return this.ValidationFailed(result.Errors);

            return CreatedAtAction(nameof(GetAll), new { courseId }, result.Data);
        }

        [HttpDelete("{reviewId:long}")]
        public async Task<IActionResult> Delete(long courseId, long reviewId)
        {
            var deleted = await _reviewService.SoftDeleteAsync(courseId, reviewId);

            if (!deleted) return NotFound();
            return Ok(new { success = true });
        }

        [HttpPost("{reviewId:long}/helpful")]
        public async Task<IActionResult> ToggleHelpful(long courseId, long reviewId)
        {
            var result = await _reviewService.ToggleHelpfulAsync(courseId, reviewId);

            if (result == null) return NotFound();
            return Ok(new { markedHelpful = result.Value.MarkedHelpful, helpfulCount = result.Value.HelpfulCount });
        }

        [HttpPost("{reviewId:long}/replies")]
        public async Task<IActionResult> AddReply(long courseId, long reviewId, [FromBody] ReviewReplyRequest model)
        {
            var result = await _reviewService.AddReplyAsync(courseId, reviewId, model);

            if (result.IsNotFound) return NotFound();
            if (!result.IsSuccess) return this.ValidationFailed(result.Errors);

            return CreatedAtAction(nameof(GetAll), new { courseId }, result.Data);
        }
    }
}
