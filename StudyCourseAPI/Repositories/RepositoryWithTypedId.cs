using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using StudyCourseAPI.Data;

namespace StudyCourseAPI.Repositories
{
    public class RepositoryWithTypedId<T, TId> : IRepositoryWithTypedId<T, TId> where T : class
    {
        private readonly ICurrentUser _currentUser;

        public RepositoryWithTypedId(ApplicationDbContext context, ICurrentUser currentUser)
        {
            Context = context;
            DbSet = Context.Set<T>();
            _currentUser = currentUser;
        }

        protected DbContext Context { get; }
        protected IDbContextTransaction ContextTransaction { get; set; }

        protected DbSet<T> DbSet { get; }

        public async Task<T> FindAsync(TId id)
        {
            return await DbSet.FindAsync(id);
        }

        public void Add(T entity)
        {
            DbSet.Add(entity);
        }

        public async Task AddAsync(T entity)
        {
            await DbSet.AddAsync(entity);
        }

        public void AddRange(IEnumerable<T> entity)
        {
            DbSet.AddRange(entity);
        }

        public void BeginTransaction()
        {
            ContextTransaction = Context.Database.BeginTransaction();
        }

        public void SaveChanges()
        {
            Context.SaveChanges();
        }

        public Task SaveChangesAsync()
        {
            return Context.SaveChangesAsync();
        }

        public IQueryable<T> Query()
        {
            return DbSet;
        }

        public void Remove(T entity)
        {
            DbSet.Remove(entity);
        }

        public Task DeleteAsync(T entity)
        {
            DbSet.Remove(entity);

            return Task.CompletedTask;
        }
    }
}