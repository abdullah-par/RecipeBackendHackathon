using System.ComponentModel.DataAnnotations;

namespace RecipeSugesstionApp.Models
{
    public class Step
    {
        public int StepId { get; set; }

        public int RecipeId { get; set; }

        [Required]
        public int Order { get; set; } // Sequence of the step

        [Required, MaxLength(2000)]
        public string Instruction { get; set; } = string.Empty;

        // Navigation
        public Recipe? Recipe { get; set; }
    }
}
