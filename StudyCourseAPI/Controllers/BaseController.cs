
using Microsoft.AspNetCore.OData.Routing.Controllers;
using StudyCourseAPI.Repositories;

namespace StudyCourseAPI.Controllers
{
    public class BaseController<TEntity> : ODataController where TEntity : class
    {
        protected readonly IRepository<TEntity> _baseRepository;
        protected readonly ICurrentUser _currentUser;

        public BaseController(IRepository<TEntity> baseRepository,
            ICurrentUser currentUser)
        {
            _baseRepository = baseRepository;
            _currentUser = currentUser;
        }
    }
}