using RbacApi.DTOs;

namespace RbacApi.Services
{
    public interface IRoleService
    {
        Task<IEnumerable<RoleResponseDto>> GetRolesAsync();
        Task<RoleResponseDto?> GetRoleByIdAsync(int id);
        Task<RoleResponseDto> CreateRoleAsync(RoleCreateDto dto);
        Task<bool> UpdateRoleAsync(RoleUpdateDto dto, int id);
        Task<bool> DeleteRoleAsync(int id);
        Task<bool> AssignPermissionToRoleAsync(AssignPermissionsDto dto);
        Task<bool> AddSinglePermissionAsync(int roleId, int permissionId);
        Task<bool> RemoveSinglePermissionAsync(int roleId, int permissionId);

    }
}