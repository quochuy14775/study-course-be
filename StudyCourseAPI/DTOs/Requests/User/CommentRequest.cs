using System.ComponentModel.DataAnnotations;

namespace StudyCourseAPI.DTOs.Requests;

public class CommentRequest
{
    [Required]
    [MaxLength(2000)]
    public string Content { get; set; } = null!;

    public long? ParentCommentId { get; set; }
}
