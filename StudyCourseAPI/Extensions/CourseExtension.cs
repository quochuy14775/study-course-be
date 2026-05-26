using Microsoft.EntityFrameworkCore;
using StudyCourseAPI.DTOs.Requests.Admin;
using StudyCourseAPI.Enums;
using StudyCourseAPI.Models;
using StudyCourseAPI.Repositories;

namespace StudyCourseAPI.Extensions
{
    public static class CourseExtensions
    {
        public static async Task<(bool Success, Dictionary<string, List<string>>? Errors)>
            ValidateCourseAsync(
                this CourseRequest model,
                IRepository<Course> repository,
                long? excludeId = null)
        {
            var errors = new Dictionary<string, List<string>>();

            void Add(string key, string msg)
            {
                if (!errors.ContainsKey(key)) errors[key] = new List<string>();
                errors[key].Add(msg);
            }

            // Title
            if (string.IsNullOrWhiteSpace(model.Title))
                Add("title", "Course title is required.");
            else if (model.Title.Length > 255)
                Add("title", "Course title cannot exceed 255 characters.");

            // Subtitle
            if (!string.IsNullOrEmpty(model.Subtitle) && model.Subtitle.Length > 500)
                Add("subtitle", "Subtitle cannot exceed 500 characters.");

            // Description
            if (string.IsNullOrWhiteSpace(model.Description))
                Add("description", "Description is required.");
            else if (model.Description.Length > 2000)
                Add("description", "Description cannot exceed 2000 characters.");

            // ImageUrl
            if (!string.IsNullOrEmpty(model.ImageUrl) && model.ImageUrl.Length > 500)
                Add("imageUrl", "ImageUrl cannot exceed 500 characters.");

            // Price
            if (model.Price < 0)
                Add("price", "Price must be greater than or equal to 0.");

            // Level enum
            if (!Enum.IsDefined(typeof(CourseLevel), model.Level))
                Add("level", "Must choose a valid course level.");

            // Duplicate title (case-insensitive)
            if (!string.IsNullOrWhiteSpace(model.Title))
            {
                var normalizedTitle = model.Title.Trim().ToUpper();
                var existingCourse = await repository.Query()
                    .FirstOrDefaultAsync(c =>
                        c.Title.ToUpper() == normalizedTitle &&
                        !c.IsDeleted &&
                        c.Id != excludeId);

                if (existingCourse != null)
                    Add("title", "A course with this title already exists.");
            }

            if (errors.Any())
                return (false, errors);

            return (true, null);
        }

        public static Course GetCourse(this CourseRequest model)
        {
            return new Course
            {
                Title = model.Title!.Trim(),
                Subtitle = model.Subtitle?.Trim(),
                Description = model.Description!.Trim(),
                ImageUrl = model.ImageUrl?.Trim(),
                Price = model.Price,
                Level = model.Level,
                IsFeatured = model.IsFeatured,
                IsActive = model.IsActive,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };
        }

        public static void ToEntity(this CourseRequest model, Course entity)
        {
            entity.Title = model.Title!.Trim();
            entity.Subtitle = model.Subtitle?.Trim();
            entity.Description = model.Description!.Trim();
            entity.ImageUrl = model.ImageUrl?.Trim();
            entity.Price = model.Price;
            entity.Level = model.Level;
            entity.IsFeatured = model.IsFeatured;
            entity.IsActive = model.IsActive;
            entity.UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Sync course tags to the provided list of tag ids.
        /// Removes tags not in the list and adds missing ones.
        /// </summary>
        public static async Task SyncTagsAsync(
            this Course course,
            IRepository<CourseTag> courseTagRepository,
            IRepository<Tag> tagRepository,
            List<long>? targetTagIds)
        {
            targetTagIds ??= new List<long>();

            // Validate provided tag ids exist
            if (targetTagIds.Any())
            {
                var existingIds = await tagRepository.Query()
                    .Where(t => targetTagIds.Contains(t.Id) && !t.IsDeleted)
                    .Select(t => t.Id)
                    .ToListAsync();
                targetTagIds = existingIds;
            }

            var currentLinks = await courseTagRepository.Query()
                .Where(ct => ct.CourseId == course.Id)
                .ToListAsync();
            var currentIds = currentLinks.Select(ct => ct.TagId).ToHashSet();
            var targetIds = targetTagIds.ToHashSet();

            // Remove links no longer in target
            foreach (var link in currentLinks)
            {
                if (!targetIds.Contains(link.TagId))
                    await courseTagRepository.DeleteAsync(link);
            }

            // Add new links
            foreach (var tagId in targetIds)
            {
                if (!currentIds.Contains(tagId))
                    courseTagRepository.Add(new CourseTag { CourseId = course.Id, TagId = tagId });
            }
        }
    }
}
