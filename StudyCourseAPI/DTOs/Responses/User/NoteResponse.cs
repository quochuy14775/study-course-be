using StudyCourseAPI.Models;

namespace StudyCourseAPI.DTOs.Responses;

public class NoteResponse
{
    public long Id { get; set; }
    public long LessonId { get; set; }
    public int VideoTimestamp { get; set; }
    public string Content { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public NoteResponse(LessonNote n)
    {
        Id = n.Id;
        LessonId = n.LessonId;
        VideoTimestamp = n.VideoTimestamp;
        Content = n.Content;
        CreatedAt = n.CreatedAt;
        UpdatedAt = n.UpdatedAt;
    }
}
