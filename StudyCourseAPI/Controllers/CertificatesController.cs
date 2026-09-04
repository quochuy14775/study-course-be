using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudyCourseAPI.DTOs.Responses;
using StudyCourseAPI.DTOs.Responses.Admin;
using StudyCourseAPI.Models;
using StudyCourseAPI.Repositories;

namespace StudyCourseAPI.Controllers;

/// <summary>
/// Certificates are issued as a side effect of passing a course test (see QuizController.Submit),
/// never created by hand. This controller only reads (and, for admins, revokes) them.
/// </summary>
[ApiController]
public class CertificatesController : BaseController<Certificate>
{
    public CertificatesController(IRepository<Certificate> baseRepository, ICurrentUser currentUser)
        : base(baseRepository, currentUser)
    {
    }

    // ── Learner: their own certificate for a course ──
    [Authorize]
    [HttpGet("api/courses/{courseId:long}/certificate")]
    public async Task<IActionResult> GetForCourse(long courseId)
    {
        var userId = _currentUser.GetCurrentUserId();

        var certificate = await _baseRepository.Query()
            .AsNoTracking()
            .Include(c => c.Course)
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.CourseId == courseId && c.UserId == userId);

        if (certificate == null) return NotFound();
        return Ok(new CertificateResponse(certificate));
    }

    // ── Admin: list all, optional filter by course + search on learner/code ──
    [Authorize(Roles = AppRoles.Admin)]
    [HttpGet("api/admin/certificates")]
    public async Task<IActionResult> Get([FromQuery] long? courseId, [FromQuery] string? search)
    {
        var query = _baseRepository.Query()
            .AsNoTracking()
            .Include(c => c.Course)
            .Include(c => c.User)
            .AsQueryable();

        if (courseId.HasValue)
            query = query.Where(c => c.CourseId == courseId.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search}%";
            query = query.Where(c =>
                EF.Functions.ILike(c.CertificateCode, pattern)
                || (c.User.FullName != null && EF.Functions.ILike(c.User.FullName, pattern))
                || (c.User.UserName != null && EF.Functions.ILike(c.User.UserName, pattern))
                || (c.User.Email != null && EF.Functions.ILike(c.User.Email, pattern)));
        }

        var items = await query
            .OrderByDescending(c => c.IssuedAt)
            .ToListAsync();

        return Ok(items.Select(c => new CertificateAdminResponse(c)));
    }

    // ── Public: lookup by verification code ──
    [AllowAnonymous]
    [HttpGet("api/admin/certificates/verify/{code}")]
    public async Task<IActionResult> Verify(string code)
    {
        var certificate = await _baseRepository.Query()
            .AsNoTracking()
            .Include(c => c.Course)
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.CertificateCode == code);

        if (certificate == null) return NotFound();
        return Ok(new CertificateAdminResponse(certificate));
    }

    // ── Admin: revoke a wrongly issued certificate (hard delete: the entity has no soft-delete flags) ──
    [Authorize(Roles = AppRoles.Admin)]
    [HttpDelete("api/admin/certificates/{id:long}")]
    public async Task<IActionResult> Revoke(long id)
    {
        var entity = await _baseRepository.Query()
            .FirstOrDefaultAsync(c => c.Id == id);

        if (entity == null) return NotFound();

        _baseRepository.Remove(entity);
        await _baseRepository.SaveChangesAsync();

        return Ok(new { success = true });
    }
}
