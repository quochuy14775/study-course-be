using StudyCourseAPI.Models;

namespace StudyCourseAPI.DTOs.Responses.Admin
{
    public class LessonResponse
    {
        public long Id { get; set; }
        public int OrderIndex { get; set; }
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public string VideoId { get; set; } = null!;
        public int? Duration { get; set; }
        public string? ThumbnailUrl { get; set; }
        public bool IsPreview { get; set; }

        // FK
        public long CourseId { get; set; }
        public long? ChapterId { get; set; }

        // audit
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public bool IsDeleted { get; set; }
        public bool IsActive { get; set; }

        public LessonResponse(Lesson lesson)
        {
            Id = lesson.Id;
            OrderIndex = lesson.OrderIndex;
            Title = lesson.Title;
            Description = lesson.Description;
            VideoId = lesson.VideoId;
            Duration = lesson.Duration;
            ThumbnailUrl = lesson.ThumbnailUrl;
            IsPreview = lesson.IsPreview;
            CourseId = lesson.CourseId;
            ChapterId = lesson.ChapterId;
            CreatedAt = lesson.CreatedAt;
            UpdatedAt = lesson.UpdatedAt;
            IsDeleted = lesson.IsDeleted;
            IsActive = lesson.IsActive;
        }
    }

    public class LessonDetailResponse : LessonResponse
    {
        public LessonDetailResponse(Lesson lesson) : base(lesson) { }
    }
}
