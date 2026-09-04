using StudyCourseAPI.Models;

namespace StudyCourseAPI.DTOs.Responses;

public class ReviewResponse
{
    public long Id { get; set; }
    public long CourseId { get; set; }
    public long UserId { get; set; }
    public string Author { get; set; } = null!;
    public string? AvatarUrl { get; set; }
    public int Rating { get; set; }
    public string Content { get; set; } = null!;
    public int HelpfulCount { get; set; }
    public bool MarkedHelpful { get; set; } // populated per-user
    public DateTime CreatedAt { get; set; }
    public List<ReviewReplyResponse> Replies { get; set; } = new();

    public ReviewResponse(CourseReview r, long currentUserId, HashSet<long> instructorIds)
    {
        Id = r.Id;
        CourseId = r.CourseId;
        UserId = r.UserId;
        Author = r.User?.FullName ?? r.User?.UserName ?? r.User?.Email ?? "Người dùng";
        AvatarUrl = r.User?.AvatarUrl;
        Rating = r.Rating;
        Content = r.Content;
        HelpfulCount = r.HelpfulCount;
        MarkedHelpful = r.Helpfuls.Any(h => h.UserId == currentUserId);
        CreatedAt = r.CreatedAt;
        Replies = r.Replies
            .Where(x => !x.IsDeleted)
            .OrderBy(x => x.CreatedAt)
            .Select(x => new ReviewReplyResponse(x, instructorIds))
            .ToList();
    }
}

public class ReviewReplyResponse
{
    public long Id { get; set; }
    public long ReviewId { get; set; }
    public long UserId { get; set; }
    public string Author { get; set; } = null!;
    public string? AvatarUrl { get; set; }
    public bool IsInstructor { get; set; }
    public string Content { get; set; } = null!;
    public DateTime CreatedAt { get; set; }

    public ReviewReplyResponse(CourseReviewReply x, HashSet<long> instructorIds)
    {
        Id = x.Id;
        ReviewId = x.ReviewId;
        UserId = x.UserId;
        Author = x.User?.FullName ?? x.User?.UserName ?? x.User?.Email ?? "Người dùng";
        AvatarUrl = x.User?.AvatarUrl;
        IsInstructor = instructorIds.Contains(x.UserId);
        Content = x.Content;
        CreatedAt = x.CreatedAt;
    }
}
