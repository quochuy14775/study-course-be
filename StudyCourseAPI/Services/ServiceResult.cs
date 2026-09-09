namespace StudyCourseAPI.Services;

/// <summary>
/// Kết quả trả về từ service layer. Service không biết gì về HTTP, nên thay vì trả
/// IActionResult nó trả về một trong các trạng thái dưới đây; controller đọc trạng thái
/// rồi tự quyết định trả 200 / 404 / 400.
/// </summary>
public class ServiceResult<T>
{
    public T? Data { get; }

    /// <summary>Lỗi validate nhiều field — controller render qua ControllerBase.ValidationFailed().</summary>
    public Dictionary<string, List<string>>? Errors { get; }

    /// <summary>Lỗi một câu — controller render thành { status = 400, message }.</summary>
    public string? ErrorMessage { get; }

    /// <summary>Danh sách lỗi dạng phẳng (thường từ IdentityResult) — trả nguyên mảng string.</summary>
    public List<string>? ErrorMessages { get; }

    public bool IsNotFound { get; }

    /// <summary>Message kèm theo 404, nếu endpoint gốc có trả body. Null = 404 rỗng.</summary>
    public string? NotFoundMessage { get; }

    /// <summary>User đăng nhập rồi nhưng không có quyền trên tài nguyên này → 403.</summary>
    public bool IsForbidden { get; }

    public bool IsSuccess =>
        !IsNotFound && !IsForbidden && Errors is null && ErrorMessage is null && ErrorMessages is null;

    private ServiceResult(
        T? data,
        Dictionary<string, List<string>>? errors,
        string? errorMessage,
        List<string>? errorMessages,
        bool isNotFound,
        string? notFoundMessage,
        bool isForbidden)
    {
        Data = data;
        Errors = errors;
        ErrorMessage = errorMessage;
        ErrorMessages = errorMessages;
        IsNotFound = isNotFound;
        NotFoundMessage = notFoundMessage;
        IsForbidden = isForbidden;
    }

    public static ServiceResult<T> Ok(T data) => new(data, null, null, null, false, null, false);

    public static ServiceResult<T> Invalid(Dictionary<string, List<string>>? errors)
        => new(default, errors ?? new Dictionary<string, List<string>>(), null, null, false, null, false);

    public static ServiceResult<T> Invalid(string message)
        => new(default, null, message, null, false, null, false);

    public static ServiceResult<T> Invalid(List<string> messages)
        => new(default, null, null, messages, false, null, false);

    public static ServiceResult<T> NotFound(string? message = null)
        => new(default, null, null, null, true, message, false);

    public static ServiceResult<T> Forbidden()
        => new(default, null, null, null, false, null, true);
}
