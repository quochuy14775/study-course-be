using StudyCourseAPI.Models;

namespace StudyCourseAPI.DTOs.Responses.Admin
{
    public class ChapterResponse
    {
        public long Id { get; set; }
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public int OrderIndex { get; set; }
        public long CourseId { get; set; }
        public int LessonCount { get; set; }
        public int TotalDurationSeconds { get; set; }

        // Audit
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; }
        public bool IsActive { get; set; }

        public ChapterResponse(Chapter chapter)
        {
            Id = chapter.Id;
            Title = chapter.Title;
            Description = chapter.Description;
            OrderIndex = chapter.OrderIndex;
            CourseId = chapter.CourseId;
            LessonCount = chapter.Lessons.Count(l => !l.IsDeleted);
            TotalDurationSeconds = chapter.Lessons
                .Where(l => !l.IsDeleted)
                .Sum(l => l.Duration ?? 0);
            CreatedAt = chapter.CreatedAt;
            UpdatedAt = chapter.UpdatedAt;
            IsDeleted = chapter.IsDeleted;
            IsActive = chapter.IsActive;
        }
    }
}
