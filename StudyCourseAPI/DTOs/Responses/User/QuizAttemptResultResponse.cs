using StudyCourseAPI.Models;

namespace StudyCourseAPI.DTOs.Responses;

/// <summary>Returned right after grading — safe to reveal correct answers here since the learner already submitted.</summary>
public class QuizAttemptResultResponse
{
    public long Id { get; set; }
    public long QuizId { get; set; }
    public int AttemptNumber { get; set; }
    public int CorrectCount { get; set; }
    public int TotalCount { get; set; }
    public double PercentageScore { get; set; }
    public bool IsPassed { get; set; }
    public DateTime SubmittedAt { get; set; }
    public List<QuizAnswerSnapshot> Answers { get; set; } = new();

    public QuizAttemptResultResponse(QuizAttempt a)
    {
        Id = a.Id;
        QuizId = a.QuizId;
        AttemptNumber = a.AttemptNumber;
        CorrectCount = a.CorrectCount;
        TotalCount = a.TotalCount;
        PercentageScore = a.PercentageScore;
        IsPassed = a.IsPassed;
        SubmittedAt = a.SubmittedAt;
        Answers = a.Answers;
    }
}
