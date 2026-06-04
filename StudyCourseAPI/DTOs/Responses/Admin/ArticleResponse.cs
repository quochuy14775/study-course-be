using StudyCourseAPI.Models;

namespace StudyCourseAPI.DTOs.Responses.Admin;

public class ArticleResponse
{
    public long Id { get; set; }
    public string Title { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public string? Excerpt { get; set; }
    public string? Content { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string? Author { get; set; }
    public string? Category { get; set; }
    public int ReadTimeMinutes { get; set; }
    public int ViewCount { get; set; }
    public int LikeCount { get; set; }
    public bool IsFeatured { get; set; }
    public bool IsActive { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public ArticleResponse(Article a)
    {
        Id              = a.Id;
        Title           = a.Title;
        Slug            = a.Slug;
        Excerpt         = a.Excerpt;
        Content         = a.Content;
        ThumbnailUrl    = a.ThumbnailUrl;
        Author          = a.Author;
        Category        = a.Category;
        ReadTimeMinutes = a.ReadTimeMinutes;
        ViewCount       = a.ViewCount;
        LikeCount       = a.LikeCount;
        IsFeatured      = a.IsFeatured;
        IsActive        = a.IsActive;
        CreatedBy       = a.CreatedBy;
        CreatedAt       = a.CreatedAt;
        UpdatedAt       = a.UpdatedAt;
    }
}
