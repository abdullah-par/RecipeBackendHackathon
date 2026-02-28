using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RecipeSugesstionApp.DTOs;
using RecipeSugesstionApp.Services;

namespace RecipeSugesstionApp.Controllers
{
    [ApiController]
    [Route("api/recipes")]
    public class RecipesController : ControllerBase
    {
        private readonly IRecipeService _recipes;
        private readonly IWebHostEnvironment _env;

        public RecipesController(IRecipeService recipes, IWebHostEnvironment env)
        {
            _recipes = recipes;
            _env = env;
        }

        // ── GET /api/recipes ─────────────────────────────────────────────────
        /// <summary>Get basic recipe feed with pagination. (Public)</summary>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResultDto<RecipeSummaryDto>), 200)]
        public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var result = await _recipes.GetAllAsync(null, null, page, pageSize);
            return Ok(result);
        }

        // ── SEARCH /api/recipes/search?q=pasta ────────────────────────────────
        /// <summary>Search for recipes by title, description, ingredients, or category.</summary>
        [HttpGet("search")]
        [ProducesResponseType(typeof(PagedResultDto<RecipeSummaryDto>), 200)]
        public async Task<IActionResult> Search([FromQuery] string? q, [FromQuery] int? categoryId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var result = await _recipes.GetAllAsync(q, categoryId, page, pageSize);
            return Ok(result);
        }

        // ── GET /api/recipes/{id} ─────────────────────────────────────────────
        /// <summary>Get a single recipe with full detail. (Public)</summary>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(RecipeDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetById(int id)
        {
            var recipe = await _recipes.GetByIdAsync(id);
            if (recipe == null) return NotFound();
            return Ok(recipe);
        }

        // ── POST /api/recipes ─────────────────────────────────────────────────
        /// <summary>Create a new recipe. Requires authentication.</summary>
        [HttpPost]
        [Authorize]
        [ProducesResponseType(typeof(RecipeDto), 201)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> Create([FromBody] CreateRecipeDto dto)
        {
            var userId = GetUserId();
            if (userId == 0) return Unauthorized();

            var created = await _recipes.CreateAsync(userId, dto);
            return CreatedAtAction(nameof(GetById), new { id = created.RecipeId }, created);
        }

        // ── PUT /api/recipes/{id} ─────────────────────────────────────────────
        /// <summary>Update a recipe. Only the recipe owner can update.</summary>
        [HttpPut("{id:int}")]
        [Authorize]
        [ProducesResponseType(typeof(RecipeDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateRecipeDto dto)
        {
            var userId = GetUserId();
            if (userId == 0) return Unauthorized();

            var updated = await _recipes.UpdateAsync(id, userId, dto);
            if (updated == null) return NotFound(new { message = "Recipe not found or you are not the owner." });
            return Ok(updated);
        }

        // ── DELETE /api/recipes/{id} ──────────────────────────────────────────
        /// <summary>Delete a recipe. Only the recipe owner can delete.</summary>
        [HttpDelete("{id:int}")]
        [Authorize]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = GetUserId();
            if (userId == 0) return Unauthorized();

            var deleted = await _recipes.DeleteAsync(id, userId);
            if (!deleted) return NotFound(new { message = "Recipe not found or you are not the owner." });
            return NoContent();
        }

        // ── POST /api/recipes/{id}/image ──────────────────────────────────────
        /// <summary>Upload / replace the photo for a recipe.</summary>
        [HttpPost("{id:int}/image")]
        [Authorize]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> UploadImage(int id, IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "No file uploaded." });

            if (file.Length > 5 * 1024 * 1024) // 5 MB limit
                return BadRequest(new { message = "File size exceeds 5 MB." });

            var userId = GetUserId();
            if (userId == 0) return Unauthorized();

            var imageUrl = await _recipes.UploadImageAsync(id, userId, file, _env);
            if (imageUrl == null)
                return NotFound(new { message = "Recipe not found, you are not the owner, or file type is unsupported." });

            return Ok(new { imageUrl });
        }

        // ── HELPER ────────────────────────────────────────────────────────────
        private int GetUserId()
        {
            var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? User.FindFirstValue("sub");
            return int.TryParse(sub, out var id) ? id : 0;
        }
    }
}
