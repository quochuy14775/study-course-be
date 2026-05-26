namespace StudyCourseAPI.DTOs.Responses;

public class AiResponseDto
{
    public string Response { get; set; } = string.Empty;
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public int TotalTokens => PromptTokens + CompletionTokens;
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

public class LessonExplanationResponseDto
{
    public string Explanation { get; set; } = string.Empty;
    public List<string> KeyPoints { get; set; } = new();
    public string SummaryForStudents { get; set; } = string.Empty;
    public string? CommonMisunderstandings { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

public class QuizGenerationResponseDto
{
    public List<QuizQuestionDto> Questions { get; set; } = new();
    public string Topic { get; set; } = string.Empty;
    public int Difficulty { get; set; } // 1-3
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

public class QuizQuestionDto
{
    public int Number { get; set; }
    public string Question { get; set; } = string.Empty;
    public List<string> Options { get; set; } = new();
    public string CorrectAnswer { get; set; } = string.Empty;
    public string? Explanation { get; set; }
}

public class HomeworkAssistantResponseDto
{
    public string Solution { get; set; } = string.Empty;
    public string StepByStepExplanation { get; set; } = string.Empty;
    public string Hint { get; set; } = string.Empty;
    public List<string> RelatedConcepts { get; set; } = new();
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

public class CodeReviewResponseDto
{
    public string OverallAssessment { get; set; } = string.Empty;
    public List<string> Issues { get; set; } = new();
    public List<string> Suggestions { get; set; } = new();
    public List<string> BestPractices { get; set; } = new();
    public string ImprovedCode { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

