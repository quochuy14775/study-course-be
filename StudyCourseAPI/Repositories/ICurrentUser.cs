


 using StudyCourseAPI.Models;

 namespace StudyCourseAPI.Repositories
{
    public interface ICurrentUser
    {
        ApplicationUser GetCurrentUser();
        long GetCurrentUserId();
    }
}