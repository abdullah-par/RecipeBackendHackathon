namespace RecipeSugesstionApp.Models
{
    /// <summary>Join table for the many-to-many Recipe ↔ Category relationship.</summary>
    public class RecipeCategory
    {
        public int RecipeId { get; set; }
        public int CategoryId { get; set; }

        // Navigation
        public Recipe? Recipe { get; set; }
        public Category? Category { get; set; }
    }
}
