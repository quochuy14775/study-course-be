


using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using StudyCourseAPI.Data;

namespace StudyCourseAPI.Repositories
{
    public class Repository<T> : RepositoryWithTypedId<T, long>, IRepository<T>
        where T : class
    {
        public Repository(ApplicationDbContext context, ICurrentUser currentUser) : base(context, currentUser)
        {
        }
    }
} 