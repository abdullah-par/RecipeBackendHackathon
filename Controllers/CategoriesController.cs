using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecipeSugesstionApp.Data;
using RecipeSugesstionApp.DTOs;

namespace RecipeSugesstionApp.Controllers
{
    [ApiController]
    [Route("api/categories")]
    public class CategoriesController : ControllerBase
    {
        private readonly RecipeDbContext _db;

        public CategoriesController(RecipeDbContext db) => _db = db;

        /// <summary>Get all categories.</summary>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<CategoryDto>), 200)]
        public async Task<IActionResult> GetAll()
        {
            var cats = await _db.Categories
                .Select(c => new CategoryDto { CategoryId = c.CategoryId, Name = c.Name })
                .ToListAsync();
            return Ok(cats);
        }

        /// <summary>Create a new category (admin use).</summary>
        [HttpPost]
        [Authorize]
        [ProducesResponseType(typeof(CategoryDto), 201)]
        public async Task<IActionResult> Create([FromBody] CategoryDto dto)
        {
            var cat = new Models.Category { Name = dto.Name };
            _db.Categories.Add(cat);
            await _db.SaveChangesAsync();
            return CreatedAtAction(nameof(GetAll), new CategoryDto { CategoryId = cat.CategoryId, Name = cat.Name });
        }
    }
}
