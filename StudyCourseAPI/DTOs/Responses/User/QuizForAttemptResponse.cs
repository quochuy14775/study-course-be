using StudyCourseAPI.Models;

namespace StudyCourseAPI.DTOs.Responses;

/// <summary>What a learner sees while taking the quiz — never includes IsCorrect.</summary>
public class QuizForAttemptResponse
{
    public long Id { get; set; }
    public string Title { get; set; } = null!;
    public int PassPercentage { get; set; }
    public int TimeLimitMinutes { get; set; }
    public List<QuizQuestionForAttemptResponse> Questions { get; set; } = new();

    public QuizForAttemptResponse(Quiz q)
    {
        Id = q.Id;
        Title = q.Title;
        PassPercentage = q.PassPercentage;
        TimeLimitMinutes = q.TimeLimitMinutes;
        Questions = q.Questions
            .OrderBy(x => x.OrderIndex)
            .Select(x => new QuizQuestionForAttemptResponse(x))
            .ToList();
    }
}

public class QuizQuestionForAttemptResponse
{
    public long Id { get; set; }
    public string Content { get; set; } = null!;
    public int OrderIndex { get; set; }
    public List<QuizOptionForAttemptResponse> Options { get; set; } = new();

    public QuizQuestionForAttemptResponse(QuizQuestion q)
    {
        Id = q.Id;
        Content = q.Content;
        OrderIndex = q.OrderIndex;
        Options = q.Options
            .OrderBy(o => o.OrderIndex)
            .Select(o => new QuizOptionForAttemptResponse { OptionId = o.OptionId, Content = o.Content })
            .ToList();
    }
}

public class QuizOptionForAttemptResponse
{
    public int OptionId { get; set; }
    public string Content { get; set; } = null!;
}
