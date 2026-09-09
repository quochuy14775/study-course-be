using Microsoft.EntityFrameworkCore;
using StudyCourseAPI.Extensions;
using StudyCourseAPI.Models;
using StudyCourseAPI.Repositories;

namespace StudyCourseAPI.Services;

public class LessonProgressService : ILessonProgressService
{
    private readonly IRepository<UserLessonProgress> _progressRepository;
    private readonly IRepository<Lesson> _lessonRepository;
    private readonly IRepository<UserCourse> _userCourseRepository;
    private readonly ICurrentUser _currentUser;

    public LessonProgressService(
        IRepository<UserLessonProgress> progressRepository,
        IRepository<Lesson> lessonRepository,
        IRepository<UserCourse> userCourseRepository,
        ICurrentUser currentUser)
    {
        _progressRepository = progressRepository;
        _lessonRepository = lessonRepository;
        _userCourseRepository = userCourseRepository;
        _currentUser = currentUser;
    }

    public async Task<bool> MarkCompleteAsync(long lessonId)
    {
        var lesson = await _lessonRepository.Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == lessonId && !l.IsDeleted);

        if (lesson == null) return false;

        var userId = _currentUser.GetCurrentUserId();
        var now = DateTime.UtcNow;

        var progress = await _progressRepository.Query()
            .FirstOrDefaultAsync(p => p.LessonId == lessonId && p.UserId == userId);

        if (progress == null)
        {
            progress = new UserLessonProgress
            {
                UserId = userId,
                LessonId = lessonId,
                CourseId = lesson.CourseId,
                IsCompleted = true,
                CompletedAt = now,
                LastWatchedAt = now,
                IsActive = true,
            };
            _progressRepository.Add(progress);
        }
        else if (!progress.IsCompleted)
        {
            progress.IsCompleted = true;
            progress.CompletedAt = now;
            progress.LastWatchedAt = now;
        }

        // Lưu progress trước rồi mới tính lại % — SyncProgressAsync đếm bằng query xuống DB
        // nên sẽ không thấy row vừa Add nếu chưa flush.
        await _progressRepository.SaveChangesAsync();

        // User có thể vào thẳng /courses/{id}/learn qua deep link mà chưa bấm "Học ngay",
        // nên ghi danh ngầm ở đây để không sinh ra progress mồ côi (không thuộc khóa nào).
        await _userCourseRepository.SyncProgressAsync(
            _progressRepository, _lessonRepository, lesson.CourseId, userId);
        await _userCourseRepository.SaveChangesAsync();

        return true;
    }

    public Task<List<long>> GetCompletedLessonIdsAsync(long courseId)
    {
        var userId = _currentUser.GetCurrentUserId();

        return _progressRepository.Query()
            .AsNoTracking()
            .Where(p => p.UserId == userId && p.CourseId == courseId && p.IsCompleted)
            .Select(p => p.LessonId)
            .ToListAsync();
    }
}
