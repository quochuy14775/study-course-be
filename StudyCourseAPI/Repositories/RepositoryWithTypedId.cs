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
            AuditableEntities();
            Context.SaveChanges();
        }

        public Task SaveChangesAsync()
        {
            AuditableEntities();
            return Context.SaveChangesAsync();
        }

        private void AuditableEntities()
        {
            //var currentUser = _currentUser.GetCurrentUser();
            //var modifiedEntries = Context.ChangeTracker.Entries<IAuditable>()
            //        .Where(x => (x.State == EntityState.Added || x.State == EntityState.Modified));

            //foreach (var entity in modifiedEntries)
            //{
            //    switch (entity.State)
            //    {
            //        case EntityState.Added:
            //            entity.Entity.CreatedBy = currentUser;
            //            entity.Entity.CreatedOn = DateTime.UtcNow;
            //            entity.Entity.LatestUpdatedOn = DateTime.UtcNow;
            //            entity.Entity.LatestUpdatedBy = currentUser;
            //            break;

            //        case EntityState.Modified:
            //            entity.Entity.LatestUpdatedOn = DateTime.UtcNow;
            //            entity.Entity.LatestUpdatedBy = currentUser;
            //            break;
            //    }
            //}

            //foreach (var entry in Context.ChangeTracker.Entries<IBranchable>()
            //    .Where(x => (x.State == EntityState.Added)))
            //{
            //    switch (entry.State)
            //    {
            //        case EntityState.Added:
            //            entry.Entity.BranchId = currentUser.BranchId.Value;
            //            break;
            //    }
            //}

            //foreach (var entry in Context.ChangeTracker.Entries<IDeletable>()
            //    .Where(x => (x.State == EntityState.Deleted)))
            //{
            //    switch (entry.State)
            //    {
            //        case EntityState.Deleted:
            //            entry.Entity.IsDeleted = true;
            //            entry.Entity.DeletedBy = currentUser;
            //            entry.Entity.DeletedOn = DateTime.UtcNow;
            //            entry.State = EntityState.Modified;
            //            break;
            //    }
            //}
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