using System.ComponentModel.DataAnnotations;

namespace RecipeSugesstionApp.Models
{
    public class Ingredient
    {
        public int IngredientId { get; set; }

        public int RecipeId { get; set; }

        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(100)]
        public string Quantity { get; set; } = string.Empty;

        // Navigation
        public Recipe? Recipe { get; set; }
    }
}
