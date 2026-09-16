using StudyCourseAPI.DTOs.Responses;

namespace StudyCourseAPI.Services;

/// <summary>Số liệu công khai cho trang chủ (học viên, khóa, rating, tỉ lệ hoàn thành).</summary>
public interface IPublicStatsService
{
    Task<PublicStatsResponse> GetAsync();
}
