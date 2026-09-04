using System.ComponentModel.DataAnnotations;

namespace StudyCourseAPI.DTOs.Requests.Admin;

public class QuizRequest
{
    [Required]
    [MaxLength(255)]
    public string Title { get; set; } = null!;

    [Range(1, 100)]
    public int PassPercentage { get; set; } = 70;

    [Range(1, 300)]
    public int TimeLimitMinutes { get; set; } = 10;

    [Required]
    [MinLength(1)]
    public List<QuizQuestionRequest> Questions { get; set; } = new();
}

public class QuizQuestionRequest
{
    [Required]
    [MaxLength(2000)]
    public string Content { get; set; } = null!;

    public int OrderIndex { get; set; }

    [Range(0.1, 100)]
    public double Points { get; set; } = 1;

    [Required]
    [MinLength(2)]
    public List<QuizOptionRequest> Options { get; set; } = new();
}

public class QuizOptionRequest
{
    [Required]
    [MaxLength(1000)]
    public string Content { get; set; } = null!;

    public bool IsCorrect { get; set; }
    public int OrderIndex { get; set; }
}
