using StudyCourseAPI.DTOs.Requests;
using StudyCourseAPI.DTOs.Responses;

namespace StudyCourseAPI.Services;

/// <summary>Đánh giá khoá học: rating, reply của giảng viên, và đánh dấu "hữu ích".</summary>
public interface IReviewService
{
    Task<List<ReviewResponse>> GetByCourseAsync(long courseId);

    /// <summary>Thống kê điểm trung bình và phân bố sao của 1 course.</summary>
    Task<RatingBreakdownResponse> GetSummaryAsync(long courseId);

    Task<ServiceResult<ReviewResponse>> CreateAsync(long courseId, ReviewRequest model);

    /// <summary>Soft-delete review của chính user và refresh lại rating của course.</summary>
    Task<bool> SoftDeleteAsync(long courseId, long reviewId);

    /// <summary>Bật/tắt đánh dấu hữu ích. Null nếu review không tồn tại.</summary>
    Task<(bool MarkedHelpful, int HelpfulCount)?> ToggleHelpfulAsync(long courseId, long reviewId);

    Task<ServiceResult<ReviewReplyResponse>> AddReplyAsync(long courseId, long reviewId, ReviewReplyRequest model);
}
