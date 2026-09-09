using StudyCourseAPI.DTOs.Requests;
using StudyCourseAPI.DTOs.Responses;

namespace StudyCourseAPI.Services;

/// <summary>
/// Các tác vụ học tập chạy qua AI. Service lo dựng prompt, gọi model và bóc tách kết quả;
/// controller chỉ validate input và trả response.
/// </summary>
public interface IAiAssistantService
{
    Task<AiResponseDto> GenerateAsync(AiPromptRequest request);

    Task<LessonExplanationResponseDto> ExplainLessonAsync(LessonExplanationRequest request);

    Task<QuizGenerationResponseDto> GenerateQuizAsync(QuizGeneratorRequest request);

    Task<HomeworkAssistantResponseDto> AssistHomeworkAsync(HomeworkAssistantRequest request);

    Task<CodeReviewResponseDto> ReviewCodeAsync(CodeReviewRequest request);
}
