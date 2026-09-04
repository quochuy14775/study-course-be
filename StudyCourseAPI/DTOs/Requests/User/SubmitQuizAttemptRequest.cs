using System.ComponentModel.DataAnnotations;

namespace StudyCourseAPI.DTOs.Requests;

public class SubmitQuizAttemptRequest
{
    [Required]
    [MinLength(1)]
    public List<QuizAnswerRequest> Answers { get; set; } = new();
}

public class QuizAnswerRequest
{
    public long QuestionId { get; set; }

    /// <summary>Null when the learner left the question unanswered.</summary>
    public int? SelectedOptionId { get; set; }
}
