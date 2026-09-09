using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudyCourseAPI.DTOs.Requests;
using StudyCourseAPI.Extensions;
using StudyCourseAPI.Services;

namespace StudyCourseAPI.Controllers
{
    [Route("api/lessons/{lessonId:long}/notes")]
    [ApiController]
    [Authorize]
    public class NotesController : ControllerBase
    {
        private readonly INoteService _noteService;

        public NotesController(INoteService noteService)
        {
            _noteService = noteService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(long lessonId)
        {
            return Ok(await _noteService.GetByLessonAsync(lessonId));
        }

        [HttpPost]
        public async Task<IActionResult> Post(long lessonId, [FromBody] NoteRequest model)
        {
            var result = await _noteService.CreateAsync(lessonId, model);

            if (result.IsNotFound) return NotFound(new { message = result.NotFoundMessage });
            if (!result.IsSuccess) return this.ValidationFailed(result.Errors);

            return CreatedAtAction(nameof(GetAll), new { lessonId }, result.Data);
        }

        [HttpPut("{noteId:long}")]
        public async Task<IActionResult> Put(long lessonId, long noteId, [FromBody] NoteRequest model)
        {
            var result = await _noteService.UpdateAsync(lessonId, noteId, model);

            if (result.IsNotFound) return NotFound();
            if (!result.IsSuccess) return this.ValidationFailed(result.Errors);

            return Ok(result.Data);
        }

        [HttpDelete("{noteId:long}")]
        public async Task<IActionResult> Delete(long lessonId, long noteId)
        {
            var deleted = await _noteService.SoftDeleteAsync(lessonId, noteId);

            if (!deleted) return NotFound();
            return Ok(new { success = true });
        }
    }
}
