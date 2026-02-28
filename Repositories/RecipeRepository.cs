using Microsoft.Data.SqlClient;
using RecipeSugesstionApp.DTOs;
using System.Data;

namespace RecipeSugesstionApp.Repositories
{
    public interface IRecipeRepository
    {
        Task<(IEnumerable<RecipeSummaryDto> Recipes, int TotalCount)> SearchAsync(string? q, int? categoryId, int page, int pageSize);
    }

    /// <summary>
    /// Implementation of the 'ADO.NET Layer' as requested in the architecture diagram.
    /// Uses raw SQL and SqlConnection for high-performance searching.
    /// </summary>
    public class RecipeRepository : IRecipeRepository
    {
        private readonly string _connectionString;

        public RecipeRepository(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("DefaultConnection") 
                ?? throw new InvalidOperationException("Connection string not found.");
        }

        public async Task<(IEnumerable<RecipeSummaryDto> Recipes, int TotalCount)> SearchAsync(string? q, int? categoryId, int page, int pageSize)
        {
            var recipes = new List<RecipeSummaryDto>();
            int totalCount = 0;

            using (var conn = new SqlConnection(_connectionString))
            {
                // ── Dynamic SQL Construction (Safe with Parameters) ─────────
                var whereClause = " WHERE 1=1";
                if (!string.IsNullOrWhiteSpace(q))
                    whereClause += " AND (r.Title LIKE @q OR r.Description LIKE @q OR i.Name LIKE @q)";
                if (categoryId.HasValue)
                    whereClause += " AND rc.CategoryId = @catId";

                // Count Query
                var countSql = $@"SELECT COUNT(DISTINCT r.RecipeId) FROM Recipes r 
                                 LEFT JOIN RecipeCategories rc ON r.RecipeId = rc.RecipeId
                                 LEFT JOIN Ingredients i ON r.RecipeId = i.RecipeId {whereClause}";

                // Data Query
                var offset = (page - 1) * pageSize;
                var dataSql = $@"
                    SELECT DISTINCT r.RecipeId, r.Title, r.ImageUrl, r.CreatedAt, u.Username,
                           (SELECT AVG(CAST(Score AS FLOAT)) FROM Ratings WHERE RecipeId = r.RecipeId) as AvgRating,
                           (SELECT COUNT(*) FROM Ratings WHERE RecipeId = r.RecipeId) as RatingCount
                    FROM Recipes r
                    LEFT JOIN Users u ON r.UserId = u.UserId
                    LEFT JOIN RecipeCategories rc ON r.RecipeId = rc.RecipeId
                    LEFT JOIN Ingredients i ON r.RecipeId = i.RecipeId
                    {whereClause}
                    ORDER BY r.CreatedAt DESC
                    OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY";

                using (var cmd = new SqlCommand(countSql, conn))
                {
                    if (!string.IsNullOrWhiteSpace(q)) cmd.Parameters.AddWithValue("@q", $"%{q}%");
                    if (categoryId.HasValue) cmd.Parameters.AddWithValue("@catId", categoryId.Value);
                    await conn.OpenAsync();
                    totalCount = (int)await cmd.ExecuteScalarAsync()!;
                }

                using (var cmd = new SqlCommand(dataSql, conn))
                {
                    if (!string.IsNullOrWhiteSpace(q)) cmd.Parameters.AddWithValue("@q", $"%{q}%");
                    if (categoryId.HasValue) cmd.Parameters.AddWithValue("@catId", categoryId.Value);
                    cmd.Parameters.AddWithValue("@offset", offset);
                    cmd.Parameters.AddWithValue("@pageSize", pageSize);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            recipes.Add(new RecipeSummaryDto
                            {
                                RecipeId = reader.GetInt32(0),
                                Title = reader.GetString(1),
                                ImageUrl = reader.IsDBNull(2) ? null : reader.GetString(2),
                                CreatedAt = reader.GetDateTime(3),
                                AuthorUsername = reader.IsDBNull(4) ? "Unknown" : reader.GetString(4),
                                AverageRating = reader.IsDBNull(5) ? 0 : reader.GetDouble(5),
                                RatingCount = reader.GetInt32(6),
                                Categories = new List<string>() // Simplified for summary
                            });
                        }
                    }
                }
            }

            return (recipes, totalCount);
        }
    }
}
