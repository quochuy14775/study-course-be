namespace StudyCourseAPI.DTOs.Responses;

/// <summary>Course-test summary card shown in the sidebar — meta + eligibility + last attempt, no questions.</summary>
public class CourseTestResponse
{
    public long Id { get; set; }
    public string Title { get; set; } = null!;
    public int QuestionCount { get; set; }
    public int TimeLimitMinutes { get; set; }
    public int PassPercentage { get; set; }
    public bool Unlocked { get; set; }
    public QuizAttemptResultResponse? LastAttempt { get; set; }
}
