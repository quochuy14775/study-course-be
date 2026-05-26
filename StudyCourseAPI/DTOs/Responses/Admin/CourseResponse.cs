using StudyCourseAPI.Models;

namespace StudyCourseAPI.DTOs.Responses.Admin
{
    public class CourseResponse
    {
        public long Id { get; set; }
        public string Title { get; set; } = null!;
        public string? Subtitle { get; set; }
        public string Description { get; set; } = null!;
        public string? ImageUrl { get; set; }
        public decimal Price { get; set; }
        public string Level { get; set; } = null!;
        public bool IsFeatured { get; set; }
        public double Rating { get; set; }

        // Cached stats (auto-maintained by LessonsController on lesson/chapter mutations)
        public int LessonCount { get; set; }
        public int ChapterCount { get; set; }
        public int TotalDurationSeconds { get; set; }

        // Audit
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; }
        public bool IsActive { get; set; }
        public string? CreatedBy { get; set; }
        public string? UpdatedBy { get; set; }

        public CourseResponse(Course course)
        {
            Id = course.Id;
            Title = course.Title;
            Subtitle = course.Subtitle;
            Description = course.Description;
            ImageUrl = course.ImageUrl;
            Price = course.Price;
            Level = course.Level.ToString();
            IsFeatured = course.IsFeatured;
            Rating = course.Rating;
            LessonCount = course.LessonCount;
            ChapterCount = course.ChapterCount;
            TotalDurationSeconds = course.TotalDurationSeconds;
            CreatedAt = course.CreatedAt;
            UpdatedAt = course.UpdatedAt;
            IsDeleted = course.IsDeleted;
            IsActive = course.IsActive;
            CreatedBy = course.CreatedBy;
            UpdatedBy = course.UpdatedBy;
        }
    }

    public class CourseDetailResponse : CourseResponse
    {
        /// <summary>Tag ids associated with this course (Language/Framework/Topic).</summary>
        public List<long> TagIds { get; set; } = new();

        public CourseDetailResponse(Course course) : base(course)
        {
            if (course.CourseTags != null && course.CourseTags.Any())
                TagIds = course.CourseTags.Select(ct => ct.TagId).ToList();
        }
    }
}
