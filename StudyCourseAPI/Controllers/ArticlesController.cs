using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudyCourseAPI.DTOs.Requests.Admin;
using StudyCourseAPI.Models;
using StudyCourseAPI.Services;

namespace StudyCourseAPI.Controllers;

[Route("api/[controller]")]
[Authorize]
public class ArticlesController : ControllerBase
{
    private readonly IArticleService _articleService;

    public ArticlesController(IArticleService articleService)
    {
        _articleService = articleService;
    }

    // ── GET list (public) ─────────────────────────────────────────────────────
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string? category, [FromQuery] string? search)
    {
        return Ok(await _articleService.SearchAsync(category, search));
    }

    // ── GET single (public) ───────────────────────────────────────────────────
    [AllowAnonymous]
    [HttpGet("{id}")]
    public async Task<IActionResult> Get(long id)
    {
        var article = await _articleService.GetByIdAsync(id);

        if (article == null) return NotFound();
        return Ok(article);
    }

    // ── GET by slug (public) ──────────────────────────────────────────────────
    [AllowAnonymous]
    [HttpGet("slug/{slug}")]
    public async Task<IActionResult> GetBySlug(string slug)
    {
        var article = await _articleService.GetBySlugAsync(slug);

        if (article == null) return NotFound();
        return Ok(article);
    }

    // ── GET categories (public) ───────────────────────────────────────────────
    [AllowAnonymous]
    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories()
    {
        return Ok(await _articleService.GetCategoriesAsync());
    }

    // ── GET my articles (user's own) ──────────────────────────────────────────
    [HttpGet("mine")]
    public async Task<IActionResult> GetMine()
    {
        var articles = await _articleService.GetMineAsync();

        if (articles == null) return Unauthorized();
        return Ok(articles);
    }

    // ── POST — any authenticated user ─────────────────────────────────────────
    [HttpPost]
    public async Task<IActionResult> Post([FromBody] ArticleRequest model)
    {
        var result = await _articleService.CreateAsync(model);

        if (!result.IsSuccess)
            return BadRequest(new { status = 400, message = result.ErrorMessage });

        return CreatedAtAction(nameof(Get), new { id = result.Data!.Id }, result.Data);
    }

    // ── PUT — owner hoặc Admin ────────────────────────────────────────────────
    [HttpPut("{id}")]
    public async Task<IActionResult> Put(long id, [FromBody] ArticleRequest model)
    {
        // Quyền admin đọc từ claim của request — service không đụng tới HttpContext
        var result = await _articleService.UpdateAsync(id, model, User.IsInRole(AppRoles.Admin));

        if (result.IsNotFound) return NotFound();
        if (result.IsForbidden) return Forbid();
        if (!result.IsSuccess)
            return BadRequest(new { status = 400, message = result.ErrorMessage });

        return Ok(result.Data);
    }

    // ── DELETE — owner hoặc Admin (soft) ──────────────────────────────────────
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(long id)
    {
        var result = await _articleService.SoftDeleteAsync(id, User.IsInRole(AppRoles.Admin));

        if (result.IsNotFound) return NotFound();
        if (result.IsForbidden) return Forbid();

        return Ok(new { success = true });
    }

    // ── POST increment view (public) ──────────────────────────────────────────
    [AllowAnonymous]
    [HttpPost("{id}/view")]
    public async Task<IActionResult> IncrementView(long id)
    {
        var viewCount = await _articleService.IncrementViewAsync(id);

        if (viewCount == null) return NotFound();
        return Ok(new { viewCount });
    }
}
