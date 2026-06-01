namespace StudyCourseAPI.Models;

public class CommentLike
{
    public long CommentId { get; set; }
    public LessonComment Comment { get; set; } = null!;

    public long UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
