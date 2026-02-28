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
    [Route("api/comments")]
    public class CommentsController : ControllerBase
    {
        private readonly RecipeDbContext _db;

        public CommentsController(RecipeDbContext db) => _db = db;

        /// <summary>Get all comments for a recipe. (Public)</summary>
        [HttpGet("{recipeId:int}")]
        [ProducesResponseType(typeof(IEnumerable<CommentDto>), 200)]
        public async Task<IActionResult> GetForRecipe(int recipeId)
        {
            var comments = await _db.Comments
                .Include(c => c.User)
                .Where(c => c.RecipeId == recipeId)
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new CommentDto
                {
                    CommentId = c.CommentId,
                    RecipeId = c.RecipeId,
                    UserId = c.UserId,
                    Username = c.User!.Username,
                    Body = c.Body,
                    CreatedAt = c.CreatedAt
                }).ToListAsync();

            return Ok(comments);
        }

        /// <summary>Post a comment on a recipe. (Auth required)</summary>
        [HttpPost]
        [Authorize]
        [ProducesResponseType(typeof(CommentDto), 201)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Create([FromBody] CreateCommentDto dto)
        {
            var userId = GetUserId();
            if (userId == 0) return Unauthorized();

            if (!await _db.Recipes.AnyAsync(r => r.RecipeId == dto.RecipeId))
                return NotFound(new { message = "Recipe not found." });

            var comment = new Comment
            {
                RecipeId = dto.RecipeId,
                UserId = userId,
                Body = dto.Body,
                CreatedAt = DateTime.UtcNow
            };

            _db.Comments.Add(comment);
            await _db.SaveChangesAsync();

            var user = await _db.Users.FindAsync(userId);
            return CreatedAtAction(nameof(GetForRecipe), new { recipeId = comment.RecipeId }, new CommentDto
            {
                CommentId = comment.CommentId,
                RecipeId = comment.RecipeId,
                UserId = comment.UserId,
                Username = user?.Username ?? "Unknown",
                Body = comment.Body,
                CreatedAt = comment.CreatedAt
            });
        }

        /// <summary>Delete a comment. Only author can delete. (Auth required)</summary>
        [HttpDelete("{commentId:int}")]
        [Authorize]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Delete(int commentId)
        {
            var userId = GetUserId();
            if (userId == 0) return Unauthorized();

            var comment = await _db.Comments
                .FirstOrDefaultAsync(c => c.CommentId == commentId && c.UserId == userId);

            if (comment == null)
                return NotFound(new { message = "Comment not found or you are not the author." });

            _db.Comments.Remove(comment);
            await _db.SaveChangesAsync();
            return NoContent();
        }

        private int GetUserId()
        {
            var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? User.FindFirstValue("sub");
            return int.TryParse(sub, out var id) ? id : 0;
        }
    }
}
