namespace StudyCourseAPI.Models;

public class QuestionAnswer : BaseEntity<long>, IAuditable
{
    public long QuestionId { get; set; }
    public LessonQuestion Question { get; set; } = null!;

    public long UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;

    public string Content { get; set; } = null!;
    public bool IsAcceptedAnswer { get; set; }
    public int LikeCount { get; set; }

    public ICollection<AnswerLike> Likes { get; set; } = new List<AnswerLike>();

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public bool IsActive { get; set; } = true;
}
