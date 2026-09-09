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

namespace StudyCourseAPI.Controllers;

[Route("api/[controller]")]
[Authorize]
public class RoadmapsController : ODataController
{
    private readonly IRoadmapService _roadmapService;

    public RoadmapsController(IRoadmapService roadmapService)
    {
        _roadmapService = roadmapService;
    }

    // ─────────────────────────────────────────────────────────
    // GET — list with OData
    // ─────────────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> Get(ODataQueryOptions<Roadmap> queryOptions)
    {
        var (count, items) = await _roadmapService.GetListAsync(queryOptions);

        return Ok(new ODataResponse<RoadmapResponse>
        {
            Count = count,
            Value = items
        });
    }

    // ─────────────────────────────────────────────────────────
    // GET single
    // ─────────────────────────────────────────────────────────
    [HttpGet("{id}")]
    public async Task<IActionResult> Get(long id)
    {
        var roadmap = await _roadmapService.GetByIdAsync(id);

        if (roadmap == null) return NotFound();
        return Ok(roadmap);
    }

    // ─────────────────────────────────────────────────────────
    // POST
    // ─────────────────────────────────────────────────────────
    [Authorize(Roles = AppRoles.Admin)]
    [HttpPost]
    public async Task<IActionResult> Post([FromBody] RoadmapRequest model)
    {
        var result = await _roadmapService.CreateAsync(model);

        if (!result.IsSuccess) return this.ValidationFailed(result.Errors);

        return CreatedAtAction(
            nameof(Get),
            new { id = result.Data!.Id },
            new { success = true, message = "Roadmap created successfully.", data = result.Data });
    }

    // ─────────────────────────────────────────────────────────
    // PUT {id} — update
    // ─────────────────────────────────────────────────────────
    [Authorize(Roles = AppRoles.Admin)]
    [HttpPut("{id}")]
    public async Task<IActionResult> Put(long id, [FromBody] RoadmapRequest model)
    {
        var result = await _roadmapService.UpdateAsync(id, model);

        if (result.IsNotFound) return NotFound();
        if (!result.IsSuccess) return this.ValidationFailed(result.Errors);

        return Ok(new { success = true, message = "Roadmap updated successfully.", data = result.Data });
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

        var affected = await _roadmapService.SoftDeleteAsync(ids);

        return affected == 0 ? NotFound() : Ok(new { success = true, deleted = affected });
    }

    // ─────────────────────────────────────────────────────────
    // PUT /disable — bulk
    // ─────────────────────────────────────────────────────────
    [Authorize(Roles = AppRoles.Admin)]
    [HttpPut("disable")]
    public async Task<IActionResult> Disable([FromBody] List<long> ids)
    {
        var affected = await _roadmapService.SetActiveAsync(ids, isActive: false);

        return affected == 0 ? NotFound() : NoContent();
    }

    // ─────────────────────────────────────────────────────────
    // PUT /enable — bulk
    // ─────────────────────────────────────────────────────────
    [Authorize(Roles = AppRoles.Admin)]
    [HttpPut("enable")]
    public async Task<IActionResult> Enable([FromBody] List<long> ids)
    {
        var affected = await _roadmapService.SetActiveAsync(ids, isActive: true);

        return affected == 0 ? NotFound() : NoContent();
    }
}
