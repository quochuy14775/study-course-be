using StudyCourseAPI.Models;

namespace StudyCourseAPI.DTOs.Responses;

public class QuestionResponse
{
    public long Id { get; set; }
    public long LessonId { get; set; }
    public long UserId { get; set; }
    public string Author { get; set; } = null!;
    public string? AvatarUrl { get; set; }
    public string Content { get; set; } = null!;
    public bool IsResolved { get; set; }
    public int AnswerCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<AnswerResponse> Answers { get; set; } = new();

    public QuestionResponse(LessonQuestion q, long currentUserId)
    {
        Id = q.Id;
        LessonId = q.LessonId;
        UserId = q.UserId;
        Author = q.User?.FullName ?? q.User?.UserName ?? q.User?.Email ?? "Người dùng";
        AvatarUrl = q.User?.AvatarUrl;
        Content = q.Content;
        IsResolved = q.IsResolved;
        AnswerCount = q.AnswerCount;
        CreatedAt = q.CreatedAt;
        Answers = q.Answers
            .Where(a => !a.IsDeleted)
            .OrderByDescending(a => a.IsAcceptedAnswer)
            .ThenByDescending(a => a.LikeCount)
            .Select(a => new AnswerResponse(a, currentUserId))
            .ToList();
    }
}

public class AnswerResponse
{
    public long Id { get; set; }
    public long QuestionId { get; set; }
    public long UserId { get; set; }
    public string Author { get; set; } = null!;
    public string? AvatarUrl { get; set; }
    public string Content { get; set; } = null!;
    public bool IsAcceptedAnswer { get; set; }
    public int LikeCount { get; set; }
    public bool Liked { get; set; }
    public DateTime CreatedAt { get; set; }

    public AnswerResponse(QuestionAnswer a, long currentUserId)
    {
        Id = a.Id;
        QuestionId = a.QuestionId;
        UserId = a.UserId;
        Author = a.User?.FullName ?? a.User?.UserName ?? a.User?.Email ?? "Người dùng";
        AvatarUrl = a.User?.AvatarUrl;
        Content = a.Content;
        IsAcceptedAnswer = a.IsAcceptedAnswer;
        LikeCount = a.LikeCount;
        Liked = a.Likes.Any(l => l.UserId == currentUserId);
        CreatedAt = a.CreatedAt;
    }
}
