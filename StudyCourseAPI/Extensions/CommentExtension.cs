using Microsoft.EntityFrameworkCore;
using StudyCourseAPI.DTOs.Requests;
using StudyCourseAPI.Models;
using StudyCourseAPI.Repositories;

namespace StudyCourseAPI.Extensions
{
    public static class CommentExtensions
    {
        public static async Task<(bool Success, Dictionary<string, List<string>>? Errors)>
            ValidateCommentAsync(
                this CommentRequest model,
                IRepository<LessonComment> repository,
                long lessonId)
        {
            var errors = new Dictionary<string, List<string>>();

            void Add(string key, string msg)
            {
                if (!errors.ContainsKey(key)) errors[key] = new List<string>();
                errors[key].Add(msg);
            }

            if (string.IsNullOrWhiteSpace(model.Content))
                Add("content", "Content is required.");
            else if (model.Content.Length > 2000)
                Add("content", "Content cannot exceed 2000 characters.");

            if (model.ParentCommentId.HasValue)
            {
                var parentExists = await repository.Query()
                    .AnyAsync(c => c.Id == model.ParentCommentId.Value && c.LessonId == lessonId && !c.IsDeleted);
                if (!parentExists)
                    Add("parentCommentId", "Parent comment not found.");
            }

            return (errors.Count == 0, errors.Count == 0 ? null : errors);
        }

        public static LessonComment GetComment(this CommentRequest model, long lessonId, long userId)
        {
            return new LessonComment
            {
                LessonId = lessonId,
                UserId = userId,
                Content = model.Content,
                ParentCommentId = model.ParentCommentId,
            };
        }
    }
}
