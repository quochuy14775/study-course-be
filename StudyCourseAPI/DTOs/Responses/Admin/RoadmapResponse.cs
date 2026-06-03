using StudyCourseAPI.Models;

namespace StudyCourseAPI.DTOs.Responses.Admin;

public class RoadmapChapterResponse
{
    public long Id { get; set; }
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public int OrderIndex { get; set; }

    public RoadmapChapterResponse(Chapter chapter)
    {
        Id = chapter.Id;
        Title = chapter.Title;
        Description = chapter.Description;
        OrderIndex = chapter.OrderIndex;
    }
}

public class RoadmapCourseResponse
{
    public long Id { get; set; }
    public string Title { get; set; } = null!;
    public string? Subtitle { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public string Level { get; set; } = null!;
    public decimal Price { get; set; }
    public int TotalDurationSeconds { get; set; }
    public int LessonCount { get; set; }
    public int ChapterCount { get; set; }
    public int OrderIndex { get; set; }

    public List<RoadmapChapterResponse> Chapters { get; set; } = new();

    public RoadmapCourseResponse(RoadmapCourse rc)
    {
        var c = rc.Course;
        Id = c.Id;
        Title = c.Title;
        Subtitle = c.Subtitle;
        Description = c.Description;
        ImageUrl = c.ImageUrl;
        Level = c.Level.ToString();
        Price = c.Price;
        TotalDurationSeconds = c.TotalDurationSeconds;
        LessonCount = c.LessonCount;
        ChapterCount = c.ChapterCount;
        OrderIndex = rc.OrderIndex;

        if (c.Chapters != null)
            Chapters = c.Chapters
                .Where(ch => !ch.IsDeleted)
                .OrderBy(ch => ch.OrderIndex)
                .Select(ch => new RoadmapChapterResponse(ch))
                .ToList();
    }
}

public class RoadmapResponse
{
    public long Id { get; set; }
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public int CourseCount { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }

    public List<RoadmapCourseResponse> Courses { get; set; } = new();

    public RoadmapResponse(Roadmap roadmap)
    {
        Id = roadmap.Id;
        Title = roadmap.Title;
        Description = roadmap.Description;
        IsActive = roadmap.IsActive;
        CreatedAt = roadmap.CreatedAt;
        UpdatedAt = roadmap.UpdatedAt;
        CreatedBy = roadmap.CreatedBy;
        UpdatedBy = roadmap.UpdatedBy;

        if (roadmap.RoadmapCourses != null)
        {
            Courses = roadmap.RoadmapCourses
                .OrderBy(rc => rc.OrderIndex)
                .Select(rc => new RoadmapCourseResponse(rc))
                .ToList();
            CourseCount = Courses.Count;
        }
    }
}
