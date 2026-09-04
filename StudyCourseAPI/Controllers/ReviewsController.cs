using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudyCourseAPI.DTOs.Requests;
using StudyCourseAPI.DTOs.Responses;
using StudyCourseAPI.Enums;
using StudyCourseAPI.Extensions;
using StudyCourseAPI.Models;
using StudyCourseAPI.Repositories;
using StudyCourseAPI.Services;

namespace StudyCourseAPI.Controllers
{
    [Route("api/courses/{courseId:long}/reviews")]
    [ApiController]
    [Authorize]
    public class ReviewsController : BaseController<CourseReview>
    {
        private readonly IRepository<CourseReviewReply> _replyRepository;
        private readonly IRepository<CourseReviewHelpful> _helpfulRepository;
        private readonly IRepository<Course> _courseRepository;
        private readonly IRepository<Role> _roleRepository;
        private readonly IRepository<UserRole> _userRoleRepository;
        private readonly INotificationService _notifier;

        public ReviewsController(
            IRepository<CourseReview> baseRepository,
            IRepository<CourseReviewReply> replyRepository,
            IRepository<CourseReviewHelpful> helpfulRepository,
            IRepository<Course> courseRepository,
            IRepository<Role> roleRepository,
            IRepository<UserRole> userRoleRepository,
            INotificationService notifier,
            ICurrentUser currentUser)
            : base(baseRepository, currentUser)
        {
            _replyRepository = replyRepository;
            _helpfulRepository = helpfulRepository;
            _courseRepository = courseRepository;
            _roleRepository = roleRepository;
            _userRoleRepository = userRoleRepository;
            _notifier = notifier;
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetAll(long courseId)
        {
            var userId = _currentUser.GetCurrentUserId();

            var reviews = await _baseRepository.Query()
                .AsNoTracking()
                .Where(r => r.CourseId == courseId && !r.IsDeleted)
                .Include(r => r.User)
                .Include(r => r.Helpfuls)
                .Include(r => r.Replies).ThenInclude(x => x.User)
                .AsSplitQuery()
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            var instructorIds = await GetInstructorIdsAsync();

            return Ok(reviews.Select(r => new ReviewResponse(r, userId, instructorIds)));
        }

        [AllowAnonymous]
        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary(long courseId)
        {
            var ratings = await _baseRepository.Query()
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

            return Ok(response);
        }

        [HttpPost]
        public async Task<IActionResult> Post(long courseId, [FromBody] ReviewRequest model)
        {
            var courseExists = await _courseRepository.Query()
                .AnyAsync(c => c.Id == courseId && !c.IsDeleted);
            if (!courseExists)
                return NotFound(new { message = "Course not found." });

            var userId = _currentUser.GetCurrentUserId();

            var (success, errors) = await model.ValidateReviewAsync(_baseRepository, courseId, userId);
            if (!success)
                return this.ValidationFailed(errors);

            var entity = model.GetReview(courseId, userId);

            _baseRepository.Add(entity);
            await _baseRepository.SaveChangesAsync();

            await _courseRepository.RefreshReviewStatsAsync(_baseRepository, courseId);
            await _courseRepository.SaveChangesAsync();

            var created = await _baseRepository.Query()
                .Include(r => r.User)
                .Include(r => r.Helpfuls)
                .Include(r => r.Replies)
                .FirstOrDefaultAsync(r => r.Id == entity.Id);

            var instructorIds = await GetInstructorIdsAsync();

            return CreatedAtAction(nameof(GetAll), new { courseId }, new ReviewResponse(created!, userId, instructorIds));
        }

        [HttpDelete("{reviewId:long}")]
        public async Task<IActionResult> Delete(long courseId, long reviewId)
        {
            var userId = _currentUser.GetCurrentUserId();

            var entity = await _baseRepository.Query()
                .FirstOrDefaultAsync(r => r.Id == reviewId && r.CourseId == courseId && r.UserId == userId && !r.IsDeleted);

            if (entity == null) return NotFound();

            entity.IsDeleted = true;
            await _baseRepository.SaveChangesAsync();

            await _courseRepository.RefreshReviewStatsAsync(_baseRepository, courseId);
            await _courseRepository.SaveChangesAsync();

            return Ok(new { success = true });
        }

        [HttpPost("{reviewId:long}/helpful")]
        public async Task<IActionResult> ToggleHelpful(long courseId, long reviewId)
        {
            var userId = _currentUser.GetCurrentUserId();

            var entity = await _baseRepository.Query()
                .Include(r => r.Helpfuls)
                .FirstOrDefaultAsync(r => r.Id == reviewId && r.CourseId == courseId && !r.IsDeleted);

            if (entity == null) return NotFound();

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

            await _baseRepository.SaveChangesAsync();

            if (wasMarked)
            {
                await _notifier.NotifyAsync(
                    entity.UserId,
                    "Có người vừa đánh dấu đánh giá của bạn là hữu ích",
                    NotificationType.Success,
                    $"/courses/{courseId}",
                    actorId: userId);
            }

            return Ok(new { markedHelpful = wasMarked, helpfulCount = entity.HelpfulCount });
        }

        [HttpPost("{reviewId:long}/replies")]
        public async Task<IActionResult> AddReply(long courseId, long reviewId, [FromBody] ReviewReplyRequest model)
        {
            var review = await _baseRepository.Query()
                .FirstOrDefaultAsync(r => r.Id == reviewId && r.CourseId == courseId && !r.IsDeleted);

            if (review == null) return NotFound();

            var (success, errors) = model.ValidateReply();
            if (!success)
                return this.ValidationFailed(errors);

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

            return CreatedAtAction(nameof(GetAll), new { courseId }, new ReviewReplyResponse(created!, instructorIds));
        }

        // ─────────────────────────────────────────────────────────
        // helpers
        // ─────────────────────────────────────────────────────────
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
    }
}
