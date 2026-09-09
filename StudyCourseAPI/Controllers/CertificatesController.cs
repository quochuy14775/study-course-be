using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudyCourseAPI.Models;
using StudyCourseAPI.Services;

namespace StudyCourseAPI.Controllers;

/// <summary>
/// Certificates are issued as a side effect of passing a course test (see QuizController.Submit),
/// never created by hand. This controller only reads (and, for admins, revokes) them.
/// </summary>
[ApiController]
public class CertificatesController : ControllerBase
{
    private readonly ICertificateService _certificateService;

    public CertificatesController(ICertificateService certificateService)
    {
        _certificateService = certificateService;
    }

    // ── Learner: their own certificate for a course ──
    [Authorize]
    [HttpGet("api/courses/{courseId:long}/certificate")]
    public async Task<IActionResult> GetForCourse(long courseId)
    {
        var certificate = await _certificateService.GetOwnByCourseAsync(courseId);

        if (certificate == null) return NotFound();
        return Ok(certificate);
    }

    // ── Admin: list all, optional filter by course + search on learner/code ──
    [Authorize(Roles = AppRoles.Admin)]
    [HttpGet("api/admin/certificates")]
    public async Task<IActionResult> Get([FromQuery] long? courseId, [FromQuery] string? search)
    {
        return Ok(await _certificateService.SearchAsync(courseId, search));
    }

    // ── Public: lookup by verification code ──
    [AllowAnonymous]
    [HttpGet("api/admin/certificates/verify/{code}")]
    public async Task<IActionResult> Verify(string code)
    {
        var certificate = await _certificateService.VerifyAsync(code);

        if (certificate == null) return NotFound();
        return Ok(certificate);
    }

    // ── Admin: revoke a wrongly issued certificate (hard delete: the entity has no soft-delete flags) ──
    [Authorize(Roles = AppRoles.Admin)]
    [HttpDelete("api/admin/certificates/{id:long}")]
    public async Task<IActionResult> Revoke(long id)
    {
        var revoked = await _certificateService.RevokeAsync(id);

        if (!revoked) return NotFound();
        return Ok(new { success = true });
    }
}
