using System.ComponentModel.DataAnnotations;

namespace RecipeSugesstionApp.Models
{
    public class Recipe
    {
        public int RecipeId { get; set; }

        public int UserId { get; set; }

        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string Description { get; set; } = string.Empty;

        // Relative path to uploaded image, e.g. /uploads/recipes/abc.jpg
        [MaxLength(500)]
        public string? ImageUrl { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public User? User { get; set; }
        public ICollection<Ingredient> Ingredients { get; set; } = new List<Ingredient>();
        public ICollection<Step> Steps { get; set; } = new List<Step>();
        public ICollection<RecipeCategory> RecipeCategories { get; set; } = new List<RecipeCategory>();
        public ICollection<Rating> Ratings { get; set; } = new List<Rating>();
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    }
}
