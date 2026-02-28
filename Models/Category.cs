using System.ComponentModel.DataAnnotations;

namespace RecipeSugesstionApp.Models
{
    public class Category
    {
        public int CategoryId { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        // Navigation
        public ICollection<RecipeCategory> RecipeCategories { get; set; } = new List<RecipeCategory>();
    }
}
