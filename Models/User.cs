using System.ComponentModel.DataAnnotations;

namespace RecipeSugesstionApp.Models
{
    public class User
    {
        public int UserId { get; set; }

        [Required, MaxLength(100)]
        public string Username { get; set; } = string.Empty;

        [Required, MaxLength(200), EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public ICollection<Recipe> Recipes { get; set; } = new List<Recipe>();
        public ICollection<Rating> Ratings { get; set; } = new List<Rating>();
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    }
}
