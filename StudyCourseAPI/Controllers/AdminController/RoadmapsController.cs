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

namespace StudyCourseAPI.Controllers.AdminController;

[Route("api/[controller]")]
[Authorize]
public class RoadmapsController : BaseController<Roadmap>
{
    private readonly IRepository<Roadmap> _roadmapRepository;
    private readonly IRepository<RoadmapCourse> _roadmapCourseRepository;
    private readonly IRepository<Course> _courseRepository;

    public RoadmapsController(
        IRepository<Roadmap> baseRepository,
        ICurrentUser currentUser,
        IRepository<RoadmapCourse> roadmapCourseRepository,
        IRepository<Course> courseRepository)
        : base(baseRepository, currentUser)
    {
        _roadmapRepository = baseRepository;
        _roadmapCourseRepository = roadmapCourseRepository;
        _courseRepository = courseRepository;
    }

    // ─────────────────────────────────────────────────────────
    // GET — list with OData
    // ─────────────────────────────────────────────────────────
    [HttpGet]
    public IActionResult Get(ODataQueryOptions<Roadmap> queryOptions)
    {
        var queryable = _roadmapRepository.Query()
            .Where(r => !r.IsDeleted && r.IsActive)
            .Include(r => r.RoadmapCourses)
                .ThenInclude(rc => rc.Course)
                    .ThenInclude(c => c.Chapters);

        var (count, vm) = queryable.AppendQueryOptions(queryOptions);

        var response = new ODataResponse<RoadmapResponse>
        {
            Count = count,
            Value = vm.Select(r => new RoadmapResponse(r))
        };

        return Ok(response);
    }

    // ─────────────────────────────────────────────────────────
    // GET single
    // ─────────────────────────────────────────────────────────
    [HttpGet("{id}")]
    public async Task<IActionResult> Get(long id)
    {
        var roadmap = await _roadmapRepository.Query()
            .Include(r => r.RoadmapCourses)
                .ThenInclude(rc => rc.Course)
                    .ThenInclude(c => c.Chapters)
            .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted);

        if (roadmap == null) return NotFound();

        return Ok(new RoadmapResponse(roadmap));
    }

    // ─────────────────────────────────────────────────────────
    // POST
    // ─────────────────────────────────────────────────────────
    
    [HttpPost]
    public async Task<IActionResult> Post([FromBody] RoadmapRequest model)
    {
        var (success, errors) = await model.ValidateRoadmapAsync(_roadmapRepository);
        if (!success)
            return BadRequest(new { status = 400, message = "Validation failed", errors = FlattenErrors(errors) });

        var entity = model.GetRoadmap();
        _roadmapRepository.Add(entity);
        await _roadmapRepository.SaveChangesAsync();

        await entity.SyncCoursesAsync(_roadmapCourseRepository, _courseRepository, model.CourseIds);
        await _roadmapCourseRepository.SaveChangesAsync();

        // Reload with includes for response
        var created = await _roadmapRepository.Query()
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
        var entity = await _roadmapRepository.Query()
            .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted);

        if (entity == null) return NotFound();

        var (success, errors) = await model.ValidateRoadmapAsync(_roadmapRepository, id);
        if (!success)
            return BadRequest(new { status = 400, message = "Validation failed", errors = FlattenErrors(errors) });

        entity.Title = model.Title.Trim();
        entity.Description = model.Description?.Trim();
        entity.IsActive = model.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;

        await _roadmapRepository.SaveChangesAsync();

        await entity.SyncCoursesAsync(_roadmapCourseRepository, _courseRepository, model.CourseIds);
        await _roadmapCourseRepository.SaveChangesAsync();

        var updated = await _roadmapRepository.Query()
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

        var entities = await _roadmapRepository.Query()
            .Where(r => ids.Contains(r.Id) && !r.IsDeleted)
            .ToListAsync();

        if (!entities.Any()) return NotFound();

        foreach (var e in entities)
        {
            e.IsDeleted = true;
            e.IsActive = false;
            e.UpdatedAt = DateTime.UtcNow;
        }

        await _roadmapRepository.SaveChangesAsync();
        return Ok(new { success = true, deleted = entities.Count });
    }

    // ─────────────────────────────────────────────────────────
    // PUT /disable — bulk
    // ─────────────────────────────────────────────────────────
    [Authorize(Roles = AppRoles.Admin)]
    [HttpPut("disable")]
    public async Task<IActionResult> Disable([FromBody] List<long> ids)
    {
        var entities = await _roadmapRepository.Query()
            .Where(r => ids.Contains(r.Id) && !r.IsDeleted)
            .ToListAsync();

        if (!entities.Any()) return NotFound();

        foreach (var e in entities)
        {
            e.IsActive = false;
            e.UpdatedAt = DateTime.UtcNow;
        }

        await _roadmapRepository.SaveChangesAsync();
        return NoContent();
    }

    // ─────────────────────────────────────────────────────────
    // PUT /enable — bulk
    // ─────────────────────────────────────────────────────────
    [HttpPut("enable")]
    public async Task<IActionResult> Enable([FromBody] List<long> ids)
    {
        var entities = await _roadmapRepository.Query()
            .Where(r => ids.Contains(r.Id) && !r.IsDeleted)
            .ToListAsync();

        if (!entities.Any()) return NotFound();

        foreach (var e in entities)
        {
            e.IsActive = true;
            e.UpdatedAt = DateTime.UtcNow;
        }

        await _roadmapRepository.SaveChangesAsync();
        return NoContent();
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
