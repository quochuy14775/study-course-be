using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.EntityFrameworkCore;
using StudyCourseAPI.DTOs.Requests.Admin;
using StudyCourseAPI.DTOs.Responses;
using StudyCourseAPI.DTOs.Responses.Admin;
using StudyCourseAPI.Extensions;
using StudyCourseAPI.Models;
using StudyCourseAPI.Repositories;

namespace StudyCourseAPI.Controllers.AdminController
{
    [Route("api/[controller]")]
    [Authorize]
    public class SkillsController : BaseController<Skill>
    {
        public SkillsController(IRepository<Skill> baseRepository, ICurrentUser currentUser)
            : base(baseRepository, currentUser)
        {
        }

        [HttpGet]
        public IActionResult Get(ODataQueryOptions<Skill> queryOptions)
        {
            var queryable = _baseRepository.Query()
                .Where(x => !x.IsDeleted && x.IsActive);

            var (count, vm) = queryable.AppendQueryOptions(queryOptions);

            var response = new ODataResponse<SkillResponse>
            {
                Count = count,
                Value = vm.Select(x => new SkillResponse(x))
            };

            return Ok(response);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(long id)
        {
            var entity = await _baseRepository.Query()
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

            if (entity == null) return NotFound();

            return Ok(new SkillResponse(entity));
        }

        [Authorize(Roles = AppRoles.Admin)]
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] SkillRequest model)
        {
            var (success, errors) = await model.ValidateSkillAsync(_baseRepository);
            if (!success)
                return BadRequest(new { status = 400, message = "Validation failed", errors = FlattenErrors(errors) });

            var entity = model.GetSkill();
            _baseRepository.Add(entity);
            await _baseRepository.SaveChangesAsync();

            return CreatedAtAction(
                nameof(Get),
                new { id = entity.Id },
                new
                {
                    success = true,
                    message = "Skill created successfully.",
                    data = new SkillResponse(entity)
                });
        }

        [Authorize(Roles = AppRoles.Admin)]
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(long id, [FromBody] SkillRequest model)
        {
            var entity = await _baseRepository.Query()
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

            if (entity == null) return NotFound();

            var (success, errors) = await model.ValidateSkillAsync(_baseRepository, id);
            if (!success)
                return BadRequest(new { status = 400, message = "Validation failed", errors = FlattenErrors(errors) });

            model.MapTo(entity);
            await _baseRepository.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Skill updated successfully.",
                data = new SkillResponse(entity)
            });
        }

        [Authorize(Roles = AppRoles.Admin)]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            var entity = await _baseRepository.Query()
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

            if (entity == null) return NotFound();

            entity.IsDeleted = true;
            entity.UpdatedAt = DateTime.UtcNow;

            await _baseRepository.SaveChangesAsync();
            return Ok(new { success = true });
        }

        private static Dictionary<string, object> FlattenErrors(Dictionary<string, List<string>>? errors)
        {
            var result = new Dictionary<string, object>();
            if (errors == null) return result;
            foreach (var kv in errors)
            {
                if (kv.Value == null || kv.Value.Count == 0) continue;
                result[kv.Key] = kv.Value.Count == 1 ? kv.Value[0] : (object)kv.Value;
            }
            return result;
        }
    }
}

