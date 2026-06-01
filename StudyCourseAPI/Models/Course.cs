using StudyCourseAPI.Enums;

namespace StudyCourseAPI.Models;

public class Course : BaseEntity<long>, IAuditable, IUserTracking
{
    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;

    public string? ImageUrl { get; set; }
    public decimal Price { get; set; }
    public CourseLevel Level { get; set; }
    public bool IsFeatured { get; set; } = false;
    public double Rating { get; set; } = 0;

    /// <summary>Short tagline shown on course cards. Optional.</summary>
    public string? Subtitle { get; set; }

    /// <summary>
    /// Cached aggregate of all lesson durations (seconds).
    /// Updated when lessons are added/removed/edited.
    /// Lets course-list endpoint return totals without joining Lessons.
    /// </summary>
    public int TotalDurationSeconds { get; set; }

    /// <summary>Cached lesson count (kept in sync via service layer).</summary>
    public int LessonCount { get; set; }

    /// <summary>Cached chapter count.</summary>
    public int ChapterCount { get; set; }

    // Audit
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public bool IsActive { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }

    // Children
    public ICollection<Chapter> Chapters { get; set; } = new List<Chapter>();
    public ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();
    public ICollection<UserCourse> UserCourses { get; set; } = new List<UserCourse>();
    public ICollection<CourseTag> CourseTags { get; set; } = new List<CourseTag>();
    public ICollection<CourseBookmark> Bookmarks { get; set; } = new List<CourseBookmark>();
    public ICollection<CourseSkill> CourseSkills { get; set; } = new List<CourseSkill>();
}
