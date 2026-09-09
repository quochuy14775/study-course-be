using Microsoft.EntityFrameworkCore;
using StudyCourseAPI.DTOs.Requests.Admin;
using StudyCourseAPI.DTOs.Responses.Admin;
using StudyCourseAPI.Models;
using StudyCourseAPI.Repositories;

namespace StudyCourseAPI.Services;

public class ChapterService : IChapterService
{
    private readonly IRepository<Chapter> _chapterRepository;
    private readonly IRepository<Course> _courseRepository;

    public ChapterService(
        IRepository<Chapter> chapterRepository,
        IRepository<Course> courseRepository)
    {
        _chapterRepository = chapterRepository;
        _courseRepository = courseRepository;
    }

    // ─────────────────────────────────────────────────────────
    // Queries
    // ─────────────────────────────────────────────────────────

    public async Task<List<ChapterResponse>?> GetByCourseAsync(long courseId)
    {
        var chapters = await _chapterRepository.Query()
            .AsNoTracking()
            .Where(c => c.CourseId == courseId && !c.IsDeleted)
            .Include(c => c.Lessons.Where(l => !l.IsDeleted))
            .AsSplitQuery()
            .OrderBy(c => c.OrderIndex)
            .ToListAsync();

        // Chỉ hỏi DB về sự tồn tại của course khi không có chapter nào —
        // trường hợp phổ biến (có dữ liệu) không tốn thêm round-trip.
        if (chapters.Count == 0)
        {
            var courseExists = await _courseRepository.Query()
                .AsNoTracking()
                .AnyAsync(c => c.Id == courseId && !c.IsDeleted);

            if (!courseExists) return null;
        }

        return chapters.Select(c => new ChapterResponse(c)).ToList();
    }

    public async Task<ChapterResponse?> GetByIdAsync(long courseId, long id)
    {
        var chapter = await _chapterRepository.Query()
            .AsNoTracking()
            .Where(c => c.Id == id && c.CourseId == courseId && !c.IsDeleted)
            .Include(c => c.Lessons.Where(l => !l.IsDeleted))
            .AsSplitQuery()
            .FirstOrDefaultAsync();

        return chapter == null ? null : new ChapterResponse(chapter);
    }

    // ─────────────────────────────────────────────────────────
    // Commands
    // ─────────────────────────────────────────────────────────

    public async Task<ServiceResult<ChapterResponse>> CreateAsync(long courseId, ChapterRequest model)
    {
        if (string.IsNullOrWhiteSpace(model.Title))
            return ServiceResult<ChapterResponse>.Invalid("Title is required.");

        var course = await _courseRepository.Query()
            .FirstOrDefaultAsync(c => c.Id == courseId && !c.IsDeleted);

        if (course == null)
            return ServiceResult<ChapterResponse>.NotFound();

        var entity = new Chapter
        {
            Title = model.Title.Trim(),
            Description = model.Description?.Trim(),
            OrderIndex = model.OrderIndex,
            CourseId = courseId,
            IsActive = model.IsActive
        };

        _chapterRepository.Add(entity);
        await _chapterRepository.SaveChangesAsync();

        var created = await _chapterRepository.Query()
            .Where(c => c.Id == entity.Id)
            .Include(c => c.Lessons)
            .FirstAsync();

        return ServiceResult<ChapterResponse>.Ok(new ChapterResponse(created));
    }

    public async Task<ServiceResult<ChapterResponse>> UpdateAsync(long courseId, long id, ChapterRequest model)
    {
        if (string.IsNullOrWhiteSpace(model.Title))
            return ServiceResult<ChapterResponse>.Invalid("Title is required.");

        var entity = await _chapterRepository.Query()
            .FirstOrDefaultAsync(c => c.Id == id && c.CourseId == courseId && !c.IsDeleted);

        if (entity == null)
            return ServiceResult<ChapterResponse>.NotFound();

        entity.Title = model.Title.Trim();
        entity.Description = model.Description?.Trim();
        entity.OrderIndex = model.OrderIndex;
        entity.IsActive = model.IsActive;

        await _chapterRepository.SaveChangesAsync();

        var updated = await _chapterRepository.Query()
            .Where(c => c.Id == entity.Id)
            .Include(c => c.Lessons)
            .FirstAsync();

        return ServiceResult<ChapterResponse>.Ok(new ChapterResponse(updated));
    }

    public async Task<bool> SoftDeleteAsync(long courseId, long id)
    {
        var entity = await _chapterRepository.Query()
            .FirstOrDefaultAsync(c => c.Id == id && c.CourseId == courseId && !c.IsDeleted);

        if (entity == null) return false;

        entity.IsDeleted = true;
        entity.IsActive = false;

        await _chapterRepository.SaveChangesAsync();

        return true;
    }

    public async Task<int> SetActiveAsync(long courseId, List<long> ids, bool isActive)
    {
        var now = DateTime.UtcNow;

        return await _chapterRepository.Query()
            .Where(c => ids.Contains(c.Id) && c.CourseId == courseId && !c.IsDeleted)
            .ExecuteUpdateAsync(s => s
                .SetProperty(c => c.IsActive, isActive)
                .SetProperty(c => c.UpdatedAt, now));
    }
}
