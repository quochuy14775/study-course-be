using Microsoft.EntityFrameworkCore;
using StudyCourseAPI.DTOs.Requests.Admin;
using StudyCourseAPI.Models;
using StudyCourseAPI.Repositories;

namespace StudyCourseAPI.Extensions;

public static class RoadmapExtensions
{
    public static async Task<(bool Success, Dictionary<string, List<string>>? Errors)>
        ValidateRoadmapAsync(
            this RoadmapRequest model,
            IRepository<Roadmap> repository,
            long? excludeId = null)
    {
        var errors = new Dictionary<string, List<string>>();

        void Add(string key, string msg)
        {
            if (!errors.ContainsKey(key)) errors[key] = new List<string>();
            errors[key].Add(msg);
        }

        if (string.IsNullOrWhiteSpace(model.Title))
            Add("title", "Roadmap title is required.");
        else if (model.Title.Length > 255)
            Add("title", "Roadmap title cannot exceed 255 characters.");

        if (!string.IsNullOrEmpty(model.Description) && model.Description.Length > 2000)
            Add("description", "Description cannot exceed 2000 characters.");

        if (!string.IsNullOrWhiteSpace(model.Title))
        {
            var normalized = model.Title.Trim().ToUpper();
            var exists = await repository.Query()
                .FirstOrDefaultAsync(r =>
                    r.Title.ToUpper() == normalized &&
                    !r.IsDeleted &&
                    r.Id != excludeId);

            if (exists != null)
                Add("title", "A roadmap with this title already exists.");
        }

        return errors.Any() ? (false, errors) : (true, null);
    }

    public static Roadmap GetRoadmap(this RoadmapRequest model)
    {
        return new Roadmap
        {
            Title = model.Title.Trim(),
            Description = model.Description?.Trim(),
            IsActive = model.IsActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
    }

    public static async Task SyncCoursesAsync(
        this Roadmap roadmap,
        IRepository<RoadmapCourse> roadmapCourseRepository,
        IRepository<Course> courseRepository,
        List<long> targetCourseIds)
    {
        // Validate that provided course ids actually exist
        var validIds = await courseRepository.Query()
            .Where(c => targetCourseIds.Contains(c.Id) && !c.IsDeleted)
            .Select(c => c.Id)
            .ToListAsync();

        var currentLinks = await roadmapCourseRepository.Query()
            .Where(rc => rc.RoadmapId == roadmap.Id)
            .ToListAsync();

        var currentCourseIds = currentLinks.Select(rc => rc.CourseId).ToHashSet();
        var targetSet = validIds.ToHashSet();

        foreach (var link in currentLinks)
        {
            if (!targetSet.Contains(link.CourseId))
                await roadmapCourseRepository.DeleteAsync(link);
        }

        for (var i = 0; i < validIds.Count; i++)
        {
            var courseId = validIds[i];
            if (!currentCourseIds.Contains(courseId))
            {
                roadmapCourseRepository.Add(new RoadmapCourse
                {
                    RoadmapId = roadmap.Id,
                    CourseId = courseId,
                    OrderIndex = i,
                });
            }
            else
            {
                // Update order of existing link
                var existing = currentLinks.First(rc => rc.CourseId == courseId);
                existing.OrderIndex = i;
            }
        }
    }
}
