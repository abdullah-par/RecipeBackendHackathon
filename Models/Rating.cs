using System.ComponentModel.DataAnnotations;

namespace RecipeSugesstionApp.Models
{
    public class Rating
    {
        public int RatingId { get; set; }

        public int RecipeId { get; set; }

        public int UserId { get; set; }

        [Range(1, 5)]
        public int Score { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public Recipe? Recipe { get; set; }
        public User? User { get; set; }
    }
}
