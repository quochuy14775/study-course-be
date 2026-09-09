using Microsoft.EntityFrameworkCore;
using StudyCourseAPI.Enums;
using StudyCourseAPI.Models;
using StudyCourseAPI.Repositories;

namespace StudyCourseAPI.Services;

public class AnswerService : IAnswerService
{
    private readonly IRepository<QuestionAnswer> _answerRepository;
    private readonly IRepository<AnswerLike> _answerLikeRepository;
    private readonly INotificationService _notifier;
    private readonly ICurrentUser _currentUser;

    public AnswerService(
        IRepository<QuestionAnswer> answerRepository,
        IRepository<AnswerLike> answerLikeRepository,
        INotificationService notifier,
        ICurrentUser currentUser)
    {
        _answerRepository = answerRepository;
        _answerLikeRepository = answerLikeRepository;
        _notifier = notifier;
        _currentUser = currentUser;
    }

    public async Task<(bool Liked, int LikeCount)?> ToggleLikeAsync(long answerId)
    {
        var userId = _currentUser.GetCurrentUserId();

        var entity = await _answerRepository.Query()
            .Include(a => a.Likes)
            .Include(a => a.User)
            .FirstOrDefaultAsync(a => a.Id == answerId && !a.IsDeleted);

        if (entity == null) return null;

        var existingLike = entity.Likes.FirstOrDefault(l => l.UserId == userId);
        var wasLike = existingLike == null;

        if (existingLike != null)
        {
            _answerLikeRepository.Remove(existingLike);
            entity.LikeCount = Math.Max(0, entity.LikeCount - 1);
        }
        else
        {
            _answerLikeRepository.Add(new AnswerLike { AnswerId = answerId, UserId = userId });
            entity.LikeCount++;
        }

        await _answerRepository.SaveChangesAsync();

        // Chỉ báo khi vừa like, không báo khi bỏ like
        if (wasLike)
        {
            await _notifier.NotifyAsync(
                entity.UserId,
                "Có người vừa thích câu trả lời của bạn",
                NotificationType.Success,
                null,
                actorId: userId);
        }

        return (wasLike, entity.LikeCount);
    }

    public async Task<ServiceResult<bool>> ToggleAcceptedAsync(long answerId)
    {
        var userId = _currentUser.GetCurrentUserId();

        var entity = await _answerRepository.Query()
            .Include(a => a.Question)
            .FirstOrDefaultAsync(a => a.Id == answerId && !a.IsDeleted);

        if (entity == null)
            return ServiceResult<bool>.NotFound();

        // Chỉ chủ câu hỏi mới được chọn đáp án
        if (entity.Question.UserId != userId)
            return ServiceResult<bool>.Forbidden();

        // Mỗi câu hỏi chỉ có tối đa 1 đáp án được chấp nhận
        var siblings = await _answerRepository.Query()
            .Where(a => a.QuestionId == entity.QuestionId && a.Id != answerId && !a.IsDeleted)
            .ToListAsync();

        foreach (var s in siblings) s.IsAcceptedAnswer = false;

        entity.IsAcceptedAnswer = !entity.IsAcceptedAnswer;

        if (entity.IsAcceptedAnswer)
            entity.Question.IsResolved = true;

        await _answerRepository.SaveChangesAsync();

        if (entity.IsAcceptedAnswer)
        {
            await _notifier.NotifyAsync(
                entity.UserId,
                "🎉 Câu trả lời của bạn đã được chấp nhận!",
                NotificationType.Success,
                null,
                actorId: userId);
        }

        return ServiceResult<bool>.Ok(entity.IsAcceptedAnswer);
    }

    public async Task<bool> SoftDeleteAsync(long answerId)
    {
        var userId = _currentUser.GetCurrentUserId();

        var entity = await _answerRepository.Query()
            .Include(a => a.Question)
            .FirstOrDefaultAsync(a => a.Id == answerId && a.UserId == userId && !a.IsDeleted);

        if (entity == null) return false;

        entity.IsDeleted = true;
        entity.Question.AnswerCount = Math.Max(0, entity.Question.AnswerCount - 1);
        await _answerRepository.SaveChangesAsync();

        return true;
    }
}
