using Microsoft.EntityFrameworkCore;
using StudyCourseAPI.DTOs.Requests;
using StudyCourseAPI.DTOs.Responses;
using StudyCourseAPI.Enums;
using StudyCourseAPI.Extensions;
using StudyCourseAPI.Models;
using StudyCourseAPI.Repositories;

namespace StudyCourseAPI.Services;

public class QuestionService : IQuestionService
{
    private readonly IRepository<LessonQuestion> _questionRepository;
    private readonly IRepository<QuestionAnswer> _answerRepository;
    private readonly IRepository<Lesson> _lessonRepository;
    private readonly INotificationService _notifier;
    private readonly ICurrentUser _currentUser;

    public QuestionService(
        IRepository<LessonQuestion> questionRepository,
        IRepository<QuestionAnswer> answerRepository,
        IRepository<Lesson> lessonRepository,
        INotificationService notifier,
        ICurrentUser currentUser)
    {
        _questionRepository = questionRepository;
        _answerRepository = answerRepository;
        _lessonRepository = lessonRepository;
        _notifier = notifier;
        _currentUser = currentUser;
    }

    public async Task<List<QuestionResponse>> GetByLessonAsync(long lessonId)
    {
        var userId = _currentUser.GetCurrentUserId();

        var questions = await _questionRepository.Query()
            .AsNoTracking()
            .Where(q => q.LessonId == lessonId && !q.IsDeleted)
            .Include(q => q.User)
            .Include(q => q.Answers).ThenInclude(a => a.User)
            .Include(q => q.Answers).ThenInclude(a => a.Likes)
            .AsSplitQuery()
            .OrderByDescending(q => q.CreatedAt)
            .ToListAsync();

        return questions.Select(q => new QuestionResponse(q, userId)).ToList();
    }

    public async Task<ServiceResult<QuestionResponse>> CreateAsync(long lessonId, QuestionRequest model)
    {
        var lessonExists = await _lessonRepository.Query()
            .AnyAsync(l => l.Id == lessonId && !l.IsDeleted);

        if (!lessonExists)
            return ServiceResult<QuestionResponse>.NotFound("Lesson not found.");

        var (valid, errors) = model.ValidateQuestion();
        if (!valid)
            return ServiceResult<QuestionResponse>.Invalid(errors);

        var userId = _currentUser.GetCurrentUserId();
        var entity = model.GetQuestion(lessonId, userId);

        _questionRepository.Add(entity);
        await _questionRepository.SaveChangesAsync();

        var created = await _questionRepository.Query()
            .Include(q => q.User)
            .Include(q => q.Answers)
            .FirstOrDefaultAsync(q => q.Id == entity.Id);

        return ServiceResult<QuestionResponse>.Ok(new QuestionResponse(created!, userId));
    }

    public async Task<bool> SoftDeleteAsync(long lessonId, long questionId)
    {
        var entity = await FindOwnQuestionAsync(lessonId, questionId);
        if (entity == null) return false;

        entity.IsDeleted = true;
        await _questionRepository.SaveChangesAsync();

        return true;
    }

    public async Task<bool?> ToggleResolvedAsync(long lessonId, long questionId)
    {
        var entity = await FindOwnQuestionAsync(lessonId, questionId);
        if (entity == null) return null;

        entity.IsResolved = !entity.IsResolved;
        await _questionRepository.SaveChangesAsync();

        return entity.IsResolved;
    }

    public async Task<ServiceResult<AnswerResponse>> AddAnswerAsync(
        long lessonId, long questionId, AnswerRequest model)
    {
        var question = await _questionRepository.Query()
            .FirstOrDefaultAsync(q => q.Id == questionId && q.LessonId == lessonId && !q.IsDeleted);

        if (question == null)
            return ServiceResult<AnswerResponse>.NotFound();

        var (valid, errors) = model.ValidateAnswer();
        if (!valid)
            return ServiceResult<AnswerResponse>.Invalid(errors);

        var userId = _currentUser.GetCurrentUserId();
        var entity = model.GetAnswer(questionId, userId);

        _answerRepository.Add(entity);
        question.AnswerCount++;
        // Hai repository dùng chung 1 DbContext nên 1 lần SaveChanges là đủ cho cả hai
        await _questionRepository.SaveChangesAsync();

        var created = await _answerRepository.Query()
            .Include(a => a.User)
            .Include(a => a.Likes)
            .FirstOrDefaultAsync(a => a.Id == entity.Id);

        var courseId = await _lessonRepository.Query()
            .Where(l => l.Id == lessonId)
            .Select(l => l.CourseId)
            .FirstOrDefaultAsync();

        var actorName = created?.User?.FullName ?? created?.User?.UserName ?? "Một học viên";

        await _notifier.NotifyAsync(
            question.UserId,
            $"{actorName} đã trả lời câu hỏi của bạn",
            NotificationType.Info,
            $"/courses/{courseId}/learn/{lessonId}",
            actorId: userId);

        return ServiceResult<AnswerResponse>.Ok(new AnswerResponse(created!, userId));
    }

    /// <summary>Lấy câu hỏi nhưng chỉ khi nó thuộc về user đang đăng nhập.</summary>
    private Task<LessonQuestion?> FindOwnQuestionAsync(long lessonId, long questionId)
    {
        var userId = _currentUser.GetCurrentUserId();

        return _questionRepository.Query()
            .FirstOrDefaultAsync(q =>
                q.Id == questionId &&
                q.LessonId == lessonId &&
                q.UserId == userId &&
                !q.IsDeleted);
    }
}
