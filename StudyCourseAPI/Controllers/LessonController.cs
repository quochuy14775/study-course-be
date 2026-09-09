using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using StudyCourseAPI.DTOs.Requests.Admin;
using StudyCourseAPI.DTOs.Responses;
using StudyCourseAPI.DTOs.Responses.Admin;
using StudyCourseAPI.Extensions;
using StudyCourseAPI.Models;
using StudyCourseAPI.Services;

namespace StudyCourseAPI.Controllers
{
    [Route("api/Courses/{courseId}/[controller]")]
    [Authorize]
    public class LessonsController : ODataController
    {
        private readonly ILessonService _lessonService;

        public LessonsController(ILessonService lessonService)
        {
            _lessonService = lessonService;
        }

        // ─────────────────────────────────────────────────────────
        // GET — list with OData (optional ?chapterId= for filtering)
        // ─────────────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Get(long courseId, [FromQuery] long? chapterId, ODataQueryOptions<Lesson> queryOptions)
        {
            var (count, items) = await _lessonService.GetListAsync(courseId, chapterId, queryOptions);

            return Ok(new ODataResponse<LessonResponse>
            {
                Count = count,
                Value = items
            });
        }

        // ─────────────────────────────────────────────────────────
        // GET single
        // ─────────────────────────────────────────────────────────
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(long courseId, long id)
        {
            var lesson = await _lessonService.GetByIdAsync(courseId, id);

            if (lesson == null) return NotFound();
            return Ok(lesson);
        }

        // ─────────────────────────────────────────────────────────
        // POST — bulk create lessons within a chapter
        // ─────────────────────────────────────────────────────────
        [Authorize(Roles = AppRoles.Admin)]
        [HttpPost]
        public async Task<IActionResult> Post(long courseId, [FromBody] BulkCreateLessonsRequest request)
        {
            var result = await _lessonService.BulkCreateAsync(courseId, request);

            if (result.ErrorMessage != null)
                return BadRequest(new { status = 400, message = result.ErrorMessage });
            if (result.IsNotFound) return NotFound();
            if (!result.IsSuccess) return this.ValidationFailed(result.Errors);

            var data = result.Data!;

            return Ok(new
            {
                success = true,
                message = $"Created {data.Lessons.Count} lesson(s) successfully."
                          + (data.IsNewChapter ? $" Created chapter '{data.ChapterTitle}'." : ""),
                data = data.Lessons,
                chapterId = data.ChapterId,
                chapterTitle = data.ChapterTitle ?? "Chưa phân loại"
            });
        }

        // ─────────────────────────────────────────────────────────
        // PUT {id} — update single
        // ─────────────────────────────────────────────────────────
        [Authorize(Roles = AppRoles.Admin)]
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(long courseId, long id, [FromBody] LessonRequest model)
        {
            var result = await _lessonService.UpdateAsync(courseId, id, model);

            if (result.IsNotFound) return NotFound();
            if (!result.IsSuccess) return this.ValidationFailed(result.Errors);

            return Ok(new
            {
                success = true,
                message = "Lesson updated successfully.",
                data = result.Data
            });
        }

        // ─────────────────────────────────────────────────────────
        // PUT /delete — bulk soft-delete
        // ─────────────────────────────────────────────────────────
        [Authorize(Roles = AppRoles.Admin)]
        [HttpPut("delete")]
        public async Task<IActionResult> Delete(long courseId, [FromBody] List<long> ids)
        {
            if (ids == null || ids.Count == 0)
                return BadRequest(new { status = 400, message = "Provide at least one lesson id." });

            var affected = await _lessonService.SoftDeleteAsync(courseId, ids);

            if (affected == 0) return NotFound();
            return Ok(new { success = true, deleted = affected });
        }

        // ─────────────────────────────────────────────────────────
        // PUT /disable — bulk
        // ─────────────────────────────────────────────────────────
        [Authorize(Roles = AppRoles.Admin)]
        [HttpPut("disable")]
        public async Task<IActionResult> Disable(long courseId, [FromBody] List<long> ids)
        {
            var affected = await _lessonService.SetActiveAsync(courseId, ids, isActive: false);

            return affected == 0 ? NotFound() : NoContent();
        }

        // ─────────────────────────────────────────────────────────
        // PUT /enable — bulk
        // ─────────────────────────────────────────────────────────
        [Authorize(Roles = AppRoles.Admin)]
        [HttpPut("enable")]
        public async Task<IActionResult> Enable(long courseId, [FromBody] List<long> ids)
        {
            var affected = await _lessonService.SetActiveAsync(courseId, ids, isActive: true);

            return affected == 0 ? NotFound() : NoContent();
        }

        // ─────────────────────────────────────────────────────────
        // PUT /reorder — drag-drop support (CurriculumBuilder)
        // Body: [{ id, orderIndex, chapterId? }, ...]
        // ─────────────────────────────────────────────────────────
        [Authorize(Roles = AppRoles.Admin)]
        [HttpPut("reorder")]
        public async Task<IActionResult> Reorder(long courseId, [FromBody] List<LessonReorderItem> items)
        {
            var result = await _lessonService.ReorderAsync(courseId, items);

            if (!result.IsSuccess)
                return BadRequest(new { status = 400, message = result.ErrorMessage });

            return Ok(new { success = true, updated = result.Data });
        }
    }
}
