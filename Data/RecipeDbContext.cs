using Microsoft.EntityFrameworkCore;
using RecipeSugesstionApp.Models;

namespace RecipeSugesstionApp.Data
{
    public class RecipeDbContext : DbContext
    {
        public RecipeDbContext(DbContextOptions<RecipeDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Recipe> Recipes { get; set; }
        public DbSet<Ingredient> Ingredients { get; set; }
        public DbSet<Step> Steps { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<RecipeCategory> RecipeCategories { get; set; }
        public DbSet<Rating> Ratings { get; set; }
        public DbSet<Comment> Comments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ── Steps ────────────────────────────────────────────────────────
            modelBuilder.Entity<Step>()
                .HasOne(s => s.Recipe)
                .WithMany(r => r.Steps)
                .HasForeignKey(s => s.RecipeId)
                .OnDelete(DeleteBehavior.Cascade);

            // ── RecipeCategory composite PK ──────────────────────────────────
            modelBuilder.Entity<RecipeCategory>()
                .HasKey(rc => new { rc.RecipeId, rc.CategoryId });

            modelBuilder.Entity<RecipeCategory>()
                .HasOne(rc => rc.Recipe)
                .WithMany(r => r.RecipeCategories)
                .HasForeignKey(rc => rc.RecipeId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RecipeCategory>()
                .HasOne(rc => rc.Category)
                .WithMany(c => c.RecipeCategories)
                .HasForeignKey(rc => rc.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);

            // ── Rating: prevent multiple cascades on SQL Server ──────────────
            modelBuilder.Entity<Rating>()
                .HasOne(r => r.Recipe)
                .WithMany(r => r.Ratings)
                .HasForeignKey(r => r.RecipeId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Rating>()
                .HasOne(r => r.User)
                .WithMany(u => u.Ratings)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            // ── Comment ──────────────────────────────────────────────────────
            modelBuilder.Entity<Comment>()
                .HasOne(c => c.Recipe)
                .WithMany(r => r.Comments)
                .HasForeignKey(c => c.RecipeId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Comment>()
                .HasOne(c => c.User)
                .WithMany(u => u.Comments)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            // ── Recipe owner ─────────────────────────────────────────────────
            modelBuilder.Entity<Recipe>()
                .HasOne(r => r.User)
                .WithMany(u => u.Recipes)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // ── Unique index on Username & Email ─────────────────────────────
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username).IsUnique();
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email).IsUnique();

            // ── Unique Rating per User-Recipe pair ───────────────────────────
            modelBuilder.Entity<Rating>()
                .HasIndex(r => new { r.RecipeId, r.UserId }).IsUnique();

            // ── Seed data ────────────────────────────────────────────────────
            SeedData(modelBuilder);
        }

        private static void SeedData(ModelBuilder modelBuilder)
        {
            // Seed categories only — all values are deterministic static strings.
            // ✅ Do NOT seed users here: BCrypt.HashPassword() is non-deterministic
            //    (new random salt each call) and EF Core rejects non-deterministic HasData values.
            //    Create users via POST /api/auth/register instead.
            modelBuilder.Entity<Category>().HasData(
                new Category { CategoryId = 1,  Name = "Vegetarian" },
                new Category { CategoryId = 2,  Name = "Vegan" },
                new Category { CategoryId = 3,  Name = "Dessert" },
                new Category { CategoryId = 4,  Name = "Breakfast" },
                new Category { CategoryId = 5,  Name = "Lunch" },
                new Category { CategoryId = 6,  Name = "Dinner" },
                new Category { CategoryId = 7,  Name = "Snack" },
                new Category { CategoryId = 8,  Name = "Gluten-Free" },
                new Category { CategoryId = 9,  Name = "Seafood" },
                new Category { CategoryId = 10, Name = "Quick & Easy" }
            );
        }
    }
}
