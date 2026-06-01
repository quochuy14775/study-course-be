using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudyCourseAPI.DTOs.Requests;
using StudyCourseAPI.DTOs.Responses;
using StudyCourseAPI.Extensions;
using StudyCourseAPI.Models;
using StudyCourseAPI.Repositories;

namespace StudyCourseAPI.Controllers.UserController
{
    [Route("api/lessons/{lessonId:long}/questions")]
    [ApiController]
    [Authorize]
    public class QuestionsController : BaseController<LessonQuestion>
    {
        private readonly IRepository<QuestionAnswer> _answerRepository;
        private readonly IRepository<Lesson> _lessonRepository;

        public QuestionsController(
            IRepository<LessonQuestion> baseRepository,
            IRepository<QuestionAnswer> answerRepository,
            IRepository<Lesson> lessonRepository,
            ICurrentUser currentUser)
            : base(baseRepository, currentUser)
        {
            _answerRepository = answerRepository;
            _lessonRepository = lessonRepository;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(long lessonId)
        {
            var userId = _currentUser.GetCurrentUserId();

            var questions = await _baseRepository.Query()
                .Include(q => q.User)
                .Include(q => q.Answers).ThenInclude(a => a.User)
                .Include(q => q.Answers).ThenInclude(a => a.Likes)
                .Where(q => q.LessonId == lessonId && !q.IsDeleted)
                .OrderByDescending(q => q.CreatedAt)
                .ToListAsync();

            return Ok(questions.Select(q => new QuestionResponse(q, userId)));
        }

        [HttpPost]
        public async Task<IActionResult> Post(long lessonId, [FromBody] QuestionRequest model)
        {
            var lessonExists = await _lessonRepository.Query()
                .AnyAsync(l => l.Id == lessonId && !l.IsDeleted);
            if (!lessonExists)
                return NotFound(new { message = "Lesson not found." });

            var (success, errors) = model.ValidateQuestion();
            if (!success)
                return BadRequest(new { status = 400, message = "Validation failed", errors = FlattenErrors(errors) });

            var userId = _currentUser.GetCurrentUserId();
            var entity = model.GetQuestion(lessonId, userId);

            _baseRepository.Add(entity);
            await _baseRepository.SaveChangesAsync();

            var created = await _baseRepository.Query()
                .Include(q => q.User)
                .Include(q => q.Answers)
                .FirstOrDefaultAsync(q => q.Id == entity.Id);

            return CreatedAtAction(nameof(GetAll), new { lessonId }, new QuestionResponse(created!, userId));
        }

        [HttpDelete("{questionId:long}")]
        public async Task<IActionResult> Delete(long lessonId, long questionId)
        {
            var userId = _currentUser.GetCurrentUserId();

            var entity = await _baseRepository.Query()
                .FirstOrDefaultAsync(q => q.Id == questionId && q.LessonId == lessonId && q.UserId == userId && !q.IsDeleted);

            if (entity == null) return NotFound();

            entity.IsDeleted = true;
            entity.UpdatedAt = DateTime.UtcNow;
            await _baseRepository.SaveChangesAsync();

            return Ok(new { success = true });
        }

        [HttpPost("{questionId:long}/resolve")]
        public async Task<IActionResult> Resolve(long lessonId, long questionId)
        {
            var userId = _currentUser.GetCurrentUserId();

            var entity = await _baseRepository.Query()
                .FirstOrDefaultAsync(q => q.Id == questionId && q.LessonId == lessonId && q.UserId == userId && !q.IsDeleted);

            if (entity == null) return NotFound();

            entity.IsResolved = !entity.IsResolved;
            entity.UpdatedAt = DateTime.UtcNow;
            await _baseRepository.SaveChangesAsync();

            return Ok(new { isResolved = entity.IsResolved });
        }

        [HttpPost("{questionId:long}/answers")]
        public async Task<IActionResult> AddAnswer(long lessonId, long questionId, [FromBody] AnswerRequest model)
        {
            var question = await _baseRepository.Query()
                .FirstOrDefaultAsync(q => q.Id == questionId && q.LessonId == lessonId && !q.IsDeleted);

            if (question == null) return NotFound();

            var (success, errors) = model.ValidateAnswer();
            if (!success)
                return BadRequest(new { status = 400, message = "Validation failed", errors = FlattenErrors(errors) });

            var userId = _currentUser.GetCurrentUserId();
            var entity = model.GetAnswer(questionId, userId);

            _answerRepository.Add(entity);
            question.AnswerCount++;
            await _baseRepository.SaveChangesAsync();

            var created = await _answerRepository.Query()
                .Include(a => a.User)
                .Include(a => a.Likes)
                .FirstOrDefaultAsync(a => a.Id == entity.Id);

            return CreatedAtAction(nameof(GetAll), new { lessonId }, new AnswerResponse(created!, userId));
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

    [Route("api/answers")]
    [ApiController]
    [Authorize]
    public class AnswersController : BaseController<QuestionAnswer>
    {
        private readonly IRepository<AnswerLike> _answerLikeRepository;

        public AnswersController(
            IRepository<QuestionAnswer> baseRepository,
            IRepository<AnswerLike> answerLikeRepository,
            ICurrentUser currentUser)
            : base(baseRepository, currentUser)
        {
            _answerLikeRepository = answerLikeRepository;
        }

        [HttpPost("{answerId:long}/like")]
        public async Task<IActionResult> ToggleLike(long answerId)
        {
            var userId = _currentUser.GetCurrentUserId();

            var entity = await _baseRepository.Query()
                .Include(a => a.Likes)
                .FirstOrDefaultAsync(a => a.Id == answerId && !a.IsDeleted);

            if (entity == null) return NotFound();

            var existingLike = entity.Likes.FirstOrDefault(l => l.UserId == userId);

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

            await _baseRepository.SaveChangesAsync();

            return Ok(new { liked = existingLike == null, likeCount = entity.LikeCount });
        }

        [HttpPost("{answerId:long}/accept")]
        public async Task<IActionResult> Accept(long answerId)
        {
            var userId = _currentUser.GetCurrentUserId();

            var entity = await _baseRepository.Query()
                .Include(a => a.Question)
                .FirstOrDefaultAsync(a => a.Id == answerId && !a.IsDeleted);

            if (entity == null) return NotFound();

            if (entity.Question.UserId != userId) return Forbid();

            var siblings = await _baseRepository.Query()
                .Where(a => a.QuestionId == entity.QuestionId && a.Id != answerId && !a.IsDeleted)
                .ToListAsync();

            foreach (var s in siblings) s.IsAcceptedAnswer = false;

            entity.IsAcceptedAnswer = !entity.IsAcceptedAnswer;
            entity.UpdatedAt = DateTime.UtcNow;

            if (entity.IsAcceptedAnswer)
                entity.Question.IsResolved = true;

            await _baseRepository.SaveChangesAsync();

            return Ok(new { isAccepted = entity.IsAcceptedAnswer });
        }

        [HttpDelete("{answerId:long}")]
        public async Task<IActionResult> Delete(long answerId)
        {
            var userId = _currentUser.GetCurrentUserId();

            var entity = await _baseRepository.Query()
                .Include(a => a.Question)
                .FirstOrDefaultAsync(a => a.Id == answerId && a.UserId == userId && !a.IsDeleted);

            if (entity == null) return NotFound();

            entity.IsDeleted = true;
            entity.UpdatedAt = DateTime.UtcNow;
            entity.Question.AnswerCount = Math.Max(0, entity.Question.AnswerCount - 1);
            await _baseRepository.SaveChangesAsync();

            return Ok(new { success = true });
        }
    }
}
