using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RecipeSugesstionApp.Data;
using RecipeSugesstionApp.DTOs;
using RecipeSugesstionApp.Models;

namespace RecipeSugesstionApp.Services
{
    public interface IAuthService
    {
        Task<AuthResponseDto?> RegisterAsync(RegisterDto dto);
        Task<AuthResponseDto?> LoginAsync(LoginDto dto);
    }

    public class AuthService : IAuthService
    {
        private readonly RecipeDbContext _db;
        private readonly IConfiguration _config;

        public AuthService(RecipeDbContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }

        public async Task<AuthResponseDto?> RegisterAsync(RegisterDto dto)
        {
            if (await _db.Users.AnyAsync(u => u.Email == dto.Email || u.Username == dto.Username))
                return null;

            var user = new User
            {
                Username = dto.Username,
                Email = dto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                CreatedAt = DateTime.UtcNow
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            return GenerateToken(user);
        }

        public async Task<AuthResponseDto?> LoginAsync(LoginDto dto)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                return null;

            return GenerateToken(user);
        }

        private AuthResponseDto GenerateToken(User user)
        {
            var jwtKey = _config["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not configured.");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expiry = DateTime.UtcNow.AddHours(12);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim("username", user.Username),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: expiry,
                signingCredentials: creds);

            return new AuthResponseDto
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                Username = user.Username,
                UserId = user.UserId,
                ExpiresAt = expiry
            };
        }
    }
}
