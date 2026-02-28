using Microsoft.EntityFrameworkCore;
using RecipeSugesstionApp.Data;
using RecipeSugesstionApp.DTOs;
using RecipeSugesstionApp.Models;
using System.Text.Json;

namespace RecipeSugesstionApp.Services
{
    public interface IRecipeService
    {
        Task<RecipeDto?> GetByIdAsync(int id);
        Task<IEnumerable<RecipeSummaryDto>> GetAllAsync(string? search, int? categoryId);
        Task<IEnumerable<RecipeSummaryDto>> GetByUserIdAsync(int userId);
        Task<RecipeDto> CreateAsync(int userId, CreateRecipeDto dto);
        Task<RecipeDto?> UpdateAsync(int recipeId, int userId, UpdateRecipeDto dto);
        Task<bool> DeleteAsync(int recipeId, int userId);
        Task<string?> UploadImageAsync(int recipeId, int userId, IFormFile file, IWebHostEnvironment env);
    }

    public class RecipeService : IRecipeService
    {
        private readonly RecipeDbContext _db;

        public RecipeService(RecipeDbContext db) => _db = db;

        // ── GET ALL (with optional search & category filter) ─────────────────
        public async Task<PagedResultDto<RecipeSummaryDto>> GetAllAsync(string? search, int? categoryId, int page, int pageSize)
        {
            var query = _db.Recipes
                .Include(r => r.User)
                .Include(r => r.RecipeCategories).ThenInclude(rc => rc.Category)
                .Include(r => r.Ratings)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.ToLower();
                query = query.Where(r =>
                    r.Title.ToLower().Contains(term) ||
                    r.Description.ToLower().Contains(term) ||
                    r.Ingredients.Any(i => i.Name.ToLower().Contains(term)) ||
                    r.RecipeCategories.Any(rc => rc.Category!.Name.ToLower().Contains(term)));
            }

            if (categoryId.HasValue)
                query = query.Where(r => r.RecipeCategories.Any(rc => rc.CategoryId == categoryId.Value));

            var totalCount = await query.CountAsync();
            var recipes = await query.OrderByDescending(r => r.CreatedAt)
                                     .Skip((page - 1) * pageSize)
                                     .Take(pageSize)
                                     .ToListAsync();

            var items = recipes.Select(r => new RecipeSummaryDto
            {
                RecipeId = r.RecipeId,
                Title = r.Title,
                ImageUrl = r.ImageUrl,
                AuthorUsername = r.User?.Username ?? "Unknown",
                Categories = r.RecipeCategories.Select(rc => rc.Category?.Name ?? "").ToList(),
                AverageRating = r.Ratings.Any() ? r.Ratings.Average(rt => rt.Score) : 0,
                RatingCount = r.Ratings.Count,
                CreatedAt = r.CreatedAt
            });

            return new PagedResultDto<RecipeSummaryDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<IEnumerable<RecipeSummaryDto>> GetByUserIdAsync(int userId)
        {
            var recipes = await _db.Recipes
                .Include(r => r.User)
                .Include(r => r.RecipeCategories).ThenInclude(rc => rc.Category)
                .Include(r => r.Ratings)
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return recipes.Select(r => new RecipeSummaryDto
            {
                RecipeId = r.RecipeId,
                Title = r.Title,
                ImageUrl = r.ImageUrl,
                AuthorUsername = r.User?.Username ?? "Unknown",
                Categories = r.RecipeCategories.Select(rc => rc.Category?.Name ?? "").ToList(),
                AverageRating = r.Ratings.Any() ? r.Ratings.Average(rt => rt.Score) : 0,
                RatingCount = r.Ratings.Count,
                CreatedAt = r.CreatedAt
            });
        }

        // ── GET BY ID ─────────────────────────────────────────────────────────
        public async Task<RecipeDto?> GetByIdAsync(int id)
        {
            var r = await _db.Recipes
                .Include(r => r.User)
                .Include(r => r.Ingredients)
                .Include(r => r.RecipeCategories).ThenInclude(rc => rc.Category)
                .Include(r => r.Ratings)
                .FirstOrDefaultAsync(r => r.RecipeId == id);

            if (r == null) return null;
            return MapToDto(r);
        }

        // ── CREATE ────────────────────────────────────────────────────────────
        public async Task<RecipeDto> CreateAsync(int userId, CreateRecipeDto dto)
        {
            var recipe = new Recipe
            {
                UserId = userId,
                Title = dto.Title,
                Description = dto.Description,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Steps (normalized)
            recipe.Steps = dto.Steps.Select((s, index) => new Step
            {
                Instruction = s,
                Order = index + 1
            }).ToList();

            // Ingredients
            recipe.Ingredients = dto.Ingredients.Select(i => new Ingredient
            {
                Name = i.Name,
                Quantity = i.Quantity
            }).ToList();

            _db.Recipes.Add(recipe);
            await _db.SaveChangesAsync();

            // Categories
            foreach (var catId in dto.CategoryIds.Distinct())
            {
                if (await _db.Categories.AnyAsync(c => c.CategoryId == catId))
                    _db.RecipeCategories.Add(new RecipeCategory { RecipeId = recipe.RecipeId, CategoryId = catId });
            }
            await _db.SaveChangesAsync();

            return (await GetByIdAsync(recipe.RecipeId))!;
        }

        // ── UPDATE ────────────────────────────────────────────────────────────
        public async Task<RecipeDto?> UpdateAsync(int recipeId, int userId, UpdateRecipeDto dto)
        {
            var recipe = await _db.Recipes
                .Include(r => r.Ingredients)
                .Include(r => r.Steps)
                .Include(r => r.RecipeCategories)
                .FirstOrDefaultAsync(r => r.RecipeId == recipeId && r.UserId == userId);

            if (recipe == null) return null;

            if (dto.Title != null) recipe.Title = dto.Title;
            if (dto.Description != null) recipe.Description = dto.Description;
            recipe.UpdatedAt = DateTime.UtcNow;

            if (dto.Steps != null)
            {
                _db.Steps.RemoveRange(recipe.Steps);
                recipe.Steps = dto.Steps.Select((s, index) => new Step
                {
                    RecipeId = recipeId,
                    Instruction = s,
                    Order = index + 1
                }).ToList();
            }

            if (dto.Ingredients != null)
            {
                _db.Ingredients.RemoveRange(recipe.Ingredients);
                recipe.Ingredients = dto.Ingredients.Select(i => new Ingredient
                {
                    RecipeId = recipeId,
                    Name = i.Name,
                    Quantity = i.Quantity
                }).ToList();
            }

            if (dto.CategoryIds != null)
            {
                _db.RecipeCategories.RemoveRange(recipe.RecipeCategories);
                foreach (var catId in dto.CategoryIds.Distinct())
                {
                    if (await _db.Categories.AnyAsync(c => c.CategoryId == catId))
                        _db.RecipeCategories.Add(new RecipeCategory { RecipeId = recipeId, CategoryId = catId });
                }
            }

            await _db.SaveChangesAsync();
            return await GetByIdAsync(recipeId);
        }

        // ── DELETE ────────────────────────────────────────────────────────────
        public async Task<bool> DeleteAsync(int recipeId, int userId)
        {
            var recipe = await _db.Recipes.FirstOrDefaultAsync(r => r.RecipeId == recipeId && r.UserId == userId);
            if (recipe == null) return false;
            _db.Recipes.Remove(recipe);
            await _db.SaveChangesAsync();
            return true;
        }

        // ── IMAGE UPLOAD ──────────────────────────────────────────────────────
        public async Task<string?> UploadImageAsync(int recipeId, int userId, IFormFile file, IWebHostEnvironment env)
        {
            var recipe = await _db.Recipes.FirstOrDefaultAsync(r => r.RecipeId == recipeId && r.UserId == userId);
            if (recipe == null) return null;

            var uploadsFolder = Path.Combine(env.WebRootPath, "uploads", "recipes");
            Directory.CreateDirectory(uploadsFolder);

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
            if (!allowed.Contains(ext)) return null;

            var fileName = $"{recipeId}_{Guid.NewGuid()}{ext}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            await using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);

            recipe.ImageUrl = $"/uploads/recipes/{fileName}";
            recipe.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return recipe.ImageUrl;
        }

        // ── MAPPING HELPER ────────────────────────────────────────────────────
        private static RecipeDto MapToDto(Recipe r) => new()
        {
            RecipeId = r.RecipeId,
            UserId = r.UserId,
            AuthorUsername = r.User?.Username ?? "Unknown",
            Title = r.Title,
            Description = r.Description,
            Steps = r.Steps.OrderBy(s => s.Order).Select(s => s.Instruction).ToList(),
            ImageUrl = r.ImageUrl,
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt,
            Ingredients = r.Ingredients.Select(i => new IngredientDto { Name = i.Name, Quantity = i.Quantity }).ToList(),
            Categories = r.RecipeCategories.Select(rc => rc.Category?.Name ?? "").ToList(),
            AverageRating = r.Ratings.Any() ? r.Ratings.Average(rt => rt.Score) : 0,
            RatingCount = r.Ratings.Count
        };
    }
}
