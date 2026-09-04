using Microsoft.EntityFrameworkCore;
using StudyCourseAPI.Models;
using StudyCourseAPI.Repositories;

namespace StudyCourseAPI.Extensions
{
    public static class CertificateExtensions
    {
        private static string GenerateCode(long courseId) =>
            $"EDU-{courseId}-{DateTime.UtcNow:yyyy}-{Random.Shared.Next(1000, 9999)}";

        /// <summary>
        /// Issues a certificate for (courseId, userId) if one doesn't already exist, and marks
        /// the matching UserCourse as completed. Idempotent — safe to call on every course-test pass.
        /// Caller is responsible for SaveChangesAsync.
        /// </summary>
        public static async Task<Certificate> IssueIfEligibleAsync(
            this IRepository<Certificate> certificateRepository,
            IRepository<UserCourse> userCourseRepository,
            long courseId,
            long userId,
            double scorePercentage)
        {
            var existing = await certificateRepository.Query()
                .FirstOrDefaultAsync(c => c.CourseId == courseId && c.UserId == userId);
            if (existing != null) return existing;

            var certificate = new Certificate
            {
                CourseId = courseId,
                UserId = userId,
                CertificateCode = GenerateCode(courseId),
                IssuedAt = DateTime.UtcNow,
                ScorePercentage = scorePercentage,
            };
            certificateRepository.Add(certificate);

            var userCourse = await userCourseRepository.Query()
                .FirstOrDefaultAsync(uc => uc.CourseId == courseId && uc.UserId == userId && !uc.IsDeleted);
            if (userCourse != null)
            {
                userCourse.IsCompleted = true;
                userCourse.CompletedAt = DateTime.UtcNow;
                userCourse.Progress = 100;
            }

            return certificate;
        }
    }
}
