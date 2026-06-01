namespace StudyCourseAPI.Models;

public class LessonNote : BaseEntity<long>, IAuditable
{
    public long LessonId { get; set; }
    public Lesson Lesson { get; set; } = null!;

    public long UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;

    public int VideoTimestamp { get; set; } // seconds into video

    public string Content { get; set; } = null!;

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public bool IsActive { get; set; } = true;
}
