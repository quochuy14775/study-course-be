namespace StudyCourseAPI.DTOs.Requests.Admin;

public class ArticleRequest
{
    public string Title { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public string? Excerpt { get; set; }
    public string? Content { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string? Author { get; set; }
    public string? Category { get; set; }
    public int ReadTimeMinutes { get; set; } = 5;
    public bool IsFeatured { get; set; } = false;
    public bool IsActive { get; set; } = true;
}
