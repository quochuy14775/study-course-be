using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudyCourseAPI.DTOs.Requests;
using StudyCourseAPI.DTOs.Responses;
using StudyCourseAPI.Extensions;
using StudyCourseAPI.Enums;
using StudyCourseAPI.Models;
using StudyCourseAPI.Repositories;
using StudyCourseAPI.Services;

namespace StudyCourseAPI.Controllers.UserController
{
    [Route("api/lessons/{lessonId:long}/comments")]
    [ApiController]
    [Authorize]
    public class CommentsController : BaseController<LessonComment>
    {
        private readonly IRepository<CommentLike> _commentLikeRepository;
        private readonly IRepository<Lesson> _lessonRepository;
        private readonly INotificationService _notifier;

        public CommentsController(
            IRepository<LessonComment> baseRepository,
            IRepository<CommentLike> commentLikeRepository,
            IRepository<Lesson> lessonRepository,
            INotificationService notifier,
            ICurrentUser currentUser)
            : base(baseRepository, currentUser)
        {
            _commentLikeRepository = commentLikeRepository;
            _lessonRepository = lessonRepository;
            _notifier = notifier;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(long lessonId)
        {
            var userId = _currentUser.GetCurrentUserId();

            var comments = await _baseRepository.Query()
                .AsNoTracking()
                .Where(c => c.LessonId == lessonId && c.ParentCommentId == null && !c.IsDeleted)
                .Include(c => c.User)
                .Include(c => c.Likes)
                .Include(c => c.Replies).ThenInclude(r => r.User)
                .Include(c => c.Replies).ThenInclude(r => r.Likes)
                .AsSplitQuery()
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            return Ok(comments.Select(c => new CommentResponse(c, userId)));
        }

        [HttpPost]
        public async Task<IActionResult> Post(long lessonId, [FromBody] CommentRequest model)
        {
            var lessonExists = await _lessonRepository.Query()
                .AnyAsync(l => l.Id == lessonId && !l.IsDeleted);
            if (!lessonExists)
                return NotFound(new { message = "Lesson not found." });

            var (success, errors) = await model.ValidateCommentAsync(_baseRepository, lessonId);
            if (!success)
                return BadRequest(new { status = 400, message = "Validation failed", errors = FlattenErrors(errors) });

            var userId = _currentUser.GetCurrentUserId();
            var entity = model.GetComment(lessonId, userId);

            _baseRepository.Add(entity);
            await _baseRepository.SaveChangesAsync();

            var created = await _baseRepository.Query()
                .Include(c => c.User)
                .Include(c => c.Likes)
                .Include(c => c.Replies)
                .FirstOrDefaultAsync(c => c.Id == entity.Id);

            // Notify parent commenter on reply
            if (model.ParentCommentId.HasValue)
            {
                var parent = await _baseRepository.Query()
                    .Include(c => c.User)
                    .FirstOrDefaultAsync(c => c.Id == model.ParentCommentId.Value && !c.IsDeleted);
                if (parent is not null)
                {
                    var courseId = await _lessonRepository.Query()
                        .Where(l => l.Id == lessonId).Select(l => l.CourseId).FirstOrDefaultAsync();
                    var actorName = created?.User?.FullName ?? created?.User?.UserName ?? "Một học viên";
                    await _notifier.NotifyAsync(
                        parent.UserId,
                        $"{actorName} đã trả lời bình luận của bạn",
                        NotificationType.Info,
                        $"/courses/{courseId}/learn/{lessonId}",
                        actorId: userId);
                }
            }

            return CreatedAtAction(nameof(GetAll), new { lessonId }, new CommentResponse(created!, userId));
        }

        [HttpDelete("{commentId:long}")]
        public async Task<IActionResult> Delete(long lessonId, long commentId)
        {
            var userId = _currentUser.GetCurrentUserId();

            var entity = await _baseRepository.Query()
                .FirstOrDefaultAsync(c => c.Id == commentId && c.LessonId == lessonId && c.UserId == userId && !c.IsDeleted);

            if (entity == null) return NotFound();

            entity.IsDeleted = true;
            entity.UpdatedAt = DateTime.UtcNow;
            await _baseRepository.SaveChangesAsync();

            return Ok(new { success = true });
        }

        [HttpPost("{commentId:long}/like")]
        public async Task<IActionResult> ToggleLike(long lessonId, long commentId)
        {
            var userId = _currentUser.GetCurrentUserId();

            var entity = await _baseRepository.Query()
                .Include(c => c.Likes)
                .FirstOrDefaultAsync(c => c.Id == commentId && c.LessonId == lessonId && !c.IsDeleted);

            if (entity == null) return NotFound();

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

            await _baseRepository.SaveChangesAsync();

            // Notify comment owner when liked
            if (wasLike)
            {
                var courseId = await _lessonRepository.Query()
                    .Where(l => l.Id == lessonId).Select(l => l.CourseId).FirstOrDefaultAsync();
                await _notifier.NotifyAsync(
                    entity.UserId,
                    "Có người vừa thích bình luận của bạn ❤️",
                    NotificationType.Success,
                    $"/courses/{courseId}/learn/{lessonId}",
                    actorId: userId);
            }

            return Ok(new { liked = wasLike, likeCount = entity.LikeCount });
        }

        private static Dictionary<string, object> FlattenErrors(Dictionary<string, List<string>>? errors)
        {
            var result = new Dictionary<string, object>();
            if (errors == null) return result;
            foreach (var kv in errors)
            {
                if (kv.Value == null || kv.Value.Count == 0) continue;
                result[kv.Key] = kv.Value.Count == 1 ? kv.Value[0] : (object)kv.Value;
            }
            return result;
        }
    }
}
