using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RbacApi.Data;
using RbacApi.DTOs;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace RbacApi.Services.Auth
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;
        public AuthService(AppDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }
        public async Task<LoginResponseDto?> LoginAsync(LoginRequestDto dto)
        {
            // find user by username
            var user = await _context.Users.
               Include(u => u.Role!)
               .ThenInclude(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(u => u.Username == dto.Username);

            if (user == null || !user.IsActive)
            {
                return null;
            }

            // verify password
            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(dto.Password, user.Password);
            if(!isPasswordValid)
            {
                return null;
            }

            // collect permissions
            var permissions = user.Role?.RolePermissions
                .Select(rp => rp.Permission.Name)
                .ToList() ?? new List<string>();


            // build JWT claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role?.Name ?? string.Empty)
            };

            // add each permission as a claim
            foreach (var perm in permissions)
            {
                claims.Add(new Claim("permission", perm));
            }

            // generate jwt token
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var tokenDescriptor = new JwtSecurityToken(
              issuer: _config["Jwt:Issuer"],
              audience: _config["Jwt:Audience"],
              claims: claims,
              expires: DateTime.UtcNow.AddHours(8), // Token valid for 8 hours
              signingCredentials: creds
            );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);

            return new LoginResponseDto
            {
                Token = tokenString,
                Username = user.Username,
                Role = user.Role?.Name ?? "NoRole",
                Permissions = permissions
            };
        }
    }
}
