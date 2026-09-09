using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudyCourseAPI.DTOs.Requests.Admin;
using StudyCourseAPI.Models;
using StudyCourseAPI.Services;

namespace StudyCourseAPI.Controllers
{
    [Route("api/Courses/{courseId}/[controller]")]
    [Authorize]
    public class ChaptersController : ControllerBase
    {
        private readonly IChapterService _chapterService;

        public ChaptersController(IChapterService chapterService)
        {
            _chapterService = chapterService;
        }

        // ─────────────────────────────────────────────────────────
        // GET — list all chapters of a course, ordered by OrderIndex
        // ─────────────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Get(long courseId)
        {
            var chapters = await _chapterService.GetByCourseAsync(courseId);

            if (chapters == null) return NotFound();
            return Ok(chapters);
        }

        // ─────────────────────────────────────────────────────────
        // GET single
        // ─────────────────────────────────────────────────────────
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(long courseId, long id)
        {
            var chapter = await _chapterService.GetByIdAsync(courseId, id);

            if (chapter == null) return NotFound();
            return Ok(chapter);
        }

        // ─────────────────────────────────────────────────────────
        // POST — create a new chapter for the course
        // ─────────────────────────────────────────────────────────
        [Authorize(Roles = AppRoles.Admin)]
        [HttpPost]
        public async Task<IActionResult> Post(long courseId, [FromBody] ChapterRequest model)
        {
            var result = await _chapterService.CreateAsync(courseId, model);

            if (result.ErrorMessage != null)
                return BadRequest(new { status = 400, message = result.ErrorMessage });
            if (result.IsNotFound) return NotFound();

            return CreatedAtAction(nameof(Get), new { courseId, id = result.Data!.Id }, result.Data);
        }

        // ─────────────────────────────────────────────────────────
        // PUT {id} — update
        // ─────────────────────────────────────────────────────────
        [Authorize(Roles = AppRoles.Admin)]
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(long courseId, long id, [FromBody] ChapterRequest model)
        {
            var result = await _chapterService.UpdateAsync(courseId, id, model);

            if (result.ErrorMessage != null)
                return BadRequest(new { status = 400, message = result.ErrorMessage });
            if (result.IsNotFound) return NotFound();

            return Ok(result.Data);
        }

        // ─────────────────────────────────────────────────────────
        // DELETE {id} — soft delete
        // ─────────────────────────────────────────────────────────
        [Authorize(Roles = AppRoles.Admin)]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long courseId, long id)
        {
            var deleted = await _chapterService.SoftDeleteAsync(courseId, id);

            if (!deleted) return NotFound();
            return Ok(new { success = true });
        }

        // ─────────────────────────────────────────────────────────
        // PUT /disable — bulk
        // ─────────────────────────────────────────────────────────
        [Authorize(Roles = AppRoles.Admin)]
        [HttpPut("disable")]
        public async Task<IActionResult> Disable(long courseId, [FromBody] List<long> ids)
        {
            var affected = await _chapterService.SetActiveAsync(courseId, ids, isActive: false);

            return affected == 0 ? NotFound() : NoContent();
        }

        // ─────────────────────────────────────────────────────────
        // PUT /enable — bulk
        // ─────────────────────────────────────────────────────────
        [Authorize(Roles = AppRoles.Admin)]
        [HttpPut("enable")]
        public async Task<IActionResult> Enable(long courseId, [FromBody] List<long> ids)
        {
            var affected = await _chapterService.SetActiveAsync(courseId, ids, isActive: true);

            return affected == 0 ? NotFound() : NoContent();
        }
    }
}
