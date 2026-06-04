using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudyCourseAPI.DTOs.Requests;
using StudyCourseAPI.DTOs.Responses;
using StudyCourseAPI.Extensions;
using StudyCourseAPI.Models;
using StudyCourseAPI.Repositories;

namespace StudyCourseAPI.Controllers.UserController
{
    [Route("api/lessons/{lessonId:long}/notes")]
    [ApiController]
    [Authorize]
    public class NotesController : BaseController<LessonNote>
    {
        private readonly IRepository<Lesson> _lessonRepository;

        public NotesController(
            IRepository<LessonNote> baseRepository,
            IRepository<Lesson> lessonRepository,
            ICurrentUser currentUser)
            : base(baseRepository, currentUser)
        {
            _lessonRepository = lessonRepository;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(long lessonId)
        {
            var userId = _currentUser.GetCurrentUserId();

            var notes = await _baseRepository.Query()
                .AsNoTracking()
                .Where(n => n.LessonId == lessonId && n.UserId == userId && !n.IsDeleted)
                .OrderBy(n => n.VideoTimestamp)
                .Select(n => new NoteResponse(n))
                .ToListAsync();

            return Ok(notes);
        }

        [HttpPost]
        public async Task<IActionResult> Post(long lessonId, [FromBody] NoteRequest model)
        {
            var lessonExists = await _lessonRepository.Query()
                .AnyAsync(l => l.Id == lessonId && !l.IsDeleted);
            if (!lessonExists)
                return NotFound(new { message = "Lesson not found." });

            var (success, errors) = model.ValidateNote();
            if (!success)
                return BadRequest(new { status = 400, message = "Validation failed", errors = FlattenErrors(errors) });

            var userId = _currentUser.GetCurrentUserId();
            var entity = model.GetNote(lessonId, userId);

            _baseRepository.Add(entity);
            await _baseRepository.SaveChangesAsync();

            return CreatedAtAction(nameof(GetAll), new { lessonId }, new NoteResponse(entity));
        }

        [HttpPut("{noteId:long}")]
        public async Task<IActionResult> Put(long lessonId, long noteId, [FromBody] NoteRequest model)
        {
            var userId = _currentUser.GetCurrentUserId();

            var entity = await _baseRepository.Query()
                .FirstOrDefaultAsync(n => n.Id == noteId && n.LessonId == lessonId && n.UserId == userId && !n.IsDeleted);

            if (entity == null) return NotFound();

            var (success, errors) = model.ValidateNote();
            if (!success)
                return BadRequest(new { status = 400, message = "Validation failed", errors = FlattenErrors(errors) });

            model.MapTo(entity);
            await _baseRepository.SaveChangesAsync();

            return Ok(new NoteResponse(entity));
        }

        [HttpDelete("{noteId:long}")]
        public async Task<IActionResult> Delete(long lessonId, long noteId)
        {
            var userId = _currentUser.GetCurrentUserId();

            var entity = await _baseRepository.Query()
                .FirstOrDefaultAsync(n => n.Id == noteId && n.LessonId == lessonId && n.UserId == userId && !n.IsDeleted);

            if (entity == null) return NotFound();

            entity.IsDeleted = true;
            entity.UpdatedAt = DateTime.UtcNow;
            await _baseRepository.SaveChangesAsync();

            return Ok(new { success = true });
        }

        private static Dictionary<string, object> FlattenErrors(Dictionary<string, List<string>>? errors)
        {
            var result = new Dictionary<string, object>();
            if (errors == null) return result;
            foreach (var kv in errors)
            {
                if (kv.Value == null || kv.Value.Count == 0) continue;
                result[kv.Key] = kv.Value.Count == 1 ? kv.Value[0] : (object)kv.Value;
            }
            return result;
        }
    }
}
