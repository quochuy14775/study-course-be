using StudyCourseAPI.DTOs.Requests.Admin;
using StudyCourseAPI.DTOs.Responses.Admin;

namespace StudyCourseAPI.Services;

/// <summary>
/// Bài viết do người dùng đăng. Quyền sửa/xoá là "chủ bài viết hoặc admin", nên các
/// method ghi nhận thêm cờ isAdmin — controller lấy cờ đó từ claim, service không
/// đụng tới HttpContext.
/// </summary>
public interface IArticleService
{
    Task<List<ArticleResponse>> SearchAsync(string? category, string? search);

    Task<ArticleResponse?> GetByIdAsync(long id);

    Task<ArticleResponse?> GetBySlugAsync(string slug);

    Task<List<string>> GetCategoriesAsync();

    /// <summary>Bài viết của chính user. Null nếu không xác định được email user.</summary>
    Task<List<ArticleResponse>?> GetMineAsync();

    Task<ServiceResult<ArticleResponse>> CreateAsync(ArticleRequest model);

    Task<ServiceResult<ArticleResponse>> UpdateAsync(long id, ArticleRequest model, bool isAdmin);

    Task<ServiceResult<bool>> SoftDeleteAsync(long id, bool isAdmin);

    /// <summary>Tăng lượt xem (atomic). Null nếu bài viết không tồn tại.</summary>
    Task<int?> IncrementViewAsync(long id);
}
