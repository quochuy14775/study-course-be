using Microsoft.AspNetCore.OData.Query;
using Microsoft.EntityFrameworkCore;
using StudyCourseAPI.DTOs.Requests.Admin;
using StudyCourseAPI.DTOs.Responses.Admin;
using StudyCourseAPI.Extensions;
using StudyCourseAPI.Models;
using StudyCourseAPI.Repositories;

namespace StudyCourseAPI.Services;

public class RoadmapService : IRoadmapService
{
    private readonly IRepository<Roadmap> _roadmapRepository;
    private readonly IRepository<RoadmapCourse> _roadmapCourseRepository;
    private readonly IRepository<Course> _courseRepository;

    public RoadmapService(
        IRepository<Roadmap> roadmapRepository,
        IRepository<RoadmapCourse> roadmapCourseRepository,
        IRepository<Course> courseRepository)
    {
        _roadmapRepository = roadmapRepository;
        _roadmapCourseRepository = roadmapCourseRepository;
        _courseRepository = courseRepository;
    }

    // ─────────────────────────────────────────────────────────
    // Queries
    // ─────────────────────────────────────────────────────────

    public async Task<(int Count, List<RoadmapResponse> Items)> GetListAsync(
        ODataQueryOptions<Roadmap> queryOptions)
    {
        var queryable = _roadmapRepository.Query()
            .Where(r => !r.IsDeleted && r.IsActive)
            .Include(r => r.RoadmapCourses)
                .ThenInclude(rc => rc.Course)
                    .ThenInclude(c => c.Chapters)
            .AsSplitQuery();

        var (count, roadmaps) = await queryable.AppendQueryOptionsAsync(queryOptions);

        return (count, roadmaps.Select(r => new RoadmapResponse(r)).ToList());
    }

    public async Task<RoadmapResponse?> GetByIdAsync(long id)
    {
        var roadmap = await _roadmapRepository.Query()
            .AsNoTracking()
            .Include(r => r.RoadmapCourses)
                .ThenInclude(rc => rc.Course)
                    .ThenInclude(c => c.Chapters)
            .AsSplitQuery()
            .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted);

        return roadmap == null ? null : new RoadmapResponse(roadmap);
    }

    // ─────────────────────────────────────────────────────────
    // Commands
    // ─────────────────────────────────────────────────────────

    public async Task<ServiceResult<RoadmapResponse>> CreateAsync(RoadmapRequest model)
    {
        var (valid, errors) = await model.ValidateRoadmapAsync(_roadmapRepository);
        if (!valid)
            return ServiceResult<RoadmapResponse>.Invalid(errors);

        var entity = model.GetRoadmap();
        _roadmapRepository.Add(entity);
        await _roadmapRepository.SaveChangesAsync();

        await entity.SyncCoursesAsync(_roadmapCourseRepository, _courseRepository, model.CourseIds);
        await _roadmapCourseRepository.SaveChangesAsync();

        var created = await LoadWithCoursesAsync(entity.Id);

        return ServiceResult<RoadmapResponse>.Ok(new RoadmapResponse(created));
    }

    public async Task<ServiceResult<RoadmapResponse>> UpdateAsync(long id, RoadmapRequest model)
    {
        var entity = await _roadmapRepository.Query()
            .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted);

        if (entity == null)
            return ServiceResult<RoadmapResponse>.NotFound();

        var (valid, errors) = await model.ValidateRoadmapAsync(_roadmapRepository, id);
        if (!valid)
            return ServiceResult<RoadmapResponse>.Invalid(errors);

        entity.Title = model.Title.Trim();
        entity.Description = model.Description?.Trim();
        entity.IsActive = model.IsActive;

        await _roadmapRepository.SaveChangesAsync();

        await entity.SyncCoursesAsync(_roadmapCourseRepository, _courseRepository, model.CourseIds);
        await _roadmapCourseRepository.SaveChangesAsync();

        var updated = await LoadWithCoursesAsync(entity.Id);

        return ServiceResult<RoadmapResponse>.Ok(new RoadmapResponse(updated));
    }

    public async Task<int> SoftDeleteAsync(List<long> ids)
    {
        var now = DateTime.UtcNow;

        return await _roadmapRepository.Query()
            .Where(r => ids.Contains(r.Id) && !r.IsDeleted)
            .ExecuteUpdateAsync(s => s
                .SetProperty(r => r.IsDeleted, true)
                .SetProperty(r => r.IsActive, false)
                .SetProperty(r => r.UpdatedAt, now));
    }

    public async Task<int> SetActiveAsync(List<long> ids, bool isActive)
    {
        var now = DateTime.UtcNow;

        return await _roadmapRepository.Query()
            .Where(r => ids.Contains(r.Id) && !r.IsDeleted)
            .ExecuteUpdateAsync(s => s
                .SetProperty(r => r.IsActive, isActive)
                .SetProperty(r => r.UpdatedAt, now));
    }

    // ─────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────

    /// <summary>Nạp lại roadmap kèm course + chapter để dựng response đầy đủ sau khi ghi.</summary>
    private Task<Roadmap> LoadWithCoursesAsync(long id)
        => _roadmapRepository.Query()
            .Include(r => r.RoadmapCourses)
                .ThenInclude(rc => rc.Course)
                    .ThenInclude(c => c.Chapters)
            .FirstAsync(r => r.Id == id);
}
