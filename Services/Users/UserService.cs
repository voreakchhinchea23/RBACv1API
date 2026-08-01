using Microsoft.EntityFrameworkCore;
using RbacApi.Data;
using RbacApi.DTOs;
using RbacApi.Models;

namespace RbacApi.Services.Users
{
    public class UserService : IUserService
    {
        private readonly AppDbContext _context;
        public UserService(AppDbContext context)
        {
            _context = context;
        }


        public async Task<UserResponseDto> CreateUserAsync(UserCreateDTO dto)
        {
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

            return new UserResponseDto
            {
                Id = newUser.Id,
                Username = newUser.Username,
                Email = newUser.Email,
                Phone = newUser.Phone,
                IsActive = newUser.IsActive,
                RoleName = role != null ? role.Name : "No Role"
            };
        }

        public async Task<bool> DeleteUserAsync(int id)
        {
            var user = await _context.Users
                .FindAsync(id);

            if (user == null)
            {
                return false;
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<UserResponseDto?> GetUserByIdAsync(int id)
        {
            var user = await _context.Users
                 .Include(u => u.Role)
                 .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return null;
            }

            return new UserResponseDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                Phone = user.Phone,
                IsActive = user.IsActive,
                RoleName = user.Role != null ? user.Role.Name : "No Role"

            };
        }

        public async Task<IEnumerable<UserResponseDto>> GetUsersAsync()
        {
            return await _context.Users
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
        }

        public async Task<bool> UpdateUserAsync(UserUpdateDto dto, int id)
        {
            var user = await _context.Users
                .FindAsync(id);

            if (user == null)
            {
                return false;
            }

            user.Email = dto.Email;
            user.Username = dto.Username;
            user.Phone = dto.Phone;
            user.IsActive = dto.IsActive;
            user.RoleId = dto.RoleId;

            await _context.SaveChangesAsync();
            return true;
        }
    }
}