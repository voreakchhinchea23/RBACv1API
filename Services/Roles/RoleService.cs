using Microsoft.EntityFrameworkCore;
using RbacApi.Data;
using RbacApi.DTOs;
using RbacApi.Models;

namespace RbacApi.Services
{
    public class RoleService : IRoleService
    {
        private readonly AppDbContext _context;
        public RoleService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<bool> AddSinglePermissionAsync(int roleId, int permissionId)
        {
            var alreadyExists = await _context.RolePermissions
                .AnyAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId);

            if (alreadyExists)
            {
                return true;
            }

            var roleExists = await _context.Roles.AnyAsync(r => r.Id == roleId);
            var permissionExists = await _context.Permissions.AnyAsync(p => p.Id == permissionId);

            if (!roleExists || !permissionExists)
            {
                return false;
            }

            var newRolePermission = new RolePermission
            {
                RoleId = roleId,
                PermissionId = permissionId
            };

            _context.RolePermissions.Add(newRolePermission);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> AssignPermissionToRoleAsync(AssignPermissionsDto dto)
        {
            var role = await _context.Roles
                .Include(r => r.RolePermissions)
                .FirstOrDefaultAsync(r => r.Id == dto.RoleId);

            if (role == null) return false;

            var requestedPermIds = dto.PermissionIds ?? new List<int>();

            var validPermissionIds = await _context.Permissions
                .Where(p => requestedPermIds.Contains(p.Id))
                .Select(p => p.Id)
                .ToListAsync();

            role.RolePermissions.Clear();

            foreach (var permId in validPermissionIds)
            {
                role.RolePermissions.Add(new RolePermission
                {
                    RoleId = role.Id,
                    PermissionId = permId
                });

            }

            await _context.SaveChangesAsync();
            return true;

        }

        public async Task<RoleResponseDto> CreateRoleAsync(RoleCreateDto dto)
        {
            var role = new Role
            {
                Name = dto.Name,
                Description = dto.Description
            };

            _context.Roles.Add(role);
            await _context.SaveChangesAsync();

            return new RoleResponseDto
            {
                Id = role.Id,
                Name = role.Name,
                Description = role.Description
            };
        }

        public async Task<bool> DeleteRoleAsync(int id)
        {
            var role = await _context.Roles.FindAsync(id);
            if (role == null) return false;

            if (role.Name == "Admin")
            {
                throw new InvalidOperationException("Action denied: Cannot delete the core Administrator role.");
            }

            _context.Roles.Remove(role);
            await _context.SaveChangesAsync();
            return true;

        }

        public async Task<RoleResponseDto?> GetRoleByIdAsync(int id)
        {
            var role = await _context.Roles
                .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (role == null) return null;

            return new RoleResponseDto
            {
                Id = role.Id,
                Name = role.Name,
                Description = role.Description,
                Permissions = role.RolePermissions
                    .Where(rp => rp.Permission != null)
                    .Select(rp => new PermissionResponseDto
                    {
                        Id = rp.Permission!.Id,
                        Name = rp.Permission.Name,
                        Module = rp.Permission.Module,
                        Description = rp.Permission.Description
                    }).ToList()
            };
        }

        public async Task<IEnumerable<RoleResponseDto>> GetRolesAsync()
        {
            return await _context.Roles
                .Select(r => new RoleResponseDto
                {
                    Id = r.Id,
                    Name = r.Name,
                    Description = r.Description
                }).ToListAsync();
        }

        public async Task<bool> RemoveSinglePermissionAsync(int roleId, int permissionId)
        {
            var rolePermission = await _context.RolePermissions
                .FirstOrDefaultAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId);

            if (rolePermission == null)
            {
                return false;
            }

            _context.RolePermissions.Remove(rolePermission);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> UpdateRoleAsync(RoleUpdateDto dto, int id)
        {
            var role = await _context.Roles.FindAsync(id);
            if (role == null) return false;

            if (role.Name == "Admin" && dto.Name != "Admin")
            {
                throw new InvalidOperationException("Action denied: Cannot rename the core Administrator role.");
            }

            role.Name = dto.Name;
            role.Description = dto.Description;

            await _context.SaveChangesAsync();
            return true;

        }
    }
}