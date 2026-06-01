using System.ComponentModel.DataAnnotations;

namespace StudyCourseAPI.DTOs.Requests;

public class QuestionRequest
{
    [Required]
    [MaxLength(2000)]
    public string Content { get; set; } = null!;
}
