using Microsoft.AspNetCore.Diagnostics;

namespace StudyCourseAPI.Middleware;

/// <summary>
/// Catches any exception a controller doesn't handle itself and turns it into the
/// same { status, message } envelope the app already uses for validation errors,
/// instead of leaking a stack trace or an empty 500.
/// </summary>
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Unhandled exception on {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        httpContext.Response.ContentType = "application/json";

        await httpContext.Response.WriteAsJsonAsync(new
        {
            status = 500,
            message = "An unexpected error occurred. Please try again later."
        }, cancellationToken);

        return true;
    }
}
