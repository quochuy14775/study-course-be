using StudyCourseAPI.Enums;
using StudyCourseAPI.Models;

namespace StudyCourseAPI.DTOs.Responses.Admin;

public class QuizResponse
{
    public long Id { get; set; }
    public QuizType QuizType { get; set; }
    public long? LessonId { get; set; }
    public long CourseId { get; set; }
    public string Title { get; set; } = null!;
    public int PassPercentage { get; set; }
    public int TimeLimitMinutes { get; set; }
    public List<QuizQuestionResponse> Questions { get; set; } = new();

    public QuizResponse(Quiz q)
    {
        Id = q.Id;
        QuizType = q.QuizType;
        LessonId = q.LessonId;
        CourseId = q.CourseId;
        Title = q.Title;
        PassPercentage = q.PassPercentage;
        TimeLimitMinutes = q.TimeLimitMinutes;
        Questions = q.Questions
            .OrderBy(x => x.OrderIndex)
            .Select(x => new QuizQuestionResponse(x))
            .ToList();
    }
}

public class QuizQuestionResponse
{
    public long Id { get; set; }
    public string Content { get; set; } = null!;
    public int OrderIndex { get; set; }
    public double Points { get; set; }
    public List<QuizOptionResponse> Options { get; set; } = new();

    public QuizQuestionResponse(QuizQuestion q)
    {
        Id = q.Id;
        Content = q.Content;
        OrderIndex = q.OrderIndex;
        Points = q.Points;
        Options = q.Options
            .OrderBy(o => o.OrderIndex)
            .Select(o => new QuizOptionResponse(o))
            .ToList();
    }
}

/// <summary>Admin-facing — includes IsCorrect. Never expose this shape to learners taking the quiz.</summary>
public class QuizOptionResponse
{
    public int OptionId { get; set; }
    public string Content { get; set; } = null!;
    public bool IsCorrect { get; set; }

    public QuizOptionResponse(QuizOptionItem o)
    {
        OptionId = o.OptionId;
        Content = o.Content;
        IsCorrect = o.IsCorrect;
    }
}
