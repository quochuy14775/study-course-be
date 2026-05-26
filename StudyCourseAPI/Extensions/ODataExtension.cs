using Microsoft.AspNetCore.OData.Query;

namespace StudyCourseAPI.Extensions
{
        public static class ODataExtension
        {
            public static (int count, IEnumerable<T> data) AppendQueryOptions<T>(
                this IQueryable<T> queryable, 
                ODataQueryOptions<T> queryOptions) where T : class
            {
                var count = queryable.Count();
                var data = queryOptions.ApplyTo(queryable).Cast<T>();
                return (count, data);
            }
        }
}