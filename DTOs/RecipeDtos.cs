using System.ComponentModel.DataAnnotations;

namespace RecipeSugesstionApp.DTOs
{
    // ── Auth ─────────────────────────────────────────────────────────────────

    public class RegisterDto
    {
        [Required, MaxLength(100)]
        public string Username { get; set; } = string.Empty;

        [Required, EmailAddress, MaxLength(200)]
        public string Email { get; set; } = string.Empty;

        [Required, MinLength(6)]
        public string Password { get; set; } = string.Empty;
    }

    public class LoginDto
    {
        [Required]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }

    public class AuthResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public int UserId { get; set; }
        public DateTime ExpiresAt { get; set; }
    }

    // ── User ─────────────────────────────────────────────────────────────────

    public class UserDto
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    // ── Recipe ───────────────────────────────────────────────────────────────

    public class CreateRecipeDto
    {
        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        public List<string> Steps { get; set; } = new();

        public List<IngredientDto> Ingredients { get; set; } = new();

        public List<int> CategoryIds { get; set; } = new();
    }

    public class UpdateRecipeDto
    {
        [MaxLength(200)]
        public string? Title { get; set; }

        [MaxLength(2000)]
        public string? Description { get; set; }

        public List<string>? Steps { get; set; }
        public List<IngredientDto>? Ingredients { get; set; }
        public List<int>? CategoryIds { get; set; }
    }

    public class RecipeDto
    {
        public int RecipeId { get; set; }
        public int UserId { get; set; }
        public string AuthorUsername { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> Steps { get; set; } = new();
        public string? ImageUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<IngredientDto> Ingredients { get; set; } = new();
        public List<string> Categories { get; set; } = new();
        public double AverageRating { get; set; }
        public int RatingCount { get; set; }
    }

    public class RecipeSummaryDto
    {
        public int RecipeId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public string AuthorUsername { get; set; } = string.Empty;
        public List<string> Categories { get; set; } = new();
        public double AverageRating { get; set; }
        public int RatingCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // ── Ingredient ───────────────────────────────────────────────────────────

    public class IngredientDto
    {
        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(100)]
        public string Quantity { get; set; } = string.Empty;
    }

    // ── Category ─────────────────────────────────────────────────────────────

    public class CategoryDto
    {
        public int CategoryId { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    // ── Rating ───────────────────────────────────────────────────────────────

    public class CreateRatingDto
    {
        [Required]
        public int RecipeId { get; set; }

        [Required, Range(1, 5)]
        public int Score { get; set; }
    }

    public class RatingDto
    {
        public int RatingId { get; set; }
        public int RecipeId { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public int Score { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // ── Comment ──────────────────────────────────────────────────────────────

    public class CreateCommentDto
    {
        [Required]
        public int RecipeId { get; set; }

        [Required, MaxLength(2000)]
        public string Body { get; set; } = string.Empty;
    }

    public class CommentDto
    {
        public int CommentId { get; set; }
        public int RecipeId { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
