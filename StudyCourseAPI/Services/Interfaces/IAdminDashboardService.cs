using StudyCourseAPI.DTOs.Responses.Admin;

namespace StudyCourseAPI.Services;

/// <summary>
/// Số liệu tổng hợp cho trang Tổng quan của admin. Chỉ đọc.
/// </summary>
public interface IAdminDashboardService
{
    /// <summary>KPI, chuỗi theo ngày, phễu, chất lượng học tập, heatmap, top... theo khoảng 7d | 30d | 90d.</summary>
    Task<AdminDashboardResponse> GetDashboardAsync(string range);

    /// <summary>Việc tồn đọng: khóa thiếu nội dung, câu hỏi chưa trả lời, review thấp chưa phản hồi.</summary>
    Task<AdminInboxResponse> GetInboxAsync();

    /// <summary>Dòng hoạt động gần đây, phân trang bằng cursor thời gian.</summary>
    Task<AdminActivityResponse> GetActivityAsync(DateTime? before, int limit);
}
