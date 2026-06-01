using System.ComponentModel.DataAnnotations;

namespace StudyCourseAPI.DTOs.Requests;

public class AnswerRequest
{
    [Required]
    [MaxLength(2000)]
    public string Content { get; set; } = null!;
}
