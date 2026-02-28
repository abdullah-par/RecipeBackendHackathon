using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecipeSugesstionApp.Data;
using RecipeSugesstionApp.DTOs;
using RecipeSugesstionApp.Models;

namespace RecipeSugesstionApp.Controllers
{
    [ApiController]
    [Route("api/ratings")]
    public class RatingsController : ControllerBase
    {
        private readonly RecipeDbContext _db;

        public RatingsController(RecipeDbContext db) => _db = db;

        /// <summary>Get all ratings for a recipe. (Public)</summary>
        [HttpGet("{recipeId:int}")]
        [ProducesResponseType(typeof(IEnumerable<RatingDto>), 200)]
        public async Task<IActionResult> GetForRecipe(int recipeId)
        {
            var ratings = await _db.Ratings
                .Include(r => r.User)
                .Where(r => r.RecipeId == recipeId)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new RatingDto
                {
                    RatingId = r.RatingId,
                    RecipeId = r.RecipeId,
                    UserId = r.UserId,
                    Username = r.User!.Username,
                    Score = r.Score,
                    CreatedAt = r.CreatedAt
                }).ToListAsync();

            return Ok(ratings);
        }

        /// <summary>Rate a recipe (1–5). If already rated, updates the existing score. (Auth required)</summary>
        [HttpPost]
        [Authorize]
        [ProducesResponseType(typeof(RatingDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Rate([FromBody] CreateRatingDto dto)
        {
            var userId = GetUserId();
            if (userId == 0) return Unauthorized();

            if (!await _db.Recipes.AnyAsync(r => r.RecipeId == dto.RecipeId))
                return NotFound(new { message = "Recipe not found." });

            var existing = await _db.Ratings
                .FirstOrDefaultAsync(r => r.RecipeId == dto.RecipeId && r.UserId == userId);

            if (existing != null)
            {
                existing.Score = dto.Score;
                existing.CreatedAt = DateTime.UtcNow;
            }
            else
            {
                existing = new Rating
                {
                    RecipeId = dto.RecipeId,
                    UserId = userId,
                    Score = dto.Score,
                    CreatedAt = DateTime.UtcNow
                };
                _db.Ratings.Add(existing);
            }

            await _db.SaveChangesAsync();

            var user = await _db.Users.FindAsync(userId);
            return Ok(new RatingDto
            {
                RatingId = existing.RatingId,
                RecipeId = existing.RecipeId,
                UserId = existing.UserId,
                Username = user?.Username ?? "Unknown",
                Score = existing.Score,
                CreatedAt = existing.CreatedAt
            });
        }

        private int GetUserId()
        {
            var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? User.FindFirstValue("sub");
            return int.TryParse(sub, out var id) ? id : 0;
        }
    }
}
