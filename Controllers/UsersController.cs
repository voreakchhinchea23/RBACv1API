using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RbacApi.Data;
using RbacApi.DTOs;
using RbacApi.Models;

namespace RbacApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;
        public UsersController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/users
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserResponseDto>>> GetsUsers()
        {
            // LINQ Projection
            var users = await _context.Users
                .Include(u => u.Role)
                .Select(u => new UserResponseDto
                {
                    Id = u.Id,
                    Username = u.Username,
                    Email = u.Email,
                    IsActive = u.IsActive,
                    Phone = u.Phone,
                    RoleName = u.Role != null ? u.Role.Name : "No Role"
                }).ToListAsync();

            return Ok(users);
        }

        // POST: api/users
        [HttpPost]
        public async Task<ActionResult<UserResponseDto>> CreateUser(UserCreateDTO dto)
        {
            // Hash password using BCrypt
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            var newUser = new User
            {
                Username = dto.Username,
                Email = dto.Email,
                Password = passwordHash,
                Phone = dto.Phone,
                RoleId = dto.RoleId,
                IsActive = true
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            var role = await _context.Roles.FindAsync(dto.RoleId);

            var responseDto = new UserResponseDto
            {
                Id = newUser.Id,
                Username = newUser.Username,
                Email = newUser.Email,
                Phone = newUser.Phone,
                IsActive = newUser.IsActive,
                RoleName = role != null ? role.Name : "No Role"
            };

            return CreatedAtAction(nameof(GetsUsers), new {id = newUser.Id}, responseDto);
        }
    }
}