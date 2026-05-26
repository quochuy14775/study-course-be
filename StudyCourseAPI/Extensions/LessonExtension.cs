using Microsoft.EntityFrameworkCore;
using StudyCourseAPI.DTOs.Requests.Admin;
using StudyCourseAPI.Models;
using StudyCourseAPI.Repositories;

namespace StudyCourseAPI.Extensions
{
    public static class LessonExtensions
    {
        public static async Task<(bool Success, Dictionary<string, List<string>>? Errors)>
            ValidateLessonAsync(
                this LessonRequest model,
                IRepository<Lesson> repository,
                IRepository<Chapter> chapterRepository,
                long courseId,
                long? excludeId = null)
        {
            var errors = new Dictionary<string, List<string>>();

            void Add(string key, string msg)
            {
                if (!errors.ContainsKey(key)) errors[key] = new List<string>();
                errors[key].Add(msg);
            }

            // OrderIndex
            if (model.OrderIndex < 0)
                Add("orderIndex", "OrderIndex must be greater than or equal to 0.");

            // Title
            if (string.IsNullOrWhiteSpace(model.Title))
                Add("title", "Lesson title is required.");
            else if (model.Title.Length > 200)
                Add("title", "Lesson title cannot exceed 200 characters.");

            // Description
            if (!string.IsNullOrEmpty(model.Description) && model.Description.Length > 2000)
                Add("description", "Description cannot exceed 2000 characters.");

            // VideoId
            if (string.IsNullOrWhiteSpace(model.VideoId))
                Add("videoId", "VideoId is required.");
            else if (model.VideoId.Length > 200)
                Add("videoId", "VideoId cannot exceed 200 characters.");

            // Duration
            if (model.Duration.HasValue && model.Duration < 0)
                Add("duration", "Duration must be greater than or equal to 0.");

            // ThumbnailUrl
            if (!string.IsNullOrEmpty(model.ThumbnailUrl) && model.ThumbnailUrl.Length > 500)
                Add("thumbnailUrl", "ThumbnailUrl cannot exceed 500 characters.");

            // ChapterId belongs to the same course (if provided)
            if (model.ChapterId.HasValue)
            {
                var chapterExists = await chapterRepository.Query()
                    .AnyAsync(c => c.Id == model.ChapterId.Value && c.CourseId == courseId && !c.IsDeleted);

                if (!chapterExists)
                    Add("chapterId", "Chapter does not exist in this course.");
            }

            if (errors.Any())
                return (false, errors);

            return (true, null);
        }

        public static Lesson GetLesson(this LessonRequest model, long courseId)
        {
            return new Lesson
            {
                OrderIndex = model.OrderIndex,
                Title = model.Title!.Trim(),
                Description = model.Description?.Trim(),
                VideoId = model.VideoId!.Trim(),
                Duration = model.Duration,
                ThumbnailUrl = model.ThumbnailUrl?.Trim(),
                CourseId = courseId,
                ChapterId = model.ChapterId,
                IsPreview = model.IsPreview,
                IsActive = model.IsActive,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        public static void ToEntity(this LessonRequest model, Lesson entity)
        {
            entity.OrderIndex = model.OrderIndex;
            entity.Title = model.Title!.Trim();
            entity.Description = model.Description?.Trim();
            entity.VideoId = model.VideoId!.Trim();
            entity.Duration = model.Duration;
            entity.ThumbnailUrl = model.ThumbnailUrl?.Trim();
            entity.ChapterId = model.ChapterId;
            entity.IsPreview = model.IsPreview;
            entity.IsActive = model.IsActive;
            entity.UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Recompute and update cached Course stats (LessonCount, TotalDurationSeconds, ChapterCount).
        /// Call after any Lesson or Chapter mutation that affects counts/durations.
        /// </summary>
        public static async Task RefreshCourseStatsAsync(
            this IRepository<Course> courseRepository,
            IRepository<Lesson> lessonRepository,
            IRepository<Chapter> chapterRepository,
            long courseId)
        {
            var course = await courseRepository.Query()
                .FirstOrDefaultAsync(c => c.Id == courseId && !c.IsDeleted);
            if (course == null) return;

            var lessonStats = await lessonRepository.Query()
                .Where(l => l.CourseId == courseId && !l.IsDeleted)
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    Count = g.Count(),
                    TotalDuration = g.Sum(l => l.Duration ?? 0)
                })
                .FirstOrDefaultAsync();

            var chapterCount = await chapterRepository.Query()
                .CountAsync(c => c.CourseId == courseId && !c.IsDeleted);

            course.LessonCount = lessonStats?.Count ?? 0;
            course.TotalDurationSeconds = lessonStats?.TotalDuration ?? 0;
            course.ChapterCount = chapterCount;
            course.UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>Compute next available OrderIndex for a course (or for a chapter if specified).</summary>
        public static async Task<int> NextOrderIndexAsync(
            this IRepository<Lesson> lessonRepository,
            long courseId,
            long? chapterId = null)
        {
            var query = lessonRepository.Query()
                .Where(l => l.CourseId == courseId && !l.IsDeleted);

            if (chapterId.HasValue)
                query = query.Where(l => l.ChapterId == chapterId);

            var max = await query.MaxAsync(l => (int?)l.OrderIndex) ?? -1;
            return max + 1;
        }
    }
}
