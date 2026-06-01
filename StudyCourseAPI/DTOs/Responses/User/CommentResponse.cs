using StudyCourseAPI.Models;

namespace StudyCourseAPI.DTOs.Responses;

public class CommentResponse
{
    public long Id { get; set; }
    public long LessonId { get; set; }
    public long UserId { get; set; }
    public string? Author { get; set; } = null!;
    public string? AvatarUrl { get; set; }
    public long? ParentCommentId { get; set; }
    public string Content { get; set; } = null!;
    public int LikeCount { get; set; }
    public bool Liked { get; set; } // populated per-user
    public DateTime CreatedAt { get; set; }
    public List<CommentResponse> Replies { get; set; } = new();

    public CommentResponse(LessonComment c, long currentUserId)
    {
        Id = c.Id;
        LessonId = c.LessonId;
        UserId = c.UserId;
        Author = c.User.UserName;
        AvatarUrl = c.User?.AvatarUrl;
        ParentCommentId = c.ParentCommentId;
        Content = c.Content;
        LikeCount = c.LikeCount;
        Liked = c.Likes.Any(l => l.UserId == currentUserId);
        CreatedAt = c.CreatedAt;
        Replies = c.Replies
            .Where(r => !r.IsDeleted)
            .OrderBy(r => r.CreatedAt)
            .Select(r => new CommentResponse(r, currentUserId))
            .ToList();
    }
}
