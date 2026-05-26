using Microsoft.AspNetCore.Identity;

namespace StudyCourseAPI.Models
{
    public class UserRole : IdentityUserRole<long>
    {
        public ApplicationUser User { get; set; }
        public Role Role { get; set; }
    }
}