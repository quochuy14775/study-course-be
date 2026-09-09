using Microsoft.EntityFrameworkCore;
using StudyCourseAPI.DTOs.Responses;
using StudyCourseAPI.DTOs.Responses.Admin;
using StudyCourseAPI.Models;
using StudyCourseAPI.Repositories;

namespace StudyCourseAPI.Services;

public class CertificateService : ICertificateService
{
    private readonly IRepository<Certificate> _certificateRepository;
    private readonly ICurrentUser _currentUser;

    public CertificateService(
        IRepository<Certificate> certificateRepository,
        ICurrentUser currentUser)
    {
        _certificateRepository = certificateRepository;
        _currentUser = currentUser;
    }

    public async Task<CertificateResponse?> GetOwnByCourseAsync(long courseId)
    {
        var userId = _currentUser.GetCurrentUserId();

        var certificate = await _certificateRepository.Query()
            .AsNoTracking()
            .Include(c => c.Course)
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.CourseId == courseId && c.UserId == userId);

        return certificate == null ? null : new CertificateResponse(certificate);
    }

    public async Task<List<CertificateAdminResponse>> SearchAsync(long? courseId, string? search)
    {
        var query = _certificateRepository.Query()
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

        return items.Select(c => new CertificateAdminResponse(c)).ToList();
    }

    public async Task<CertificateAdminResponse?> VerifyAsync(string code)
    {
        var certificate = await _certificateRepository.Query()
            .AsNoTracking()
            .Include(c => c.Course)
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.CertificateCode == code);

        return certificate == null ? null : new CertificateAdminResponse(certificate);
    }

    public async Task<bool> RevokeAsync(long id)
    {
        var entity = await _certificateRepository.Query()
            .FirstOrDefaultAsync(c => c.Id == id);

        if (entity == null) return false;

        _certificateRepository.Remove(entity);
        await _certificateRepository.SaveChangesAsync();

        return true;
    }
}
