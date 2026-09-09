using Microsoft.EntityFrameworkCore;
using StudyCourseAPI.DTOs.Requests.Admin;
using StudyCourseAPI.DTOs.Responses.Admin;
using StudyCourseAPI.Enums;
using StudyCourseAPI.Extensions;
using StudyCourseAPI.Models;
using StudyCourseAPI.Repositories;

namespace StudyCourseAPI.Services;

public class QuizManagementService : IQuizManagementService
{
    private readonly IRepository<Quiz> _quizRepository;
    private readonly IRepository<Lesson> _lessonRepository;
    private readonly IRepository<Course> _courseRepository;

    public QuizManagementService(
        IRepository<Quiz> quizRepository,
        IRepository<Lesson> lessonRepository,
        IRepository<Course> courseRepository)
    {
        _quizRepository = quizRepository;
        _lessonRepository = lessonRepository;
        _courseRepository = courseRepository;
    }

    // ─────────────────────────────────────────────────────────
    // Lesson quiz
    // ─────────────────────────────────────────────────────────

    public async Task<QuizResponse?> GetLessonQuizAsync(long lessonId)
    {
        var quiz = await LessonQuizQuery(lessonId)
            .AsNoTracking()
            .Include(q => q.Questions)
            .FirstOrDefaultAsync();

        return quiz == null ? null : new QuizResponse(quiz);
    }

    public async Task<ServiceResult<QuizResponse>> UpsertLessonQuizAsync(long lessonId, QuizRequest model)
    {
        var lesson = await _lessonRepository.Query()
            .FirstOrDefaultAsync(l => l.Id == lessonId && !l.IsDeleted);

        if (lesson == null)
            return ServiceResult<QuizResponse>.NotFound("Lesson not found.");

        var (valid, errors) = model.ValidateQuiz();
        if (!valid)
            return ServiceResult<QuizResponse>.Invalid(errors);

        var entity = await LessonQuizQuery(lessonId)
            .Include(q => q.Questions)
            .FirstOrDefaultAsync();

        if (entity == null)
        {
            entity = model.ToEntity(QuizType.Lesson, lesson.CourseId, lessonId);
            _quizRepository.Add(entity);
        }
        else
        {
            model.MapTo(entity);
        }

        await _quizRepository.SaveChangesAsync();

        return ServiceResult<QuizResponse>.Ok(new QuizResponse(entity));
    }

    public async Task<bool> DeleteLessonQuizAsync(long lessonId)
    {
        var entity = await LessonQuizQuery(lessonId).FirstOrDefaultAsync();
        if (entity == null) return false;

        entity.IsDeleted = true;
        await _quizRepository.SaveChangesAsync();

        return true;
    }

    // ─────────────────────────────────────────────────────────
    // Course test
    // ─────────────────────────────────────────────────────────

    public async Task<QuizResponse?> GetCourseTestAsync(long courseId)
    {
        var quiz = await CourseTestQuery(courseId)
            .AsNoTracking()
            .Include(q => q.Questions)
            .FirstOrDefaultAsync();

        return quiz == null ? null : new QuizResponse(quiz);
    }

    public async Task<ServiceResult<QuizResponse>> UpsertCourseTestAsync(long courseId, QuizRequest model)
    {
        var course = await _courseRepository.Query()
            .FirstOrDefaultAsync(c => c.Id == courseId && !c.IsDeleted);

        if (course == null)
            return ServiceResult<QuizResponse>.NotFound("Course not found.");

        var (valid, errors) = model.ValidateQuiz();
        if (!valid)
            return ServiceResult<QuizResponse>.Invalid(errors);

        var entity = await CourseTestQuery(courseId)
            .Include(q => q.Questions)
            .FirstOrDefaultAsync();

        if (entity == null)
        {
            entity = model.ToEntity(QuizType.CourseTest, courseId, null);
            _quizRepository.Add(entity);
        }
        else
        {
            model.MapTo(entity);
        }

        await _quizRepository.SaveChangesAsync();

        return ServiceResult<QuizResponse>.Ok(new QuizResponse(entity));
    }

    public async Task<bool> DeleteCourseTestAsync(long courseId)
    {
        var entity = await CourseTestQuery(courseId).FirstOrDefaultAsync();
        if (entity == null) return false;

        entity.IsDeleted = true;
        await _quizRepository.SaveChangesAsync();

        return true;
    }

    // ─────────────────────────────────────────────────────────
    // Helpers — bộ lọc dùng chung, tránh lặp điều kiện ở 6 chỗ
    // ─────────────────────────────────────────────────────────

    private IQueryable<Quiz> LessonQuizQuery(long lessonId)
        => _quizRepository.Query()
            .Where(q => q.LessonId == lessonId && q.QuizType == QuizType.Lesson && !q.IsDeleted);

    private IQueryable<Quiz> CourseTestQuery(long courseId)
        => _quizRepository.Query()
            .Where(q => q.CourseId == courseId && q.QuizType == QuizType.CourseTest && !q.IsDeleted);
}
