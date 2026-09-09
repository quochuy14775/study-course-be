using Microsoft.EntityFrameworkCore;
using StudyCourseAPI.DTOs.Requests;
using StudyCourseAPI.DTOs.Responses;
using StudyCourseAPI.Enums;
using StudyCourseAPI.Extensions;
using StudyCourseAPI.Models;
using StudyCourseAPI.Repositories;

namespace StudyCourseAPI.Services;

public class ReviewService : IReviewService
{
    private readonly IRepository<CourseReview> _reviewRepository;
    private readonly IRepository<CourseReviewReply> _replyRepository;
    private readonly IRepository<CourseReviewHelpful> _helpfulRepository;
    private readonly IRepository<Course> _courseRepository;
    private readonly IRepository<Role> _roleRepository;
    private readonly IRepository<UserRole> _userRoleRepository;
    private readonly INotificationService _notifier;
    private readonly ICurrentUser _currentUser;

    public ReviewService(
        IRepository<CourseReview> reviewRepository,
        IRepository<CourseReviewReply> replyRepository,
        IRepository<CourseReviewHelpful> helpfulRepository,
        IRepository<Course> courseRepository,
        IRepository<Role> roleRepository,
        IRepository<UserRole> userRoleRepository,
        INotificationService notifier,
        ICurrentUser currentUser)
    {
        _reviewRepository = reviewRepository;
        _replyRepository = replyRepository;
        _helpfulRepository = helpfulRepository;
        _courseRepository = courseRepository;
        _roleRepository = roleRepository;
        _userRoleRepository = userRoleRepository;
        _notifier = notifier;
        _currentUser = currentUser;
    }

    // ─────────────────────────────────────────────────────────
    // Queries
    // ─────────────────────────────────────────────────────────

    public async Task<List<ReviewResponse>> GetByCourseAsync(long courseId)
    {
        var userId = _currentUser.GetCurrentUserId();

        var reviews = await _reviewRepository.Query()
            .AsNoTracking()
            .Where(r => r.CourseId == courseId && !r.IsDeleted)
            .Include(r => r.User)
            .Include(r => r.Helpfuls)
            .Include(r => r.Replies).ThenInclude(x => x.User)
            .AsSplitQuery()
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        var instructorIds = await GetInstructorIdsAsync();

        return reviews.Select(r => new ReviewResponse(r, userId, instructorIds)).ToList();
    }

    public async Task<RatingBreakdownResponse> GetSummaryAsync(long courseId)
    {
        var ratings = await _reviewRepository.Query()
            .AsNoTracking()
            .Where(r => r.CourseId == courseId && !r.IsDeleted)
            .Select(r => r.Rating)
            .ToListAsync();

        var response = new RatingBreakdownResponse
        {
            Total = ratings.Count,
            Average = ratings.Count > 0 ? Math.Round(ratings.Average(), 1) : 0,
        };

        foreach (var r in ratings)
            response.Distribution[r]++;

        return response;
    }

    // ─────────────────────────────────────────────────────────
    // Commands
    // ─────────────────────────────────────────────────────────

    public async Task<ServiceResult<ReviewResponse>> CreateAsync(long courseId, ReviewRequest model)
    {
        var courseExists = await _courseRepository.Query()
            .AnyAsync(c => c.Id == courseId && !c.IsDeleted);

        if (!courseExists)
            return ServiceResult<ReviewResponse>.NotFound("Course not found.");

        var userId = _currentUser.GetCurrentUserId();

        var (valid, errors) = await model.ValidateReviewAsync(_reviewRepository, courseId, userId);
        if (!valid)
            return ServiceResult<ReviewResponse>.Invalid(errors);

        var entity = model.GetReview(courseId, userId);

        _reviewRepository.Add(entity);
        await _reviewRepository.SaveChangesAsync();

        await RefreshReviewStatsAsync(courseId);

        var created = await _reviewRepository.Query()
            .Include(r => r.User)
            .Include(r => r.Helpfuls)
            .Include(r => r.Replies)
            .FirstOrDefaultAsync(r => r.Id == entity.Id);

        var instructorIds = await GetInstructorIdsAsync();

        return ServiceResult<ReviewResponse>.Ok(new ReviewResponse(created!, userId, instructorIds));
    }

    public async Task<bool> SoftDeleteAsync(long courseId, long reviewId)
    {
        var userId = _currentUser.GetCurrentUserId();

        var entity = await _reviewRepository.Query()
            .FirstOrDefaultAsync(r =>
                r.Id == reviewId && r.CourseId == courseId && r.UserId == userId && !r.IsDeleted);

        if (entity == null) return false;

        entity.IsDeleted = true;
        await _reviewRepository.SaveChangesAsync();

        await RefreshReviewStatsAsync(courseId);

        return true;
    }

    public async Task<(bool MarkedHelpful, int HelpfulCount)?> ToggleHelpfulAsync(long courseId, long reviewId)
    {
        var userId = _currentUser.GetCurrentUserId();

        var entity = await _reviewRepository.Query()
            .Include(r => r.Helpfuls)
            .FirstOrDefaultAsync(r => r.Id == reviewId && r.CourseId == courseId && !r.IsDeleted);

        if (entity == null) return null;

        var existing = entity.Helpfuls.FirstOrDefault(h => h.UserId == userId);
        var wasMarked = existing == null;

        if (existing != null)
        {
            _helpfulRepository.Remove(existing);
            entity.HelpfulCount = Math.Max(0, entity.HelpfulCount - 1);
        }
        else
        {
            _helpfulRepository.Add(new CourseReviewHelpful { ReviewId = reviewId, UserId = userId });
            entity.HelpfulCount++;
        }

        await _reviewRepository.SaveChangesAsync();

        // Chỉ báo khi vừa đánh dấu, không báo khi bỏ đánh dấu
        if (wasMarked)
        {
            await _notifier.NotifyAsync(
                entity.UserId,
                "Có người vừa đánh dấu đánh giá của bạn là hữu ích",
                NotificationType.Success,
                $"/courses/{courseId}",
                actorId: userId);
        }

        return (wasMarked, entity.HelpfulCount);
    }

    public async Task<ServiceResult<ReviewReplyResponse>> AddReplyAsync(
        long courseId, long reviewId, ReviewReplyRequest model)
    {
        var review = await _reviewRepository.Query()
            .FirstOrDefaultAsync(r => r.Id == reviewId && r.CourseId == courseId && !r.IsDeleted);

        if (review == null)
            return ServiceResult<ReviewReplyResponse>.NotFound();

        var (valid, errors) = model.ValidateReply();
        if (!valid)
            return ServiceResult<ReviewReplyResponse>.Invalid(errors);

        var userId = _currentUser.GetCurrentUserId();
        var entity = model.GetReply(reviewId, userId);

        _replyRepository.Add(entity);
        await _replyRepository.SaveChangesAsync();

        var created = await _replyRepository.Query()
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == entity.Id);

        var actorName = created?.User?.FullName ?? created?.User?.UserName ?? "Một học viên";

        await _notifier.NotifyAsync(
            review.UserId,
            $"{actorName} đã trả lời đánh giá của bạn",
            NotificationType.Info,
            $"/courses/{courseId}",
            actorId: userId);

        var instructorIds = await GetInstructorIdsAsync();

        return ServiceResult<ReviewReplyResponse>.Ok(new ReviewReplyResponse(created!, instructorIds));
    }

    // ─────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────

    /// <summary>Id của các user có role Admin — dùng để gắn nhãn "giảng viên" lên review/reply.</summary>
    private async Task<HashSet<long>> GetInstructorIdsAsync()
    {
        var adminRoleId = await _roleRepository.Query()
            .Where(r => r.Name == AppRoles.Admin)
            .Select(r => r.Id)
            .FirstOrDefaultAsync();

        if (adminRoleId == 0) return new HashSet<long>();

        var ids = await _userRoleRepository.Query()
            .Where(ur => ur.RoleId == adminRoleId)
            .Select(ur => ur.UserId)
            .ToListAsync();

        return ids.ToHashSet();
    }

    /// <summary>Cập nhật lại Rating / ReviewCount đang cache trên Course.</summary>
    private async Task RefreshReviewStatsAsync(long courseId)
    {
        await _courseRepository.RefreshReviewStatsAsync(_reviewRepository, courseId);
        await _courseRepository.SaveChangesAsync();
    }
}
