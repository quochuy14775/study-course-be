using Microsoft.AspNetCore.Identity;

namespace StudyCourseAPI.Models
{
    public class Role : IdentityRole<long>
    {
        public Role() : base() { }

        public Role(string roleName) : base(roleName) { }

        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    }
}