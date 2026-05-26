

using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore;

namespace StudyCourseAPI.Repositories
{
    public interface IRepositoryWithTypedId<T, TId> where T : class
    {
        IQueryable<T> Query();

        Task<T> FindAsync(TId id);

        void Add(T entity);
        Task AddAsync(T entity);

        void AddRange(IEnumerable<T> entity);

        void BeginTransaction();

        void SaveChanges();

        Task SaveChangesAsync();

        void Remove(T entity);

        Task DeleteAsync(T entity);
    }
}