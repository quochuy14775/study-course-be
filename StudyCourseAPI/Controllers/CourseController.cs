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
    [Route("api/[controller]")]
    [Authorize]
    public class CoursesController : ODataController
    {
        private readonly ICourseService _courseService;

        public CoursesController(ICourseService courseService)
        {
            _courseService = courseService;
        }

        // ─────────────────────────────────────────────────────────
        // GET — list with OData (public — no auth required)
        // ─────────────────────────────────────────────────────────
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> Get(ODataQueryOptions<Course> queryOptions)
        {
            var (count, items) = await _courseService.GetListAsync(queryOptions);

            return Ok(new ODataResponse<CourseResponse>
            {
                Count = count,
                Value = items
            });
        }

        // ─────────────────────────────────────────────────────────
        // GET single (public)
        // ─────────────────────────────────────────────────────────
        [AllowAnonymous]
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(long id)
        {
            var course = await _courseService.GetByIdAsync(id);

            if (course == null) return NotFound();
            return Ok(course);
        }

        // ─────────────────────────────────────────────────────────
        // POST
        // ─────────────────────────────────────────────────────────
        [Authorize(Roles = AppRoles.Admin)]
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] CourseRequest model)
        {
            var result = await _courseService.CreateAsync(model);

            if (!result.IsSuccess)
                return this.ValidationFailed(result.Errors);

            return CreatedAtAction(
                nameof(Get),
                new { id = result.Data!.Id },
                new
                {
                    success = true,
                    message = "Course created successfully.",
                    data = result.Data
                });
        }

        // ─────────────────────────────────────────────────────────
        // PUT {id} — update
        // ─────────────────────────────────────────────────────────
        [Authorize(Roles = AppRoles.Admin)]
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(long id, [FromBody] CourseRequest model)
        {
            var result = await _courseService.UpdateAsync(id, model);

            if (result.IsNotFound) return NotFound();
            if (!result.IsSuccess) return this.ValidationFailed(result.Errors);

            return Ok(new
            {
                success = true,
                message = "Course updated successfully.",
                data = result.Data
            });
        }

        // ─────────────────────────────────────────────────────────
        // PUT /delete — bulk soft-delete
        // ─────────────────────────────────────────────────────────
        [Authorize(Roles = AppRoles.Admin)]
        [HttpPut("delete")]
        public async Task<IActionResult> Delete([FromBody] List<long> ids)
        {
            if (ids == null || ids.Count == 0)
                return BadRequest(new { status = 400, message = "Provide at least one course id." });

            var affected = await _courseService.SoftDeleteAsync(ids);

            return affected == 0
                ? NotFound()
                : Ok(new { success = true, deleted = affected });
        }

        // ─────────────────────────────────────────────────────────
        // PUT /disable — bulk
        // ─────────────────────────────────────────────────────────
        [Authorize(Roles = AppRoles.Admin)]
        [HttpPut("disable")]
        public async Task<IActionResult> Disable([FromBody] List<long> ids)
        {
            var affected = await _courseService.SetActiveAsync(ids, isActive: false);

            return affected == 0 ? NotFound() : NoContent();
        }

        // ─────────────────────────────────────────────────────────
        // PUT /enable — bulk
        // ─────────────────────────────────────────────────────────
        [Authorize(Roles = AppRoles.Admin)]
        [HttpPut("enable")]
        public async Task<IActionResult> Enable([FromBody] List<long> ids)
        {
            var affected = await _courseService.SetActiveAsync(ids, isActive: true);

            return affected == 0 ? NotFound() : NoContent();
        }

        // ─────────────────────────────────────────────────────────
        // GET /suggest?keyword=... — quick search for autocomplete (public)
        // ─────────────────────────────────────────────────────────
        [AllowAnonymous]
        [HttpGet("suggest")]
        public async Task<IActionResult> Suggest([FromQuery] string? keyword)
        {
            var suggestions = await _courseService.SuggestAsync(keyword);
            return Ok(suggestions);
        }
    }
}
