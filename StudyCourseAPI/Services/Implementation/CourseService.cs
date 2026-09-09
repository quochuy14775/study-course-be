using Microsoft.AspNetCore.OData.Query;
using Microsoft.EntityFrameworkCore;
using StudyCourseAPI.DTOs.Requests.Admin;
using StudyCourseAPI.DTOs.Responses.Admin;
using StudyCourseAPI.Enums;
using StudyCourseAPI.Extensions;
using StudyCourseAPI.Models;
using StudyCourseAPI.Repositories;

namespace StudyCourseAPI.Services;

public class CourseService : ICourseService
{
    private readonly IRepository<Course> _courseRepository;
    private readonly IRepository<CourseTag> _courseTagRepository;
    private readonly IRepository<Tag> _tagRepository;
    private readonly IRepository<CourseLanguage> _courseLanguageRepository;
    private readonly IRepository<CourseFramework> _courseFrameworkRepository;
    private readonly IRepository<Language> _languageRepository;
    private readonly IRepository<Framework> _frameworkRepository;
    private readonly INotificationService _notifier;
    private readonly ICurrentUser _currentUser;

    public CourseService(
        IRepository<Course> courseRepository,
        IRepository<CourseTag> courseTagRepository,
        IRepository<Tag> tagRepository,
        IRepository<CourseLanguage> courseLanguageRepository,
        IRepository<CourseFramework> courseFrameworkRepository,
        IRepository<Language> languageRepository,
        IRepository<Framework> frameworkRepository,
        INotificationService notifier,
        ICurrentUser currentUser)
    {
        _courseRepository = courseRepository;
        _courseTagRepository = courseTagRepository;
        _tagRepository = tagRepository;
        _courseLanguageRepository = courseLanguageRepository;
        _courseFrameworkRepository = courseFrameworkRepository;
        _languageRepository = languageRepository;
        _frameworkRepository = frameworkRepository;
        _notifier = notifier;
        _currentUser = currentUser;
    }

    // ─────────────────────────────────────────────────────────
    // Queries
    // ─────────────────────────────────────────────────────────

    public async Task<(int Count, List<CourseResponse> Items)> GetListAsync(ODataQueryOptions<Course> queryOptions)
    {
        // AsSplitQuery tránh cartesian explosion khi có nhiều Include.
        // AppendQueryOptionsAsync đã tự áp dụng AsNoTracking.
        var queryable = _courseRepository.Query()
            .Where(x => !x.IsDeleted && x.IsActive)
            .Include(c => c.CourseLanguages).ThenInclude(cl => cl.Language)
            .Include(c => c.CourseFrameworks).ThenInclude(cf => cf.Framework)
            .AsSplitQuery();

        var (count, courses) = await queryable.AppendQueryOptionsAsync(queryOptions);

        return (count, courses.Select(c => new CourseResponse(c)).ToList());
    }

    public async Task<CourseDetailResponse?> GetByIdAsync(long id)
    {
        var course = await _courseRepository.Query()
            .AsNoTracking()
            .Include(c => c.CourseTags)
            .Include(c => c.CourseLanguages).ThenInclude(cl => cl.Language)
            .Include(c => c.CourseFrameworks).ThenInclude(cf => cf.Framework)
            .AsSplitQuery()
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);

        return course == null ? null : new CourseDetailResponse(course);
    }

    public async Task<List<CourseSuggestionResponse>> SuggestAsync(string? keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
            return new List<CourseSuggestionResponse>();

        // EF.Functions.ILike là case-insensitive match native của PostgreSQL —
        // dùng được index, không phải .ToLower() cả bảng.
        var pattern = $"%{keyword}%";

        return await _courseRepository.Query()
            .AsNoTracking()
            .Where(x => !x.IsDeleted && x.IsActive && EF.Functions.ILike(x.Title, pattern))
            .OrderBy(x => x.Title)
            .Select(x => new CourseSuggestionResponse
            {
                Id = x.Id,
                Title = x.Title,
                ImageUrl = x.ImageUrl
            })
            .Take(10)
            .ToListAsync();
    }

    // ─────────────────────────────────────────────────────────
    // Commands
    // ─────────────────────────────────────────────────────────

    public async Task<ServiceResult<CourseResponse>> CreateAsync(CourseRequest model)
    {
        var (valid, errors) = await model.ValidateCourseAsync(_courseRepository);
        if (!valid)
            return ServiceResult<CourseResponse>.Invalid(errors);

        var entity = model.GetCourse();
        _courseRepository.Add(entity);
        await _courseRepository.SaveChangesAsync();

        // Sync các bảng liên kết sau khi đã có course id.
        // isNew: course vừa tạo chưa có link nào, nên list rỗng = không cần chạy gì.
        await SyncRelationsAsync(entity, model, isNew: true);

        await _notifier.NotifyAllAsync(
            $"🎓 Khoá học mới: {entity.Title}",
            NotificationType.Info,
            $"/courses/{entity.Id}/learn",
            actorId: _currentUser.GetCurrentUserId());

        return ServiceResult<CourseResponse>.Ok(new CourseResponse(entity));
    }

    public async Task<ServiceResult<CourseResponse>> UpdateAsync(long id, CourseRequest model)
    {
        var entity = await _courseRepository.Query()
            .Include(c => c.CourseTags)
            .Include(c => c.CourseLanguages)
            .Include(c => c.CourseFrameworks)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        if (entity == null)
            return ServiceResult<CourseResponse>.NotFound();

        var (valid, errors) = await model.ValidateCourseAsync(_courseRepository, id);
        if (!valid)
            return ServiceResult<CourseResponse>.Invalid(errors);

        model.ToEntity(entity);
        await _courseRepository.SaveChangesAsync();

        await SyncRelationsAsync(entity, model, isNew: false);

        return ServiceResult<CourseResponse>.Ok(new CourseResponse(entity));
    }

    public async Task<int> SoftDeleteAsync(List<long> ids)
    {
        var now = DateTime.UtcNow;

        return await _courseRepository.Query()
            .Where(x => ids.Contains(x.Id) && !x.IsDeleted)
            .ExecuteUpdateAsync(s => s
                .SetProperty(c => c.IsDeleted, true)
                .SetProperty(c => c.IsActive, false)
                .SetProperty(c => c.UpdatedAt, now));
    }

    public async Task<int> SetActiveAsync(List<long> ids, bool isActive)
    {
        var now = DateTime.UtcNow;

        return await _courseRepository.Query()
            .Where(x => ids.Contains(x.Id) && !x.IsDeleted)
            .ExecuteUpdateAsync(s => s
                .SetProperty(c => c.IsActive, isActive)
                .SetProperty(c => c.UpdatedAt, now));
    }

    // ─────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────

    /// <summary>
    /// Đồng bộ tag / language / framework của course. null = "không thay đổi",
    /// list rỗng = "xoá hết". Với course vừa tạo (isNew) thì list rỗng không cần chạy
    /// vì chưa có link nào để xoá — bỏ qua để tiết kiệm round-trip xuống DB.
    /// </summary>
    private async Task SyncRelationsAsync(Course entity, CourseRequest model, bool isNew)
    {
        bool ShouldSync(List<long>? ids) => ids != null && (!isNew || ids.Count > 0);

        if (ShouldSync(model.TagIds))
            await entity.SyncTagsAsync(_courseTagRepository, _tagRepository, model.TagIds);

        if (ShouldSync(model.LanguageIds))
            await entity.SyncLanguagesAsync(_courseLanguageRepository, _languageRepository, model.LanguageIds);

        if (ShouldSync(model.FrameworkIds))
            await entity.SyncFrameworksAsync(_courseFrameworkRepository, _frameworkRepository, model.FrameworkIds);
    }
}
