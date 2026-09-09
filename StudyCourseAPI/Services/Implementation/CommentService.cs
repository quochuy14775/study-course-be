using Microsoft.EntityFrameworkCore;
using StudyCourseAPI.DTOs.Requests;
using StudyCourseAPI.DTOs.Responses;
using StudyCourseAPI.Enums;
using StudyCourseAPI.Extensions;
using StudyCourseAPI.Models;
using StudyCourseAPI.Repositories;

namespace StudyCourseAPI.Services;

public class CommentService : ICommentService
{
    private readonly IRepository<LessonComment> _commentRepository;
    private readonly IRepository<CommentLike> _commentLikeRepository;
    private readonly IRepository<Lesson> _lessonRepository;
    private readonly INotificationService _notifier;
    private readonly ICurrentUser _currentUser;

    public CommentService(
        IRepository<LessonComment> commentRepository,
        IRepository<CommentLike> commentLikeRepository,
        IRepository<Lesson> lessonRepository,
        INotificationService notifier,
        ICurrentUser currentUser)
    {
        _commentRepository = commentRepository;
        _commentLikeRepository = commentLikeRepository;
        _lessonRepository = lessonRepository;
        _notifier = notifier;
        _currentUser = currentUser;
    }

    public async Task<List<CommentResponse>> GetByLessonAsync(long lessonId)
    {
        var userId = _currentUser.GetCurrentUserId();

        var comments = await _commentRepository.Query()
            .AsNoTracking()
            .Where(c => c.LessonId == lessonId && c.ParentCommentId == null && !c.IsDeleted)
            .Include(c => c.User)
            .Include(c => c.Likes)
            .Include(c => c.Replies).ThenInclude(r => r.User)
            .Include(c => c.Replies).ThenInclude(r => r.Likes)
            .AsSplitQuery()
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        return comments.Select(c => new CommentResponse(c, userId)).ToList();
    }

    public async Task<ServiceResult<CommentResponse>> CreateAsync(long lessonId, CommentRequest model)
    {
        var lessonExists = await _lessonRepository.Query()
            .AnyAsync(l => l.Id == lessonId && !l.IsDeleted);

        if (!lessonExists)
            return ServiceResult<CommentResponse>.NotFound("Lesson not found.");

        var (valid, errors) = await model.ValidateCommentAsync(_commentRepository, lessonId);
        if (!valid)
            return ServiceResult<CommentResponse>.Invalid(errors);

        var userId = _currentUser.GetCurrentUserId();
        var entity = model.GetComment(lessonId, userId);

        _commentRepository.Add(entity);
        await _commentRepository.SaveChangesAsync();

        var created = await _commentRepository.Query()
            .Include(c => c.User)
            .Include(c => c.Likes)
            .Include(c => c.Replies)
            .FirstOrDefaultAsync(c => c.Id == entity.Id);

        // Là reply → báo cho chủ comment cha
        if (model.ParentCommentId.HasValue)
        {
            var parent = await _commentRepository.Query()
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.Id == model.ParentCommentId.Value && !c.IsDeleted);

            if (parent is not null)
            {
                var courseId = await GetCourseIdAsync(lessonId);
                var actorName = created?.User?.FullName ?? created?.User?.UserName ?? "Một học viên";

                await _notifier.NotifyAsync(
                    parent.UserId,
                    $"{actorName} đã trả lời bình luận của bạn",
                    NotificationType.Info,
                    $"/courses/{courseId}/learn/{lessonId}",
                    actorId: userId);
            }
        }

        return ServiceResult<CommentResponse>.Ok(new CommentResponse(created!, userId));
    }

    public async Task<bool> SoftDeleteAsync(long lessonId, long commentId)
    {
        var userId = _currentUser.GetCurrentUserId();

        var entity = await _commentRepository.Query()
            .FirstOrDefaultAsync(c =>
                c.Id == commentId && c.LessonId == lessonId && c.UserId == userId && !c.IsDeleted);

        if (entity == null) return false;

        entity.IsDeleted = true;
        await _commentRepository.SaveChangesAsync();

        return true;
    }

    public async Task<(bool Liked, int LikeCount)?> ToggleLikeAsync(long lessonId, long commentId)
    {
        var userId = _currentUser.GetCurrentUserId();

        var entity = await _commentRepository.Query()
            .Include(c => c.Likes)
            .FirstOrDefaultAsync(c => c.Id == commentId && c.LessonId == lessonId && !c.IsDeleted);

        if (entity == null) return null;

        var existingLike = entity.Likes.FirstOrDefault(l => l.UserId == userId);
        var wasLike = existingLike == null;

        if (existingLike != null)
        {
            _commentLikeRepository.Remove(existingLike);
            entity.LikeCount = Math.Max(0, entity.LikeCount - 1);
        }
        else
        {
            _commentLikeRepository.Add(new CommentLike { CommentId = commentId, UserId = userId });
            entity.LikeCount++;
        }

        await _commentRepository.SaveChangesAsync();

        // Chỉ báo khi vừa like, không báo khi bỏ like
        if (wasLike)
        {
            var courseId = await GetCourseIdAsync(lessonId);

            await _notifier.NotifyAsync(
                entity.UserId,
                "Có người vừa thích bình luận của bạn ❤️",
                NotificationType.Success,
                $"/courses/{courseId}/learn/{lessonId}",
                actorId: userId);
        }

        return (wasLike, entity.LikeCount);
    }

    private Task<long> GetCourseIdAsync(long lessonId)
        => _lessonRepository.Query()
            .Where(l => l.Id == lessonId)
            .Select(l => l.CourseId)
            .FirstOrDefaultAsync();
}
