using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudyCourseAPI.DTOs.Requests.Admin;
using StudyCourseAPI.Models;
using StudyCourseAPI.Services;

namespace StudyCourseAPI.Controllers
{
    [Route("api/[controller]")]
    [Authorize]
    public class FrameworksController : ControllerBase
    {
        private readonly IFrameworkService _frameworkService;

        public FrameworksController(IFrameworkService frameworkService)
        {
            _frameworkService = frameworkService;
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            return Ok(await _frameworkService.GetListAsync());
        }

        [AllowAnonymous]
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(long id)
        {
            var framework = await _frameworkService.GetByIdAsync(id);

            if (framework == null) return NotFound();
            return Ok(framework);
        }

        [Authorize(Roles = AppRoles.Admin)]
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] FrameworkRequest model)
        {
            var result = await _frameworkService.CreateAsync(model);

            if (!result.IsSuccess)
                return BadRequest(new { status = 400, message = result.ErrorMessage });

            return CreatedAtAction(nameof(Get), new { id = result.Data!.Id }, result.Data);
        }

        [Authorize(Roles = AppRoles.Admin)]
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(long id, [FromBody] FrameworkRequest model)
        {
            var result = await _frameworkService.UpdateAsync(id, model);

            if (result.IsNotFound) return NotFound();
            if (!result.IsSuccess)
                return BadRequest(new { status = 400, message = result.ErrorMessage });

            return Ok(result.Data);
        }

        [Authorize(Roles = AppRoles.Admin)]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            var deleted = await _frameworkService.SoftDeleteAsync(id);

            if (!deleted) return NotFound();
            return Ok(new { success = true });
        }
    }
}
