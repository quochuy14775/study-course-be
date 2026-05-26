namespace StudyCourseAPI.Models;

/// <summary>
/// Per-lesson progress for a user.
/// Composite key: (UserId, LessonId).
/// Powers the "Tiếp tục học" button + curriculum checkmarks.
/// </summary>
public class UserLessonProgress : IAuditable
{
    // Composite key
    public long UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;

    public long LessonId { get; set; }
    public Lesson Lesson { get; set; } = null!;

    // Convenience: denormalized for fast course-level aggregation
    public long CourseId { get; set; }

    /// <summary>Seconds watched (for resume + analytics).</summary>
    public int WatchedSeconds { get; set; }

    public bool IsCompleted { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? LastWatchedAt { get; set; }

    // Audit
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public bool IsActive { get; set; } = true;
}
