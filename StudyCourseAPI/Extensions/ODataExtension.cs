using Microsoft.AspNetCore.OData.Query;
using Microsoft.EntityFrameworkCore;

namespace StudyCourseAPI.Extensions
{
    public static class ODataExtension
    {
        /// <summary>
        /// Apply OData query options to a queryable. Uses AsNoTracking for read-only path.
        /// Synchronous overload — kept for backward compatibility.
        /// </summary>
        public static (int count, IEnumerable<T> data) AppendQueryOptions<T>(
            this IQueryable<T> queryable,
            ODataQueryOptions<T> queryOptions) where T : class
        {
            // No change tracking for read paths → less memory, faster materialization
            queryable = queryable.AsNoTracking();
            var count = queryable.Count();
            var data = queryOptions.ApplyTo(queryable).Cast<T>();
            return (count, data);
        }

        /// <summary>
        /// Async variant: parallel-friendly, single SQL round-trip per call.
        /// Returns a materialized List so the connection can be released early.
        /// </summary>
        public static async Task<(int count, List<T> data)> AppendQueryOptionsAsync<T>(
            this IQueryable<T> queryable,
            ODataQueryOptions<T> queryOptions) where T : class
        {
            queryable = queryable.AsNoTracking();
            var count = await queryable.CountAsync();
            var applied = (IQueryable<T>)queryOptions.ApplyTo(queryable);
            var data = await applied.ToListAsync();
            return (count, data);
        }
    }
}
