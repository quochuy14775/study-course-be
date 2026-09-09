using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudyCourseAPI.DTOs.Requests.Admin;
using StudyCourseAPI.Models;
using StudyCourseAPI.Services;

namespace StudyCourseAPI.Controllers
{
    [Route("api/[controller]")]
    [Authorize]
    public class LanguagesController : ControllerBase
    {
        private readonly ILanguageService _languageService;

        public LanguagesController(ILanguageService languageService)
        {
            _languageService = languageService;
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            return Ok(await _languageService.GetListAsync());
        }

        [AllowAnonymous]
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(long id)
        {
            var language = await _languageService.GetByIdAsync(id);

            if (language == null) return NotFound();
            return Ok(language);
        }

        [Authorize(Roles = AppRoles.Admin)]
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] LanguageRequest model)
        {
            var result = await _languageService.CreateAsync(model);

            if (!result.IsSuccess)
                return BadRequest(new { status = 400, message = result.ErrorMessage });

            return CreatedAtAction(nameof(Get), new { id = result.Data!.Id }, result.Data);
        }

        [Authorize(Roles = AppRoles.Admin)]
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(long id, [FromBody] LanguageRequest model)
        {
            var result = await _languageService.UpdateAsync(id, model);

            if (result.IsNotFound) return NotFound();
            if (!result.IsSuccess)
                return BadRequest(new { status = 400, message = result.ErrorMessage });

            return Ok(result.Data);
        }

        [Authorize(Roles = AppRoles.Admin)]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            var deleted = await _languageService.SoftDeleteAsync(id);

            if (!deleted) return NotFound();
            return Ok(new { success = true });
        }
    }
}
