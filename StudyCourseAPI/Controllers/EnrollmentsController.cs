using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudyCourseAPI.Services;

namespace StudyCourseAPI.Controllers;

/// <summary>
/// Ghi danh khóa học. Enrollment cũng được tạo ngầm khi user hoàn thành bài học đầu tiên
/// (LessonProgressService) — endpoint này để FE tạo bản ghi ngay lúc bấm "Học ngay", nhờ đó
/// khóa mới xuất hiện trong "Khóa học của tôi" trước cả khi học xong bài nào.
/// </summary>
[ApiController]
[Authorize]
public class EnrollmentsController : ControllerBase
{
    private readonly IEnrollmentService _enrollmentService;

    public EnrollmentsController(IEnrollmentService enrollmentService)
    {
        _enrollmentService = enrollmentService;
    }

    // ── POST api/courses/{courseId}/enroll — idempotent ──
    [HttpPost("api/courses/{courseId:long}/enroll")]
    public async Task<IActionResult> Enroll(long courseId)
    {
        var result = await _enrollmentService.EnrollAsync(courseId);

        if (result.IsNotFound) return NotFound(new { status = 404, message = result.NotFoundMessage });
        if (!result.IsSuccess) return BadRequest(new { status = 400, message = result.ErrorMessage });

        return Ok(result.Data);
    }

    // ── GET api/courses/{courseId}/enrollment — 404 = chưa đăng ký ──
    [HttpGet("api/courses/{courseId:long}/enrollment")]
    public async Task<IActionResult> GetForCourse(long courseId)
    {
        var enrollment = await _enrollmentService.GetOwnByCourseAsync(courseId);

        if (enrollment == null) return NotFound();
        return Ok(enrollment);
    }

    // ── GET api/users/me/courses — nguồn dữ liệu cho trang "Khóa học của tôi" ──
    [HttpGet("api/users/me/courses")]
    public async Task<IActionResult> GetMyCourses()
    {
        return Ok(await _enrollmentService.GetMyCoursesAsync());
    }
}
