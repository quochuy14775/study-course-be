using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.EntityFrameworkCore;
using StudyCourseAPI.DTOs.Requests.Admin;
using StudyCourseAPI.DTOs.Responses;
using StudyCourseAPI.DTOs.Responses.Admin;
using StudyCourseAPI.Extensions;
using StudyCourseAPI.Models;
using StudyCourseAPI.Repositories;

namespace StudyCourseAPI.Controllers;

[Route("api/[controller]")]
[Authorize]
public class RoadmapsController : BaseController<Roadmap>
{
    private readonly IRepository<RoadmapCourse> _roadmapCourseRepository;
    private readonly IRepository<Course> _courseRepository;

    public RoadmapsController(
        IRepository<Roadmap> baseRepository,
        ICurrentUser currentUser,
        IRepository<RoadmapCourse> roadmapCourseRepository,
        IRepository<Course> courseRepository)
        : base(baseRepository, currentUser)
    {
        _roadmapCourseRepository = roadmapCourseRepository;
        _courseRepository = courseRepository;
    }

    // ─────────────────────────────────────────────────────────
    // GET — list with OData
    // ─────────────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> Get(ODataQueryOptions<Roadmap> queryOptions)
    {
        var queryable = _baseRepository.Query()
            .Where(r => !r.IsDeleted && r.IsActive)
            .Include(r => r.RoadmapCourses)
                .ThenInclude(rc => rc.Course)
                    .ThenInclude(c => c.Chapters)
            .AsSplitQuery();

        var (count, vm) = await queryable.AppendQueryOptionsAsync(queryOptions);

        return Ok(new ODataResponse<RoadmapResponse>
        {
            Count = count,
            Value = vm.Select(r => new RoadmapResponse(r))
        });
    }

    // ─────────────────────────────────────────────────────────
    // GET single
    // ─────────────────────────────────────────────────────────
    [HttpGet("{id}")]
    public async Task<IActionResult> Get(long id)
    {
        var roadmap = await _baseRepository.Query()
            .AsNoTracking()
            .Include(r => r.RoadmapCourses)
                .ThenInclude(rc => rc.Course)
                    .ThenInclude(c => c.Chapters)
            .AsSplitQuery()
            .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted);

        if (roadmap == null) return NotFound();
        return Ok(new RoadmapResponse(roadmap));
    }

    // ─────────────────────────────────────────────────────────
    // POST
    // ─────────────────────────────────────────────────────────
    [Authorize(Roles = AppRoles.Admin)]
    [HttpPost]
    public async Task<IActionResult> Post([FromBody] RoadmapRequest model)
    {
        var (success, errors) = await model.ValidateRoadmapAsync(_baseRepository);
        if (!success)
            return this.ValidationFailed(errors);

        var entity = model.GetRoadmap();
        _baseRepository.Add(entity);
        await _baseRepository.SaveChangesAsync();

        await entity.SyncCoursesAsync(_roadmapCourseRepository, _courseRepository, model.CourseIds);
        await _roadmapCourseRepository.SaveChangesAsync();

        // Reload with includes for response
        var created = await _baseRepository.Query()
            .Include(r => r.RoadmapCourses)
                .ThenInclude(rc => rc.Course)
                    .ThenInclude(c => c.Chapters)
            .FirstAsync(r => r.Id == entity.Id);

        return CreatedAtAction(
            nameof(Get),
            new { id = entity.Id },
            new { success = true, message = "Roadmap created successfully.", data = new RoadmapResponse(created) });
    }

    // ─────────────────────────────────────────────────────────
    // PUT {id} — update
    // ─────────────────────────────────────────────────────────
    [Authorize(Roles = AppRoles.Admin)]
    [HttpPut("{id}")]
    public async Task<IActionResult> Put(long id, [FromBody] RoadmapRequest model)
    {
        var entity = await _baseRepository.Query()
            .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted);

        if (entity == null) return NotFound();

        var (success, errors) = await model.ValidateRoadmapAsync(_baseRepository, id);
        if (!success)
            return this.ValidationFailed(errors);

        entity.Title = model.Title.Trim();
        entity.Description = model.Description?.Trim();
        entity.IsActive = model.IsActive;

        await _baseRepository.SaveChangesAsync();

        await entity.SyncCoursesAsync(_roadmapCourseRepository, _courseRepository, model.CourseIds);
        await _roadmapCourseRepository.SaveChangesAsync();

        var updated = await _baseRepository.Query()
            .Include(r => r.RoadmapCourses)
                .ThenInclude(rc => rc.Course)
                    .ThenInclude(c => c.Chapters)
            .FirstAsync(r => r.Id == entity.Id);

        return Ok(new { success = true, message = "Roadmap updated successfully.", data = new RoadmapResponse(updated) });
    }

    // ─────────────────────────────────────────────────────────
    // PUT /delete — bulk soft-delete
    // ─────────────────────────────────────────────────────────
    [Authorize(Roles = AppRoles.Admin)]
    [HttpPut("delete")]
    public async Task<IActionResult> Delete([FromBody] List<long> ids)
    {
        if (ids == null || ids.Count == 0)
            return BadRequest(new { status = 400, message = "Provide at least one roadmap id." });

        var now = DateTime.UtcNow;
        var affected = await _baseRepository.Query()
            .Where(r => ids.Contains(r.Id) && !r.IsDeleted)
            .ExecuteUpdateAsync(s => s
                .SetProperty(r => r.IsDeleted, true)
                .SetProperty(r => r.IsActive, false)
                .SetProperty(r => r.UpdatedAt, now));

        return affected == 0 ? NotFound() : Ok(new { success = true, deleted = affected });
    }

    // ─────────────────────────────────────────────────────────
    // PUT /disable — bulk
    // ─────────────────────────────────────────────────────────
    [Authorize(Roles = AppRoles.Admin)]
    [HttpPut("disable")]
    public async Task<IActionResult> Disable([FromBody] List<long> ids)
    {
        var now = DateTime.UtcNow;
        var affected = await _baseRepository.Query()
            .Where(r => ids.Contains(r.Id) && !r.IsDeleted)
            .ExecuteUpdateAsync(s => s
                .SetProperty(r => r.IsActive, false)
                .SetProperty(r => r.UpdatedAt, now));

        return affected == 0 ? NotFound() : NoContent();
    }

    // ─────────────────────────────────────────────────────────
    // PUT /enable — bulk
    // ─────────────────────────────────────────────────────────
    [Authorize(Roles = AppRoles.Admin)]
    [HttpPut("enable")]
    public async Task<IActionResult> Enable([FromBody] List<long> ids)
    {
        var now = DateTime.UtcNow;
        var affected = await _baseRepository.Query()
            .Where(r => ids.Contains(r.Id) && !r.IsDeleted)
            .ExecuteUpdateAsync(s => s
                .SetProperty(r => r.IsActive, true)
                .SetProperty(r => r.UpdatedAt, now));

        return affected == 0 ? NotFound() : NoContent();
    }
}
