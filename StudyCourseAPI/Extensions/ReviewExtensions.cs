using Microsoft.EntityFrameworkCore;
using StudyCourseAPI.DTOs.Requests;
using StudyCourseAPI.Models;
using StudyCourseAPI.Repositories;

namespace StudyCourseAPI.Extensions
{
    public static class ReviewExtensions
    {
        public static async Task<(bool Success, Dictionary<string, List<string>>? Errors)>
            ValidateReviewAsync(
                this ReviewRequest model,
                IRepository<CourseReview> repository,
                long courseId,
                long userId)
        {
            var errors = new Dictionary<string, List<string>>();

            void Add(string key, string msg)
            {
                if (!errors.ContainsKey(key)) errors[key] = new List<string>();
                errors[key].Add(msg);
            }

            if (model.Rating < 1 || model.Rating > 5)
                Add("rating", "Rating must be between 1 and 5.");

            if (string.IsNullOrWhiteSpace(model.Content))
                Add("content", "Content is required.");
            else if (model.Content.Length > 2000)
                Add("content", "Content cannot exceed 2000 characters.");

            var alreadyReviewed = await repository.Query()
                .AnyAsync(r => r.CourseId == courseId && r.UserId == userId && !r.IsDeleted);
            if (alreadyReviewed)
                Add("courseId", "You have already reviewed this course.");

            return (errors.Count == 0, errors.Count == 0 ? null : errors);
        }

        public static CourseReview GetReview(this ReviewRequest model, long courseId, long userId)
        {
            return new CourseReview
            {
                CourseId = courseId,
                UserId = userId,
                Rating = model.Rating,
                Content = model.Content.Trim(),
            };
        }

        public static (bool Success, Dictionary<string, List<string>>? Errors)
            ValidateReply(this ReviewReplyRequest model)
        {
            var errors = new Dictionary<string, List<string>>();

            void Add(string key, string msg)
            {
                if (!errors.ContainsKey(key)) errors[key] = new List<string>();
                errors[key].Add(msg);
            }

            if (string.IsNullOrWhiteSpace(model.Content))
                Add("content", "Content is required.");
            else if (model.Content.Length > 2000)
                Add("content", "Content cannot exceed 2000 characters.");

            return (errors.Count == 0, errors.Count == 0 ? null : errors);
        }

        public static CourseReviewReply GetReply(this ReviewReplyRequest model, long reviewId, long userId)
        {
            return new CourseReviewReply
            {
                ReviewId = reviewId,
                UserId = userId,
                Content = model.Content.Trim(),
            };
        }

        /// <summary>Recomputes Course.Rating/ReviewCount from active reviews — mirrors LessonExtension.RefreshCourseStatsAsync.</summary>
        public static async Task RefreshReviewStatsAsync(
            this IRepository<Course> courseRepository,
            IRepository<CourseReview> reviewRepository,
            long courseId)
        {
            var course = await courseRepository.Query()
                .FirstOrDefaultAsync(c => c.Id == courseId && !c.IsDeleted);
            if (course == null) return;

            var stats = await reviewRepository.Query()
                .Where(r => r.CourseId == courseId && !r.IsDeleted)
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    Count = g.Count(),
                    Average = g.Average(r => (double)r.Rating)
                })
                .FirstOrDefaultAsync();

            course.ReviewCount = stats?.Count ?? 0;
            course.Rating = stats != null ? Math.Round(stats.Average, 1) : 0;
        }
    }
}
