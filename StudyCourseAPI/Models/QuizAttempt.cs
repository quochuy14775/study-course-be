namespace StudyCourseAPI.Models;

public class QuizAttempt : BaseEntity<long>
{
    public long QuizId { get; set; }
    public Quiz Quiz { get; set; } = null!;

    public long UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;

    /// <summary>1, 2, 3... per (QuizId, UserId) — lets FE show "lần làm thứ N".</summary>
    public int AttemptNumber { get; set; }

    public int CorrectCount { get; set; }
    public int TotalCount { get; set; }
    public double PercentageScore { get; set; }
    public bool IsPassed { get; set; }

    public DateTime SubmittedAt { get; set; }

    /// <summary>
    /// Stored as jsonb — snapshot of each question/selected option at submit time,
    /// so "xem lại đáp án" stays correct even if admin edits the quiz afterwards.
    /// </summary>
    public List<QuizAnswerSnapshot> Answers { get; set; } = new();
}

/// <summary>Not an entity — plain shape serialized into QuizAttempt.Answers (jsonb).</summary>
public class QuizAnswerSnapshot
{
    public long QuestionId { get; set; }
    public string QuestionContent { get; set; } = null!;
    public int? SelectedOptionId { get; set; }
    public string? SelectedOptionContent { get; set; }
    public int CorrectOptionId { get; set; }
    public string CorrectOptionContent { get; set; } = null!;
    public bool IsCorrect { get; set; }
}
