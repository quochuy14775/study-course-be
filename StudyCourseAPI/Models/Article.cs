namespace StudyCourseAPI.Models;

public class Article : BaseEntity<long>, IAuditable, IUserTracking
{
    public string Title { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public string? Excerpt { get; set; }
    public string? Content { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string? Author { get; set; }
    public string? Category { get; set; }
    public int ReadTimeMinutes { get; set; } = 5;
    public int ViewCount { get; set; } = 0;
    public int LikeCount { get; set; } = 0;
    public bool IsFeatured { get; set; } = false;

    // Audit
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public bool IsActive { get; set; } = true;

    // Ownership (IUserTracking)
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
}
