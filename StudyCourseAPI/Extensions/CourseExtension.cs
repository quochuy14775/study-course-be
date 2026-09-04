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
        }

        public static Task SyncLanguagesAsync(
            this Course course,
            IRepository<CourseLanguage> courseLanguageRepository,
            IRepository<Language> languageRepository,
            List<long>? targetLanguageIds)
        {
            targetLanguageIds ??= new List<long>();

            var currentTask = courseLanguageRepository.Query()
                .Where(cl => cl.CourseId == course.Id)
                .ToListAsync();

            var validIdsTask = targetLanguageIds.Count == 0
                ? Task.FromResult(new List<long>())
                : languageRepository.Query()
                    .Where(l => targetLanguageIds.Contains(l.Id) && !l.IsDeleted)
                    .Select(l => l.Id)
                    .ToListAsync();

            return courseLanguageRepository.SyncLinksAsync(
                currentTask,
                validIdsTask,
                cl => cl.LanguageId,
                langId => new CourseLanguage { CourseId = course.Id, LanguageId = langId });
        }

        public static Task SyncFrameworksAsync(
            this Course course,
            IRepository<CourseFramework> courseFrameworkRepository,
            IRepository<Framework> frameworkRepository,
            List<long>? targetFrameworkIds)
        {
            targetFrameworkIds ??= new List<long>();

            var currentTask = courseFrameworkRepository.Query()
                .Where(cf => cf.CourseId == course.Id)
                .ToListAsync();

            var validIdsTask = targetFrameworkIds.Count == 0
                ? Task.FromResult(new List<long>())
                : frameworkRepository.Query()
                    .Where(f => targetFrameworkIds.Contains(f.Id) && !f.IsDeleted)
                    .Select(f => f.Id)
                    .ToListAsync();

            return courseFrameworkRepository.SyncLinksAsync(
                currentTask,
                validIdsTask,
                cf => cf.FrameworkId,
                fwId => new CourseFramework { CourseId = course.Id, FrameworkId = fwId });
        }

        public static Task SyncTagsAsync(
            this Course course,
            IRepository<CourseTag> courseTagRepository,
            IRepository<Tag> tagRepository,
            List<long>? targetTagIds)
        {
            targetTagIds ??= new List<long>();

            var currentTask = courseTagRepository.Query()
                .Where(ct => ct.CourseId == course.Id)
                .ToListAsync();

            var validIdsTask = targetTagIds.Count == 0
                ? Task.FromResult(new List<long>())
                : tagRepository.Query()
                    .Where(t => targetTagIds.Contains(t.Id) && !t.IsDeleted)
                    .Select(t => t.Id)
                    .ToListAsync();

            return courseTagRepository.SyncLinksAsync(
                currentTask,
                validIdsTask,
                ct => ct.TagId,
                tagId => new CourseTag { CourseId = course.Id, TagId = tagId });
        }
    }
}
