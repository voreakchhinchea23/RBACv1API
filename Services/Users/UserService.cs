using Microsoft.EntityFrameworkCore;
using RbacApi.Data;
using RbacApi.DTOs;
using RbacApi.Models;

namespace RbacApi.Services
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
            if (string.IsNullOrWhiteSpace(dto.Username))
                throw new ArgumentException("Username is required.");
            if (string.IsNullOrWhiteSpace(dto.Password))
                throw new ArgumentException("Password is required.");
            if (string.IsNullOrWhiteSpace(dto.Email))
                throw new ArgumentException("Email is required.");

            if (await _context.Users.AnyAsync(u => u.Username == dto.Username))
                throw new InvalidOperationException($"Username '{dto.Username}' is already taken.");
            if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
                throw new InvalidOperationException($"Email '{dto.Email}' is already registered.");

            string passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            var newUser = new User
            {
                Username = dto.Username,
                Email = dto.Email,
                Password = passwordHash,
                Phone = dto.Phone,
                IsActive = true
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            // Collect requested role IDs (supporting both list and single property)
            var requestedRoleIds = dto.RoleIds?.Where(id => id > 0).Distinct().ToList() ?? new List<int>();
            if (dto.RoleId.HasValue && dto.RoleId.Value > 0 && !requestedRoleIds.Contains(dto.RoleId.Value))
            {
                requestedRoleIds.Add(dto.RoleId.Value);
            }

            if (requestedRoleIds.Count > 0)
            {
                var validRoleIds = await _context.Roles
                    .Where(r => requestedRoleIds.Contains(r.Id))
                    .Select(r => r.Id)
                    .ToListAsync();

                foreach (var rId in validRoleIds)
                {
                    _context.UserRoles.Add(new UserRole
                    {
                        UserId = newUser.Id,
                        RoleId = rId
                    });
                }
                await _context.SaveChangesAsync();
            }

            var assignedRoles = await _context.UserRoles
                .Where(ur => ur.UserId == newUser.Id)
                .Include(ur => ur.Role)
                .Where(ur => ur.Role != null)
                .Select(ur => ur.Role!.Name)
                .ToListAsync();

            return new UserResponseDto
            {
                Id = newUser.Id,
                Username = newUser.Username,
                Email = newUser.Email,
                Phone = newUser.Phone,
                IsActive = newUser.IsActive,
                Roles = assignedRoles,
                RoleName = assignedRoles.FirstOrDefault() ?? "No Role"
            };
        }

        public async Task<bool> DeleteUserAsync(int id)
        {
            var user = await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return false;
            }

            // Prevent deleting the last active Admin account
            bool isAdmin = user.UserRoles.Any(ur => ur.Role != null && ur.Role.Name == "Admin");
            if (isAdmin && user.IsActive)
            {
                var adminCount = await _context.UserRoles
                    .Include(ur => ur.Role)
                    .Include(ur => ur.User)
                    .Where(ur => ur.Role != null && ur.Role.Name == "Admin" && ur.User != null && ur.User.IsActive)
                    .Select(ur => ur.UserId)
                    .Distinct()
                    .CountAsync();

                if (adminCount <= 1)
                {
                    throw new InvalidOperationException("Action denied: Cannot delete the last remaining Administrator account.");
                }
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<UserResponseDto?> GetUserByIdAsync(int id)
        {
            var user = await _context.Users
                 .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                 .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return null;
            }

            var roles = user.UserRoles
                .Where(ur => ur.Role != null)
                .Select(ur => ur.Role!.Name)
                .ToList();

            return new UserResponseDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                Phone = user.Phone,
                IsActive = user.IsActive,
                Roles = roles,
                RoleName = roles.FirstOrDefault() ?? "No Role"
            };
        }

        public async Task<IEnumerable<UserResponseDto>> GetUsersAsync()
        {
            var users = await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .ToListAsync();

            return users.Select(u =>
            {
                var roles = u.UserRoles
                    .Where(ur => ur.Role != null)
                    .Select(ur => ur.Role!.Name)
                    .ToList();

                return new UserResponseDto
                {
                    Id = u.Id,
                    Username = u.Username,
                    Email = u.Email,
                    IsActive = u.IsActive,
                    Phone = u.Phone,
                    Roles = roles,
                    RoleName = roles.FirstOrDefault() ?? "No Role"
                };
            }).ToList();
        }

        public async Task<bool> UpdateUserAsync(UserUpdateDto dto, int id)
        {
            var user = await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return false;
            }

            if (await _context.Users.AnyAsync(u => u.Username == dto.Username && u.Id != id))
            {
                throw new InvalidOperationException($"Username '{dto.Username}' is already taken.");
            }
            if (await _context.Users.AnyAsync(u => u.Email == dto.Email && u.Id != id))
            {
                throw new InvalidOperationException($"Email '{dto.Email}' is already registered.");
            }

            bool wasAdmin = user.UserRoles.Any(ur => ur.Role != null && ur.Role.Name == "Admin");

            // Guard: Cannot deactivate the last remaining active Administrator
            if (wasAdmin && user.IsActive && !dto.IsActive)
            {
                var activeAdminCount = await _context.UserRoles
                    .Include(ur => ur.Role)
                    .Include(ur => ur.User)
                    .Where(ur => ur.Role != null && ur.Role.Name == "Admin" && ur.User != null && ur.User.IsActive)
                    .Select(ur => ur.UserId)
                    .Distinct()
                    .CountAsync();

                if (activeAdminCount <= 1)
                {
                    throw new InvalidOperationException("Action denied: Cannot deactivate the last remaining Administrator account.");
                }
            }

            user.Email = dto.Email;
            user.Username = dto.Username;
            user.Phone = dto.Phone;
            user.IsActive = dto.IsActive;

            // If roles were specified, sync them
            if (dto.RoleIds != null || dto.RoleId.HasValue)
            {
                var requestedRoleIds = dto.RoleIds?.Where(rid => rid > 0).Distinct().ToList() ?? new List<int>();
                if (dto.RoleId.HasValue && dto.RoleId.Value > 0 && !requestedRoleIds.Contains(dto.RoleId.Value))
                {
                    requestedRoleIds.Add(dto.RoleId.Value);
                }

                var validRoles = await _context.Roles
                    .Where(r => requestedRoleIds.Contains(r.Id))
                    .ToListAsync();

                // Check last admin safety: if user was an admin and the new roles do not include Admin
                bool willBeAdmin = validRoles.Any(r => r.Name == "Admin");

                if (wasAdmin && !willBeAdmin && user.IsActive)
                {
                    var activeAdminCount = await _context.UserRoles
                        .Include(ur => ur.Role)
                        .Include(ur => ur.User)
                        .Where(ur => ur.Role != null && ur.Role.Name == "Admin" && ur.User != null && ur.User.IsActive)
                        .Select(ur => ur.UserId)
                        .Distinct()
                        .CountAsync();

                    if (activeAdminCount <= 1)
                    {
                        throw new InvalidOperationException("Action denied: Cannot remove Admin role from the last remaining Administrator account.");
                    }
                }

                user.UserRoles.Clear();
                foreach (var role in validRoles)
                {
                    user.UserRoles.Add(new UserRole
                    {
                        UserId = user.Id,
                        RoleId = role.Id
                    });
                }
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> AssignRolesToUserAsync(AssignUserRolesDto dto)
        {
            var user = await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == dto.UserId);

            if (user == null) return false;

            var requestedRoleIds = dto.RoleIds?.Where(rid => rid > 0).Distinct().ToList() ?? new List<int>();

            var validRoles = await _context.Roles
                .Where(r => requestedRoleIds.Contains(r.Id))
                .ToListAsync();

            // Check if removing Admin from last active Admin
            bool wasAdmin = user.UserRoles.Any(ur => ur.Role != null && ur.Role.Name == "Admin");
            bool willBeAdmin = validRoles.Any(r => r.Name == "Admin");

            if (wasAdmin && !willBeAdmin && user.IsActive)
            {
                var activeAdminCount = await _context.UserRoles
                    .Include(ur => ur.Role)
                    .Include(ur => ur.User)
                    .Where(ur => ur.Role != null && ur.Role.Name == "Admin" && ur.User != null && ur.User.IsActive)
                    .Select(ur => ur.UserId)
                    .Distinct()
                    .CountAsync();

                if (activeAdminCount <= 1)
                {
                    throw new InvalidOperationException("Action denied: Cannot remove Admin role from the last remaining Administrator account.");
                }
            }

            user.UserRoles.Clear();
            foreach (var role in validRoles)
            {
                user.UserRoles.Add(new UserRole
                {
                    UserId = user.Id,
                    RoleId = role.Id
                });
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> AddSingleRoleAsync(int userId, int roleId)
        {
            var alreadyExists = await _context.UserRoles
                .AnyAsync(ur => ur.UserId == userId && ur.RoleId == roleId);

            if (alreadyExists) return true;

            var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
            var roleExists = await _context.Roles.AnyAsync(r => r.Id == roleId);

            if (!userExists || !roleExists) return false;

            _context.UserRoles.Add(new UserRole
            {
                UserId = userId,
                RoleId = roleId
            });

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RemoveSingleRoleAsync(int userId, int roleId)
        {
            var userRole = await _context.UserRoles
                .Include(ur => ur.Role)
                .Include(ur => ur.User)
                .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId);

            if (userRole == null) return false;

            // Prevent removing Admin role from the last active Admin
            if (userRole.Role?.Name == "Admin" && (userRole.User?.IsActive ?? true))
            {
                var activeAdminCount = await _context.UserRoles
                    .Include(ur => ur.Role)
                    .Include(ur => ur.User)
                    .Where(ur => ur.Role != null && ur.Role.Name == "Admin" && ur.User != null && ur.User.IsActive)
                    .Select(ur => ur.UserId)
                    .Distinct()
                    .CountAsync();

                if (activeAdminCount <= 1)
                {
                    throw new InvalidOperationException("Action denied: Cannot remove Admin role from the last remaining Administrator account.");
                }
            }

            _context.UserRoles.Remove(userRole);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}