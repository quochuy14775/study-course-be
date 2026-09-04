using StudyCourseAPI.DTOs.Requests.User;
using StudyCourseAPI.Models;

namespace StudyCourseAPI.Extensions
{
    public static class UserExtensions
    {
        public static void MapTo(this UpdateProfileRequest model, ApplicationUser entity)
        {
            entity.FullName  = model.FullName.Trim();
            entity.AvatarUrl = string.IsNullOrWhiteSpace(model.AvatarUrl) ? null : model.AvatarUrl.Trim();
        }
    }
}
