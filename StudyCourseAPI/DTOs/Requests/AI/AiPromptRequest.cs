namespace StudyCourseAPI.DTOs.Requests;

public class AiPromptRequest
{
    public string Prompt { get; set; } = string.Empty;
    public string? SystemPrompt { get; set; }
}

public class LessonExplanationRequest
{
    public string LessonTitle { get; set; } = string.Empty;
    public string LessonContent { get; set; } = string.Empty;
    public string? AdditionalContext { get; set; }
}

public class QuizGeneratorRequest
{
    public string Topic { get; set; } = string.Empty;
    public int NumberOfQuestions { get; set; } = 5;
    public string? Difficulty { get; set; } = "medium";
}

public class HomeworkAssistantRequest
{
    public string Question { get; set; } = string.Empty;
    public string? CourseContext { get; set; }
}

public class CodeReviewRequest
{
    public string Code { get; set; } = string.Empty;
    public string? Language { get; set; } = "csharp";
}

