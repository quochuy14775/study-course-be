using System.ComponentModel.DataAnnotations;

namespace StudyCourseAPI.DTOs.Requests;

public class NoteRequest
{
    [Required]
    [MaxLength(5000)]
    public string Content { get; set; } = null!;

    [Range(0, int.MaxValue)]
    public int VideoTimestamp { get; set; }
}
