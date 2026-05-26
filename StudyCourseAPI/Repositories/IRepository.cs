
namespace StudyCourseAPI.Repositories
{
    public interface IRepository<T> : IRepositoryWithTypedId<T, long> where T : class
    {
    }
}