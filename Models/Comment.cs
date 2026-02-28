using System.ComponentModel.DataAnnotations;

namespace RecipeSugesstionApp.Models
{
    public class Comment
    {
        public int CommentId { get; set; }

        public int RecipeId { get; set; }

        public int UserId { get; set; }

        [Required, MaxLength(2000)]
        public string Body { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public Recipe? Recipe { get; set; }
        public User? User { get; set; }
    }
}
