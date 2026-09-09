using StudyCourseAPI.DTOs.Requests.Admin;
using StudyCourseAPI.DTOs.Responses.Admin;

namespace StudyCourseAPI.Services;

/// <summary>Use case của Language (kèm đồng bộ liên kết Language ↔ Framework).</summary>
public interface ILanguageService
{
    Task<List<LanguageResponse>> GetListAsync();

    Task<LanguageResponse?> GetByIdAsync(long id);

    Task<ServiceResult<LanguageResponse>> CreateAsync(LanguageRequest model);

    Task<ServiceResult<LanguageResponse>> UpdateAsync(long id, LanguageRequest model);

    /// <summary>Soft-delete 1 language. False nếu không tìm thấy.</summary>
    Task<bool> SoftDeleteAsync(long id);
}
