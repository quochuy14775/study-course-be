using StudyCourseAPI.DTOs.Requests;
using StudyCourseAPI.DTOs.Requests.Admin;
using StudyCourseAPI.Enums;
using StudyCourseAPI.Models;

namespace StudyCourseAPI.Extensions;

public static class QuizExtensions
{
    public static (bool Success, Dictionary<string, List<string>>? Errors) ValidateQuiz(this QuizRequest model)
    {
        var errors = new Dictionary<string, List<string>>();

        void Add(string key, string msg)
        {
            if (!errors.ContainsKey(key)) errors[key] = new List<string>();
            errors[key].Add(msg);
        }

        if (string.IsNullOrWhiteSpace(model.Title))
            Add("title", "Title is required.");

        if (model.PassPercentage < 1 || model.PassPercentage > 100)
            Add("passPercentage", "Pass percentage must be between 1 and 100.");

        if (model.Questions == null || model.Questions.Count == 0)
        {
            Add("questions", "Quiz must have at least 1 question.");
            return (errors.Count == 0, errors.Count == 0 ? null : errors);
        }

        for (var i = 0; i < model.Questions.Count; i++)
        {
            var q = model.Questions[i];
            var prefix = $"questions[{i}]";

            if (string.IsNullOrWhiteSpace(q.Content))
                Add(prefix, "Question content is required.");

            if (q.Options == null || q.Options.Count < 2)
            {
                Add(prefix, "Question must have at least 2 options.");
                continue;
            }

            var correctCount = q.Options.Count(o => o.IsCorrect);
            if (correctCount != 1)
                Add(prefix, "Question must have exactly 1 correct option.");
        }

        return (errors.Count == 0, errors.Count == 0 ? null : errors);
    }

    public static Quiz ToEntity(this QuizRequest model, QuizType quizType, long courseId, long? lessonId)
    {
        var entity = new Quiz
        {
            QuizType = quizType,
            CourseId = courseId,
            LessonId = lessonId,
        };
        model.MapTo(entity);
        return entity;
    }

    /// <summary>Replaces the entire question/option set — quiz content is admin-authored and small, so full replace is simpler than diffing.</summary>
    public static void MapTo(this QuizRequest model, Quiz entity)
    {
        entity.Title = model.Title;
        entity.PassPercentage = model.PassPercentage;
        entity.TimeLimitMinutes = model.TimeLimitMinutes;

        entity.Questions = model.Questions.Select((q, qi) => new QuizQuestion
        {
            Content = q.Content,
            OrderIndex = q.OrderIndex != 0 ? q.OrderIndex : qi + 1,
            Points = q.Points,
            Options = q.Options.Select((o, oi) => new QuizOptionItem
            {
                OptionId = oi + 1,
                Content = o.Content,
                IsCorrect = o.IsCorrect,
                OrderIndex = o.OrderIndex != 0 ? o.OrderIndex : oi + 1,
            }).ToList(),
        }).ToList();
    }

    /// <summary>Grades a submission against the quiz and produces the attempt entity (not yet saved).</summary>
    public static QuizAttempt Grade(this Quiz quiz, SubmitQuizAttemptRequest submission, long userId, int nextAttemptNumber)
    {
        var answersByQuestion = submission.Answers.ToDictionary(a => a.QuestionId, a => a.SelectedOptionId);

        var snapshots = new List<QuizAnswerSnapshot>();
        var correctCount = 0;

        foreach (var question in quiz.Questions.OrderBy(q => q.OrderIndex))
        {
            var correctOption = question.Options.First(o => o.IsCorrect);
            answersByQuestion.TryGetValue(question.Id, out var selectedOptionId);
            var selectedOption = question.Options.FirstOrDefault(o => o.OptionId == selectedOptionId);
            var isCorrect = selectedOption != null && selectedOption.IsCorrect;
            if (isCorrect) correctCount++;

            snapshots.Add(new QuizAnswerSnapshot
            {
                QuestionId = question.Id,
                QuestionContent = question.Content,
                SelectedOptionId = selectedOption?.OptionId,
                SelectedOptionContent = selectedOption?.Content,
                CorrectOptionId = correctOption.OptionId,
                CorrectOptionContent = correctOption.Content,
                IsCorrect = isCorrect,
            });
        }

        var total = quiz.Questions.Count;
        var percentage = total > 0 ? Math.Round((double)correctCount / total * 100, 2) : 0;

        return new QuizAttempt
        {
            QuizId = quiz.Id,
            UserId = userId,
            AttemptNumber = nextAttemptNumber,
            CorrectCount = correctCount,
            TotalCount = total,
            PercentageScore = percentage,
            IsPassed = percentage >= quiz.PassPercentage,
            SubmittedAt = DateTime.UtcNow,
            Answers = snapshots,
        };
    }
}
