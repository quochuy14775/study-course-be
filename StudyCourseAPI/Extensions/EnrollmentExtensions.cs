using Microsoft.EntityFrameworkCore;
using StudyCourseAPI.Models;
using StudyCourseAPI.Repositories;

namespace StudyCourseAPI.Extensions
{
    /// <summary>
    /// UserCourse là bảng duy nhất trả lời câu hỏi "user này đang học course nào".
    /// Nó bị ghi từ nhiều luồng — bấm đăng ký, hoàn thành bài học, cấp chứng chỉ — nên phần
    /// upsert + tính lại progress nằm ở đây thay vì nhân bản trong từng service.
    /// Caller chịu trách nhiệm gọi SaveChangesAsync.
    /// </summary>
    public static class EnrollmentExtensions
    {
        /// <summary>
        /// Trả về enrollment của (courseId, userId), tạo mới nếu chưa có. Idempotent.
        /// Row đã soft-delete được kích hoạt lại thay vì insert bản ghi mới — khóa chính là
        /// (UserId, CourseId) nên insert trùng sẽ nổ ở DB.
        /// </summary>
        public static async Task<UserCourse> EnsureEnrolledAsync(
            this IRepository<UserCourse> userCourseRepository,
            long courseId,
            long userId)
        {
            var existing = await userCourseRepository.Query()
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(uc => uc.CourseId == courseId && uc.UserId == userId);

            if (existing != null)
            {
                if (existing.IsDeleted)
                {
                    existing.IsDeleted = false;
                    existing.IsActive = true;
                    existing.EnrolledAt = DateTime.UtcNow;
                }
                return existing;
            }

            var enrollment = new UserCourse
            {
                UserId = userId,
                CourseId = courseId,
                EnrolledAt = DateTime.UtcNow,
                Progress = 0,
                IsCompleted = false,
                IsActive = true,
            };
            userCourseRepository.Add(enrollment);

            return enrollment;
        }

        /// <summary>
        /// Tính lại Progress từ UserLessonProgress — nguồn sự thật duy nhất về việc học.
        /// Gọi sau mỗi lần user hoàn thành một bài, nếu không "Khóa học của tôi" sẽ lệch với
        /// checkmark trong curriculum.
        ///
        /// Không đụng tới IsCompleted: khóa học chỉ được coi là hoàn thành khi user pass course
        /// test và được cấp chứng chỉ (xem <see cref="CertificateExtensions"/>) — xem hết video
        /// vẫn là "đang học".
        /// </summary>
        public static async Task<UserCourse> SyncProgressAsync(
            this IRepository<UserCourse> userCourseRepository,
            IRepository<UserLessonProgress> progressRepository,
            IRepository<Lesson> lessonRepository,
            long courseId,
            long userId)
        {
            var enrollment = await userCourseRepository.EnsureEnrolledAsync(courseId, userId);

            var totalLessons = await lessonRepository.Query()
                .CountAsync(l => l.CourseId == courseId && !l.IsDeleted);

            var completedLessons = await progressRepository.Query()
                .CountAsync(p => p.CourseId == courseId && p.UserId == userId && p.IsCompleted);

            enrollment.Progress = totalLessons == 0
                ? 0
                : Math.Round(Math.Min(completedLessons, totalLessons) * 100d / totalLessons, 2);
            enrollment.UpdatedAt = DateTime.UtcNow;

            return enrollment;
        }
    }
}
