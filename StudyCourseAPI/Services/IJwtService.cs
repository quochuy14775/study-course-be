using StudyCourseAPI.Models;

namespace StudyCourseAPI.Services;

public interface IJwtService
{
    string GenerateToken(ApplicationUser user, IList<string> roles);
}