


using Microsoft.AspNetCore.Identity;
using StudyCourseAPI.Models;

namespace StudyCourseAPI.Repositories
{
    public class CurrentUser : ICurrentUser
    {
        private ApplicationUser _currentUser;
        private UserManager<ApplicationUser> _userManager;
        private HttpContext _httpContext;

        public CurrentUser(UserManager<ApplicationUser> userManager,
            IHttpContextAccessor contextAccessor)
        {
            _userManager = userManager;
            _httpContext = contextAccessor.HttpContext;
        }

        public string Name => _httpContext?.User.Identity?.Name;

        public ApplicationUser GetCurrentUser()
        {
            if (_currentUser != null)
            {
                return _currentUser;
            }

            var contextUser = _httpContext.User;
            _currentUser = _userManager.GetUserAsync(contextUser).Result;

            if (_currentUser != null)
            {
                return _currentUser;
            }

            return null;
        }

        public long GetCurrentUserId()
        {
            if (_currentUser != null)
            {
                return _currentUser.Id;
            }

            var contextUser = _httpContext.User;
            string id = _userManager.GetUserId(contextUser);

            if (id != null)
            {
                return long.Parse(id);
            }

            return 0;
        }
    }
}