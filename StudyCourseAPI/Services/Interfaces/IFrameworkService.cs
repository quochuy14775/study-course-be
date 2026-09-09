using StudyCourseAPI.DTOs.Requests.Admin;
using StudyCourseAPI.DTOs.Responses.Admin;

namespace StudyCourseAPI.Services;

/// <summary>Use case của Framework (kèm đồng bộ liên kết Language ↔ Framework).</summary>
public interface IFrameworkService
{
    Task<List<FrameworkResponse>> GetListAsync();

    Task<FrameworkResponse?> GetByIdAsync(long id);

    Task<ServiceResult<FrameworkResponse>> CreateAsync(FrameworkRequest model);

    Task<ServiceResult<FrameworkResponse>> UpdateAsync(long id, FrameworkRequest model);

    /// <summary>Soft-delete 1 framework. False nếu không tìm thấy.</summary>
    Task<bool> SoftDeleteAsync(long id);
}
