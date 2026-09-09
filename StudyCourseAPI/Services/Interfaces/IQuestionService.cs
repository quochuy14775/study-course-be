using StudyCourseAPI.DTOs.Requests;
using StudyCourseAPI.DTOs.Responses;

namespace StudyCourseAPI.Services;

/// <summary>Hỏi đáp dưới mỗi lesson: câu hỏi, câu trả lời, like và chấp nhận đáp án.</summary>
public interface IQuestionService
{
    Task<List<QuestionResponse>> GetByLessonAsync(long lessonId);

    Task<ServiceResult<QuestionResponse>> CreateAsync(long lessonId, QuestionRequest model);

    /// <summary>Soft-delete câu hỏi của chính user. False nếu không tìm thấy.</summary>
    Task<bool> SoftDeleteAsync(long lessonId, long questionId);

    /// <summary>Đảo trạng thái đã giải quyết (chỉ chủ câu hỏi). Null nếu không tìm thấy.</summary>
    Task<bool?> ToggleResolvedAsync(long lessonId, long questionId);

    /// <summary>Trả lời 1 câu hỏi; bắn thông báo cho chủ câu hỏi.</summary>
    Task<ServiceResult<AnswerResponse>> AddAnswerAsync(long lessonId, long questionId, AnswerRequest model);
}

/// <summary>Thao tác trên câu trả lời, không phụ thuộc lesson nào.</summary>
public interface IAnswerService
{
    /// <summary>Bật/tắt like. Null nếu câu trả lời không tồn tại.</summary>
    Task<(bool Liked, int LikeCount)?> ToggleLikeAsync(long answerId);

    /// <summary>Chấp nhận / bỏ chấp nhận đáp án — chỉ chủ câu hỏi mới được làm.</summary>
    Task<ServiceResult<bool>> ToggleAcceptedAsync(long answerId);

    /// <summary>Soft-delete câu trả lời của chính user. False nếu không tìm thấy.</summary>
    Task<bool> SoftDeleteAsync(long answerId);
}
