namespace StudyCourseAPI.Models;

public class AnswerLike
{
    public long AnswerId { get; set; }
    public QuestionAnswer Answer { get; set; } = null!;

    public long UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
