using Microsoft.EntityFrameworkCore;
using StudyCourseAPI.DTOs.Requests.Admin;
using StudyCourseAPI.Models;
using StudyCourseAPI.Repositories;

namespace StudyCourseAPI.Extensions
{
    public static class SkillExtensions
    {
        public static async Task<(bool Success, Dictionary<string, List<string>>? Errors)>
            ValidateSkillAsync(
                this SkillRequest model,
                IRepository<Skill> repository,
                long? excludeId = null)
        {
            var errors = new Dictionary<string, List<string>>();

            void Add(string key, string msg)
            {
                if (!errors.ContainsKey(key)) errors[key] = new List<string>();
                errors[key].Add(msg);
            }

            if (string.IsNullOrWhiteSpace(model.Name))
                Add("name", "Skill name is required.");
            else if (model.Name.Length > 128)
                Add("name", "Skill name cannot exceed 128 characters.");

            if (!string.IsNullOrEmpty(model.Description) && model.Description.Length > 1000)
                Add("description", "Description cannot exceed 1000 characters.");

            if (!string.IsNullOrEmpty(model.IconUrl) && model.IconUrl.Length > 500)
                Add("iconUrl", "IconUrl cannot exceed 500 characters.");

            // Check duplicate name
            if (!string.IsNullOrWhiteSpace(model.Name))
            {
                var query = repository.Query().Where(x => x.Name.ToLower() == model.Name.ToLower() && !x.IsDeleted);
                if (excludeId.HasValue) query = query.Where(x => x.Id != excludeId.Value);
                if (await query.AnyAsync())
                    Add("name", "A skill with this name already exists.");
            }

            return (errors.Count == 0, errors.Count == 0 ? null : errors);
        }

        public static Skill GetSkill(this SkillRequest model)
        {
            return new Skill
            {
                Name = model.Name,
                Description = model.Description,
                IconUrl = model.IconUrl,
                IsActive = model.IsActive
            };
        }

        public static void MapTo(this SkillRequest model, Skill entity)
        {
            entity.Name = model.Name;
            entity.Description = model.Description;
            entity.IconUrl = model.IconUrl;
            entity.IsActive = model.IsActive;
        }

        public static async Task SyncSkillsAsync(
            this Course course,
            IRepository<CourseSkill> courseSkillRepository,
            IRepository<Skill> skillRepository,
            List<CourseSkillRequest> skillRequests)
        {
            // 1. Remove old relations
            var existing = await courseSkillRepository.Query()
                .Where(x => x.CourseId == course.Id)
                .ToListAsync();

            foreach (var item in existing)
            {
                courseSkillRepository.Remove(item);
            }

            // 2. Add new relations
            if (skillRequests != null && skillRequests.Any())
            {
                var skillIds = skillRequests.Select(r => r.SkillId).ToList();
                var validSkillIds = await skillRepository.Query()
                    .Where(s => skillIds.Contains(s.Id) && !s.IsDeleted)
                    .Select(s => s.Id)
                    .ToListAsync();

                foreach (var req in skillRequests)
                {
                    if (validSkillIds.Contains(req.SkillId))
                    {
                        courseSkillRepository.Add(new CourseSkill
                        {
                            CourseId = course.Id,
                            SkillId = req.SkillId,
                            ContributionPercentage = req.ContributionPercentage
                        });
                    }
                }
            }
        }
    }
}

