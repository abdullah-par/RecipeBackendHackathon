using Microsoft.AspNetCore.Mvc;
using RecipeSugesstionApp.DTOs;
using RecipeSugesstionApp.Services;

namespace RecipeSugesstionApp.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _auth;

        public AuthController(IAuthService auth) => _auth = auth;

        /// <summary>Register a new user account.</summary>
        [HttpPost("register")]
        [ProducesResponseType(typeof(AuthResponseDto), 200)]
        [ProducesResponseType(409)]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            var result = await _auth.RegisterAsync(dto);
            if (result == null)
                return Conflict(new { message = "Username or email already exists." });
            return Ok(result);
        }

        /// <summary>Login and receive a JWT token.</summary>
        [HttpPost("login")]
        [ProducesResponseType(typeof(AuthResponseDto), 200)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var result = await _auth.LoginAsync(dto);
            if (result == null)
                return Unauthorized(new { message = "Invalid email or password." });
            return Ok(result);
        }
    }
}
