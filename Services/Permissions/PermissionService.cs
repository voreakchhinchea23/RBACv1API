using Microsoft.EntityFrameworkCore;
using RbacApi.Data;
using RbacApi.DTOs;
using RbacApi.Models;

namespace RbacApi.Services
{
    public class PermissionService : IPermissionService
    {
        private readonly AppDbContext _context;
        public PermissionService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<PermissionResponseDto> CreatePermissionAsync(PermissionCreateDto dto)
        {
            var perm = new Permission
            {
                Name = dto.Name,
                Module = dto.Module,
                Description = dto.Description
            };

            _context.Permissions.Add(perm);
            await _context.SaveChangesAsync();

            return new PermissionResponseDto
            {
                Id = perm.Id,
                Name = perm.Name,
                Module = perm.Module,
                Description = perm.Description
            };
        }

        public async Task<bool> DeletePermissionAsync(int id)
        {
            var perm = await _context.Permissions.FindAsync(id);
            if (perm == null) return false;
            _context.Permissions.Remove(perm);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<PermissionResponseDto?> GetPermissionByIdAsync(int id)
        {
            var perm = await _context.Permissions.FindAsync(id);
            if(perm == null) return null;

            return new PermissionResponseDto
            {
                Id = perm.Id,
                Name = perm.Name,
                Module = perm.Module,
                Description = perm.Description
            };
        }

        public async Task<IEnumerable<PermissionResponseDto>> GetPermissionsAsync()
        {
            return await _context.Permissions
                .Select(p => new PermissionResponseDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Module = p.Module,
                    Description = p.Description
                }).ToListAsync();
        }

        public async Task<bool> UpdatePermissionAsync(PermissionUpdateDto dto, int id)
        {
           var perm = await _context.Permissions.FindAsync(id);
           if (perm == null) return false;

           perm.Name = dto.Name;
           perm.Module = dto.Module;
           perm.Description = dto.Description;

           await _context.SaveChangesAsync();
           return true;
        }
    }
}