using System.ComponentModel.DataAnnotations;

namespace StudyCourseAPI.DTOs.Requests;

public class ReviewRequest
{
    [Range(1, 5)]
    public int Rating { get; set; }

    [Required]
    [MaxLength(2000)]
    public string Content { get; set; } = null!;
}
