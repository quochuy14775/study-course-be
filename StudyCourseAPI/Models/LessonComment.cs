namespace StudyCourseAPI.Models;

public class LessonComment : BaseEntity<long>, IAuditable
{
    public long LessonId { get; set; }
    public Lesson Lesson { get; set; } = null!;

    public long UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;

    public long? ParentCommentId { get; set; }
    public LessonComment? ParentComment { get; set; }
    public ICollection<LessonComment> Replies { get; set; } = new List<LessonComment>();

    public string Content { get; set; } = null!;
    public int LikeCount { get; set; }

    public ICollection<CommentLike> Likes { get; set; } = new List<CommentLike>();

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public bool IsActive { get; set; } = true;
}
