using StudyCourseAPI.DTOs.Requests;
using StudyCourseAPI.Models;

namespace StudyCourseAPI.Extensions
{
    public static class QuestionExtensions
    {
        public static (bool Success, Dictionary<string, List<string>>? Errors)
            ValidateQuestion(this QuestionRequest model)
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

            return (errors.Count == 0, errors.Count == 0 ? null : errors);
        }

        public static LessonQuestion GetQuestion(this QuestionRequest model, long lessonId, long userId)
        {
            return new LessonQuestion
            {
                LessonId = lessonId,
                UserId = userId,
                Content = model.Content,
            };
        }

        public static (bool Success, Dictionary<string, List<string>>? Errors)
            ValidateAnswer(this AnswerRequest model)
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

            return (errors.Count == 0, errors.Count == 0 ? null : errors);
        }

        public static QuestionAnswer GetAnswer(this AnswerRequest model, long questionId, long userId)
        {
            return new QuestionAnswer
            {
                QuestionId = questionId,
                UserId = userId,
                Content = model.Content,
            };
        }
    }
}
