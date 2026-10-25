using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using RbacApi.DTOs;
using RbacApi.Services.Auth;

namespace RbacApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        //POST: api/auth/login
        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginRequestDto dto)
        {
            var result = await _authService.LoginAsync(dto);

            if(result == null)
            {
                return Unauthorized(new {message = "Invalid credentials"});
            }
            return Ok(result);
        }

        // POST: api/auth/logout
        [Authorize]
        [HttpPost("logout")]
        public IActionResult Logout([FromServices] ITokenBlacklistService blacklistService)
        {
            // Extract token from Authorization header
            var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");

            if (string.IsNullOrEmpty(token))
            {
                return BadRequest(new { message = "Invalid token." });
            }

            // Add token to blacklist for 8 hours
            blacklistService.BlacklistToken(token, TimeSpan.FromHours(8));

            return Ok(new { message = "Successfully logged out. Token is now invalidated." });
        }
    }
}
