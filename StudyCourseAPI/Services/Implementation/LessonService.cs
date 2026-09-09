using Microsoft.AspNetCore.OData.Query;
using Microsoft.EntityFrameworkCore;
using StudyCourseAPI.DTOs.Requests.Admin;
using StudyCourseAPI.DTOs.Responses.Admin;
using StudyCourseAPI.Extensions;
using StudyCourseAPI.Models;
using StudyCourseAPI.Repositories;

namespace StudyCourseAPI.Services;

public class LessonService : ILessonService
{
    private readonly IRepository<Lesson> _lessonRepository;
    private readonly IRepository<Course> _courseRepository;
    private readonly IRepository<Chapter> _chapterRepository;

    public LessonService(
        IRepository<Lesson> lessonRepository,
        IRepository<Course> courseRepository,
        IRepository<Chapter> chapterRepository)
    {
        _lessonRepository = lessonRepository;
        _courseRepository = courseRepository;
        _chapterRepository = chapterRepository;
    }

    // ─────────────────────────────────────────────────────────
    // Queries
    // ─────────────────────────────────────────────────────────

    public async Task<(int Count, List<LessonResponse> Items)> GetListAsync(
        long courseId, long? chapterId, ODataQueryOptions<Lesson> queryOptions)
    {
        var queryable = _lessonRepository.Query()
            .Where(x => !x.IsDeleted && x.CourseId == courseId);

        if (chapterId.HasValue)
            queryable = queryable.Where(x => x.ChapterId == chapterId.Value);

        var (count, lessons) = await queryable.AppendQueryOptionsAsync(queryOptions);

        return (count, lessons.Select(x => new LessonResponse(x)).ToList());
    }

    public async Task<LessonDetailResponse?> GetByIdAsync(long courseId, long id)
    {
        // Không cần query course riêng: lesson tồn tại và thuộc course thì course hiển nhiên tồn tại.
        var lesson = await _lessonRepository.Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == id && !l.IsDeleted && l.CourseId == courseId);

        return lesson == null ? null : new LessonDetailResponse(lesson);
    }

    // ─────────────────────────────────────────────────────────
    // Commands
    // ─────────────────────────────────────────────────────────

    public async Task<ServiceResult<BulkCreateLessonsResult>> BulkCreateAsync(
        long courseId, BulkCreateLessonsRequest request)
    {
        if (request?.Lessons == null || request.Lessons.Count == 0)
            return ServiceResult<BulkCreateLessonsResult>.Invalid(
                "Request body must contain at least one lesson.");

        var course = await _courseRepository.Query()
            .FirstOrDefaultAsync(c => c.Id == courseId && !c.IsDeleted);

        if (course == null)
            return ServiceResult<BulkCreateLessonsResult>.NotFound();

        // Xác định chapter (không bắt buộc — null = "Chưa phân loại")
        Chapter? chapter = null;
        var isNewChapter = false;

        if (request.NewChapter != null)
        {
            isNewChapter = true;
            chapter = new Chapter
            {
                Title = request.NewChapter.Title ?? string.Empty,
                Description = request.NewChapter.Description,
                OrderIndex = request.NewChapter.OrderIndex,
                CourseId = courseId,
                IsActive = true
            };

            _chapterRepository.Add(chapter);
            await _chapterRepository.SaveChangesAsync();
        }
        else if (request.ChapterId.HasValue)
        {
            chapter = await _chapterRepository.Query()
                .FirstOrDefaultAsync(c =>
                    c.Id == request.ChapterId.Value && c.CourseId == courseId && !c.IsDeleted);

            if (chapter == null)
                return ServiceResult<BulkCreateLessonsResult>.Invalid("Chapter not found.");
        }

        foreach (var lesson in request.Lessons)
            lesson.ChapterId = chapter?.Id;

        // Validate từng item, gom lỗi theo chỉ số để FE map về đúng dòng
        var allErrors = new Dictionary<string, List<string>>();
        for (var i = 0; i < request.Lessons.Count; i++)
        {
            var (valid, errors) = await request.Lessons[i].ValidateLessonAsync(
                _lessonRepository, _chapterRepository, courseId);

            if (valid || errors == null) continue;

            foreach (var kv in errors)
            {
                if (kv.Value == null || kv.Value.Count == 0) continue;
                allErrors[$"Lessons[{i}].{kv.Key}"] = kv.Value;
            }
        }

        if (allErrors.Count > 0)
            return ServiceResult<BulkCreateLessonsResult>.Invalid(allErrors);

        // Dựng entity với OrderIndex an toàn (tự điền khi = 0 hoặc bị trùng)
        var entities = new List<Lesson>();
        var nextIdx = await _lessonRepository.NextOrderIndexAsync(courseId);

        foreach (var model in request.Lessons)
        {
            var entity = model.GetLesson(courseId);
            if (entity.OrderIndex <= 0) entity.OrderIndex = nextIdx++;
            else nextIdx = Math.Max(nextIdx, entity.OrderIndex + 1);

            entities.Add(entity);
            _lessonRepository.Add(entity);
        }

        await _lessonRepository.SaveChangesAsync();

        await RefreshCourseStatsAsync(courseId);

        return ServiceResult<BulkCreateLessonsResult>.Ok(new BulkCreateLessonsResult
        {
            Lessons = entities.Select(e => new LessonResponse(e)).ToList(),
            ChapterId = chapter?.Id,
            ChapterTitle = chapter?.Title,
            IsNewChapter = isNewChapter
        });
    }

    public async Task<ServiceResult<LessonResponse>> UpdateAsync(long courseId, long id, LessonRequest model)
    {
        var course = await _courseRepository.Query()
            .FirstOrDefaultAsync(c => c.Id == courseId && !c.IsDeleted);
        if (course == null)
            return ServiceResult<LessonResponse>.NotFound();

        var entity = await _lessonRepository.Query()
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted && x.CourseId == courseId);
        if (entity == null)
            return ServiceResult<LessonResponse>.NotFound();

        var (valid, errors) = await model.ValidateLessonAsync(
            _lessonRepository, _chapterRepository, courseId, id);

        if (!valid)
            return ServiceResult<LessonResponse>.Invalid(errors);

        model.ToEntity(entity);
        await _lessonRepository.SaveChangesAsync();

        await RefreshCourseStatsAsync(courseId);

        return ServiceResult<LessonResponse>.Ok(new LessonResponse(entity));
    }

    public async Task<int> SoftDeleteAsync(long courseId, List<long> ids)
    {
        var now = DateTime.UtcNow;

        var affected = await _lessonRepository.Query()
            .Where(x => ids.Contains(x.Id) && !x.IsDeleted && x.CourseId == courseId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.IsDeleted, true)
                .SetProperty(x => x.IsActive, false)
                .SetProperty(x => x.UpdatedAt, now));

        if (affected > 0)
            await RefreshCourseStatsAsync(courseId);

        return affected;
    }

    public async Task<int> SetActiveAsync(long courseId, List<long> ids, bool isActive)
    {
        var now = DateTime.UtcNow;

        return await _lessonRepository.Query()
            .Where(x => ids.Contains(x.Id) && x.CourseId == courseId && !x.IsDeleted)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.IsActive, isActive)
                .SetProperty(x => x.UpdatedAt, now));
    }

    public async Task<ServiceResult<int>> ReorderAsync(long courseId, List<LessonReorderItem> items)
    {
        if (items == null || items.Count == 0)
            return ServiceResult<int>.Invalid("Provide at least one item.");

        var ids = items.Select(i => i.Id).ToList();
        var chapterIds = items
            .Where(i => i.ChapterId.HasValue)
            .Select(i => i.ChapterId!.Value)
            .Distinct()
            .ToList();

        // Validate song song: load lesson + danh sách chapter hợp lệ trong 1 cặp round-trip
        var lessonsTask = _lessonRepository.Query()
            .Where(l => ids.Contains(l.Id) && l.CourseId == courseId && !l.IsDeleted)
            .ToListAsync();

        var validChapterIdsTask = chapterIds.Count == 0
            ? Task.FromResult(new List<long>())
            : _chapterRepository.Query()
                .Where(c => chapterIds.Contains(c.Id) && c.CourseId == courseId && !c.IsDeleted)
                .Select(c => c.Id)
                .ToListAsync();

        await Task.WhenAll(lessonsTask, validChapterIdsTask);
        var lessons = lessonsTask.Result;
        var validChapterIds = validChapterIdsTask.Result;

        if (lessons.Count != items.Count)
            return ServiceResult<int>.Invalid("Some lessons do not belong to this course.");

        if (chapterIds.Count > 0 && validChapterIds.Count != chapterIds.Count)
            return ServiceResult<int>.Invalid("Some chapters do not belong to this course.");

        var byId = lessons.ToDictionary(l => l.Id);
        foreach (var item in items)
        {
            if (!byId.TryGetValue(item.Id, out var lesson)) continue;
            lesson.OrderIndex = item.OrderIndex;
            lesson.ChapterId = item.ChapterId;
        }

        await _lessonRepository.SaveChangesAsync();

        return ServiceResult<int>.Ok(lessons.Count);
    }

    // ─────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────

    /// <summary>Cập nhật lại LessonCount / ChapterCount / TotalDuration đang cache trên Course.</summary>
    private async Task RefreshCourseStatsAsync(long courseId)
    {
        await _courseRepository.RefreshCourseStatsAsync(_lessonRepository, _chapterRepository, courseId);
        await _courseRepository.SaveChangesAsync();
    }
}
